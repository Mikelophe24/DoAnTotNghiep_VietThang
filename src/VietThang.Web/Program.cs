using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Văn hóa vi-VN (định dạng tiền, ngày) ----------
var viCulture = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = viCulture;
CultureInfo.DefaultThreadCurrentUICulture = viCulture;

// ---------- CSDL ----------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Identity ----------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie Identity chỉ dùng cho trang quản trị Razor (khách hàng đăng nhập bằng JWT qua Angular)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/login";
    options.LogoutPath = "/Admin/logout";
    options.AccessDeniedPath = "/Admin/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddMemoryCache();

// ---------- JWT cho REST API (Angular storefront) ----------
// Khóa ký không nằm trong mã nguồn: lấy từ cấu hình/biến môi trường, nếu không có thì dùng file App_Data/jwt-key.txt tự sinh.
var jwtKey = JwtKeyProvider.Resolve(builder.Configuration, builder.Environment);
builder.Configuration["Jwt:Key"] = jwtKey;
builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

// ---------- CORS cho Angular ----------
builder.Services.AddCors(options => options.AddPolicy("Angular", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:4200" })
    .AllowAnyHeader().AllowAnyMethod()));

// ---------- Dịch vụ ứng dụng ----------
// Ảnh tải lên: có đủ Cloudinary:CloudName/ApiKey/ApiSecret (user-secrets) thì lưu lên Cloudinary, chưa có thì lưu wwwroot/uploads
var cloudinaryOptions = builder.Configuration.GetSection("Cloudinary").Get<CloudinaryOptions>() ?? new CloudinaryOptions();
if (cloudinaryOptions.IsConfigured)
{
    builder.Services.AddSingleton(cloudinaryOptions);
    builder.Services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}
builder.Services.AddTransient<IHtmlSanitizer, HtmlSanitizer>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// Email: có cấu hình Smtp:Host thì gửi thật qua SMTP, chưa có thì ghi log
if (!string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]))
    builder.Services.AddScoped<IAppEmailSender, SmtpEmailSender>();
else
    builder.Services.AddScoped<IAppEmailSender, LogEmailSender>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(options =>
{
    // Mọi POST/PUT/DELETE của Razor đều phải có anti-forgery token (API dùng [IgnoreAntiforgeryToken] + JWT)
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

// ---------- Migrate + seed dữ liệu ----------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await SeedData.SeedAsync(scope.ServiceProvider);
        logger.LogInformation("Đã migrate và seed CSDL.");

        // Dữ liệu demo cho báo cáo/bảo vệ: chạy một lần bằng  dotnet run -- --seed-demo
        if (args.Contains("--seed-demo"))
            await DemoDataSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Không thể migrate/seed CSDL. Kiểm tra SQL Server và chuỗi kết nối.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(viCulture),
    SupportedCultures = new[] { viCulture },
    SupportedUICultures = new[] { viCulture }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

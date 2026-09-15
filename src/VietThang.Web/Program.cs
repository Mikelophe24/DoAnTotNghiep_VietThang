using System.Globalization;
using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/dang-nhap";
    options.LogoutPath = "/dang-xuat";
    options.AccessDeniedPath = "/khong-co-quyen";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    // Khu vực /Admin dùng trang đăng nhập riêng
    options.Events.OnRedirectToLogin = ctx =>
    {
        var loginPath = ctx.Request.Path.StartsWithSegments("/Admin") ? "/Admin/dang-nhap" : "/dang-nhap";
        var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
        ctx.Response.Redirect($"{loginPath}?ReturnUrl={returnUrl}");
        return Task.CompletedTask;
    };
});

builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddMemoryCache();

// ---------- Session (giỏ hàng khách vãng lai) ----------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ---------- Dịch vụ ứng dụng ----------
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddTransient<IHtmlSanitizer, HtmlSanitizer>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAppEmailSender, LogEmailSender>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(options =>
{
    // Mọi POST/PUT/DELETE đều phải có anti-forgery token
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
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
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

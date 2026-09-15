using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Api;

namespace VietThang.Web.Controllers.Api;

/// <summary>API nội dung: tin tức, tuyển dụng, cửa hàng, liên hệ, cấu hình công khai.</summary>
[Route("api/content")]
public class ContentApiController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISettingService _settings;

    public ContentApiController(ApplicationDbContext db, ISettingService settings)
    {
        _db = db;
        _settings = settings;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> Settings() => Ok(new
    {
        storeName = await _settings.GetAsync(SettingKeys.StoreName),
        hotline = await _settings.GetAsync(SettingKeys.Hotline),
        email = await _settings.GetAsync(SettingKeys.Email),
        freeShippingThreshold = await _settings.GetDecimalAsync(SettingKeys.FreeShippingThreshold, 500000),
        defaultShippingFee = await _settings.GetDecimalAsync(SettingKeys.DefaultShippingFee, 30000),
        bankAccount = await _settings.GetAsync(SettingKeys.BankAccount)
    });

    [HttpGet("posts")]
    public async Task<IActionResult> Posts([FromQuery] PostType type = PostType.News, [FromQuery] int page = 1)
    {
        var query = _db.Posts.AsNoTracking().Where(p => p.IsPublished && p.Type == type).OrderByDescending(p => p.PublishedAt);
        var paged = await PagedList<Post>.CreateAsync(query, page, 9);
        return Ok(new
        {
            items = paged.Items.Select(p => new { p.Id, p.Title, p.Slug, p.Summary, p.ThumbnailUrl, p.PublishedAt, p.Type }),
            paged.PageIndex, paged.TotalPages, paged.TotalCount
        });
    }

    [HttpGet("posts/{slug}")]
    public async Task<IActionResult> Post(string slug)
    {
        var post = await _db.Posts.AsNoTracking().Include(p => p.Author).FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (post is null) return NotFound(new { message = "Không tìm thấy bài viết." });
        var recent = await _db.Posts.AsNoTracking().Where(p => p.IsPublished && p.Type == post.Type && p.Id != post.Id)
            .OrderByDescending(p => p.PublishedAt).Take(5).Select(p => new { p.Title, p.Slug }).ToListAsync();
        return Ok(new { post.Id, post.Title, post.Slug, post.Summary, post.Content, post.ThumbnailUrl, post.Type, post.PublishedAt, author = post.Author?.FullName, recent });
    }

    [HttpGet("stores")]
    public async Task<IActionResult> Stores()
        => Ok(await _db.Stores.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.Address, s.Phone, s.OpeningHours, s.MapEmbedUrl }).ToListAsync());

    [HttpPost("contact")]
    public async Task<IActionResult> Contact([FromBody] ContactRequest req)
    {
        if (!ModelState.IsValid) return ValidationError();
        _db.Contacts.Add(new Contact { FullName = req.FullName.Trim(), Email = req.Email?.Trim(), Phone = req.Phone?.Trim(), Subject = req.Subject?.Trim(), Message = req.Message.Trim() });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Cảm ơn bạn. Cửa hàng sẽ liên hệ lại trong thời gian sớm nhất." });
    }
}

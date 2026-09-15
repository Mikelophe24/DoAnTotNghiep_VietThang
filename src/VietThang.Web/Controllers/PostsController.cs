using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;

namespace VietThang.Web.Controllers;

/// <summary>KH14 – Tin tức và tuyển dụng.</summary>
public class PostsController : Controller
{
    private const int PageSize = 9;
    private readonly ApplicationDbContext _db;
    public PostsController(ApplicationDbContext db) => _db = db;

    [HttpGet("tin-tuc")]
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewBag.Title = "Tin tức";
        ViewBag.Type = PostType.News;
        return View(await Query(PostType.News, page));
    }

    [HttpGet("tuyen-dung")]
    public async Task<IActionResult> Recruitment(int page = 1)
    {
        ViewBag.Title = "Tuyển dụng";
        ViewBag.Type = PostType.Recruitment;
        return View("Index", await Query(PostType.Recruitment, page));
    }

    [HttpGet("tin-tuc/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var post = await _db.Posts.AsNoTracking().Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        if (post is null) return NotFound();
        ViewBag.Recent = await _db.Posts.AsNoTracking()
            .Where(p => p.IsPublished && p.Type == post.Type && p.Id != post.Id)
            .OrderByDescending(p => p.PublishedAt).Take(5).ToListAsync();
        return View(post);
    }

    private Task<PagedList<Post>> Query(PostType type, int page) =>
        PagedList<Post>.CreateAsync(
            _db.Posts.AsNoTracking().Where(p => p.IsPublished && p.Type == type).OrderByDescending(p => p.PublishedAt),
            page, PageSize);
}

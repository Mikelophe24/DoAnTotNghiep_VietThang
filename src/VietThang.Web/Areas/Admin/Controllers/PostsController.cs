using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT16 – Tin tức và tuyển dụng (chỉ Admin).</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class PostsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly IHtmlSanitizer _sanitizer;
    private readonly UserManager<ApplicationUser> _userManager;

    public PostsController(ApplicationDbContext db, IFileStorageService files, IHtmlSanitizer sanitizer, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _files = files;
        _sanitizer = sanitizer;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(PostType? type, int page = 1)
    {
        var query = _db.Posts.AsNoTracking().Include(p => p.Author).AsQueryable();
        if (type.HasValue) query = query.Where(p => p.Type == type);
        ViewBag.Type = type;
        return View(await PagedList<Post>.CreateAsync(query.OrderByDescending(p => p.CreatedAt), page, 20));
    }

    public IActionResult Create(PostType type = PostType.News) => View("Form", new PostFormViewModel { Type = type });

    [HttpPost]
    public async Task<IActionResult> Create(PostFormViewModel model)
    {
        await ValidateAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        var post = new Post { AuthorId = _userManager.GetUserId(User) };
        await ApplyAsync(post, model);
        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo bài \"{post.Title}\".";
        return RedirectToAction(nameof(Index), new { type = post.Type });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Posts.FindAsync(id);
        if (p is null) return NotFound();
        return View("Form", new PostFormViewModel
        {
            Id = p.Id, Title = p.Title, Slug = p.Slug, Summary = p.Summary, Content = p.Content, Type = p.Type, IsPublished = p.IsPublished, ThumbnailUrl = p.ThumbnailUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, PostFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var p = await _db.Posts.FindAsync(id);
        if (p is null) return NotFound();
        await ValidateAsync(model);
        if (!ModelState.IsValid) { model.ThumbnailUrl = p.ThumbnailUrl; return View("Form", model); }
        await ApplyAsync(p, model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật bài \"{p.Title}\".";
        return RedirectToAction(nameof(Index), new { type = p.Type });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Posts.FindAsync(id);
        if (p is null) return NotFound();
        _files.Delete(p.ThumbnailUrl);
        _db.Posts.Remove(p);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa bài \"{p.Title}\".";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(PostFormViewModel m)
    {
        var slug = string.IsNullOrWhiteSpace(m.Slug) ? SlugHelper.ToSlug(m.Title) : SlugHelper.ToSlug(m.Slug);
        if (await _db.Posts.AnyAsync(p => p.Slug == slug && p.Id != m.Id)) ModelState.AddModelError(nameof(m.Slug), $"Slug \"{slug}\" đã tồn tại.");
        m.Slug = slug;
    }

    private async Task ApplyAsync(Post p, PostFormViewModel m)
    {
        p.Title = m.Title.Trim();
        p.Slug = m.Slug!;
        p.Summary = m.Summary?.Trim();
        p.Content = _sanitizer.Sanitize(m.Content);
        p.Type = m.Type;
        if (m.IsPublished && !p.IsPublished) p.PublishedAt = DateTime.Now;
        p.IsPublished = m.IsPublished;
        if (m.ThumbnailFile is not null)
        {
            _files.Delete(p.ThumbnailUrl);
            p.ThumbnailUrl = await _files.SaveImageAsync(m.ThumbnailFile, "posts");
        }
    }
}

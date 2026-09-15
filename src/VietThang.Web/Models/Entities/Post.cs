using VietThang.Web.Models.Enums;

namespace VietThang.Web.Models.Entities;

/// <summary>Bài viết: tin tức hoặc tuyển dụng.</summary>
public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public PostType Type { get; set; } = PostType.News;
    public string? AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

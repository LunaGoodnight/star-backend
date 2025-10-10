namespace StarApi.Models;

public class Post
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public bool IsDraft { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
}

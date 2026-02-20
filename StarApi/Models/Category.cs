namespace StarApi.Models;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Navigation property
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}

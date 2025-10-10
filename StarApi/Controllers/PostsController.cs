using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarApi.Data;
using StarApi.Models;

namespace StarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostsController> _logger;

    public PostsController(ApplicationDbContext context, ILogger<PostsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/posts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Post>>> GetPosts()
    {
        // Public users only see published posts, admin sees all
        var isAdmin = User.Identity?.IsAuthenticated ?? false;

        var posts = isAdmin
            ? await _context.Posts.OrderByDescending(p => p.CreatedAt).ToListAsync()
            : await _context.Posts.Where(p => !p.IsDraft).OrderByDescending(p => p.PublishedAt).ToListAsync();

        return Ok(posts);
    }

    // GET: api/posts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Post>> GetPost(int id)
    {
        var post = await _context.Posts.FindAsync(id);

        if (post == null)
        {
            return NotFound();
        }

        // Public users can only view published posts
        var isAdmin = User.Identity?.IsAuthenticated ?? false;
        if (!isAdmin && post.IsDraft)
        {
            return NotFound();
        }

        return Ok(post);
    }

    // POST: api/posts
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Post>> CreatePost([FromBody] Post post)
    {
        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;

        if (!post.IsDraft && post.PublishedAt == null)
        {
            post.PublishedAt = DateTime.UtcNow;
        }

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
    }

    // PUT: api/posts/5
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] Post post)
    {
        if (id != post.Id)
        {
            return BadRequest();
        }

        var existingPost = await _context.Posts.FindAsync(id);
        if (existingPost == null)
        {
            return NotFound();
        }

        existingPost.Title = post.Title;
        existingPost.Content = post.Content;
        existingPost.IsDraft = post.IsDraft;
        existingPost.UpdatedAt = DateTime.UtcNow;

        // Set PublishedAt when publishing for the first time
        if (!post.IsDraft && existingPost.PublishedAt == null)
        {
            existingPost.PublishedAt = DateTime.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PostExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/posts/5
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool PostExists(int id)
    {
        return _context.Posts.Any(e => e.Id == id);
    }
}

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
    public async Task<ActionResult<IEnumerable<Post>>> GetPosts([FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] int? categoryId)
    {
        // Public users only see published posts, admin sees all
        var isAdmin = User.Identity?.IsAuthenticated ?? false;

        var query = isAdmin
            ? _context.Posts.Include(p => p.Category).OrderByDescending(p => p.CreatedAt)
            : _context.Posts.Include(p => p.Category).Where(p => !p.IsDraft).OrderByDescending(p => p.PublishedAt);

        // Filter by category if specified
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (page.HasValue && pageSize.HasValue && page > 0 && pageSize > 0)
        {
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Total-Pages"] = totalPages.ToString();
            Response.Headers["Access-Control-Expose-Headers"] = "X-Total-Count, X-Total-Pages";

            var posts = await query
                .Skip((page.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToListAsync();

            return Ok(posts);
        }

        return Ok(await query.ToListAsync());
    }

    // GET: api/posts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Post>> GetPost(int id)
    {
        var post = await _context.Posts.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

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

        // Use provided PublishedAt or default to now when publishing
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
        existingPost.CategoryId = post.CategoryId;
        existingPost.Thumbnail = post.Thumbnail;
        existingPost.UpdatedAt = DateTime.UtcNow;

        // Allow manual PublishedAt override, or default to now when publishing for the first time
        if (post.PublishedAt != null)
        {
            existingPost.PublishedAt = post.PublishedAt;
        }
        else if (!post.IsDraft && existingPost.PublishedAt == null)
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

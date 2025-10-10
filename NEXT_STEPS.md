# Next Steps - StarApi Development Guide

This guide outlines what to do next with your StarApi backend project.

---

## ✅ What You've Already Done

- ✅ Created ASP.NET Core Web API project
- ✅ Removed all frontend code (Views, wwwroot, MVC controllers)
- ✅ Configured PostgreSQL database with Entity Framework Core
- ✅ Created Post model and ApplicationDbContext
- ✅ Implemented PostsController with CRUD operations
- ✅ Set up Docker Compose for PostgreSQL
- ✅ Created initial database migration (`InitialCreate`)
- ✅ Configured API Key authentication middleware
- ✅ Added Swagger/OpenAPI documentation

---

## 🎯 Immediate Next Steps

### 1. Start the Application

#### Option A: Using Docker Compose (Recommended)

```bash
# Start both database and API
docker compose up -d

# View logs
docker compose logs -f starapi

# Stop services
docker compose down
```

**Access Points:**
- API: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- PostgreSQL: `localhost:5432`

#### Option B: Run Locally (Development)

```bash
# Start PostgreSQL only
docker compose up db -d

# Run the API locally
cd StarApi
dotnet run
```

**Access Points:**
- API: `http://localhost:5000` or `https://localhost:5001`
- Swagger UI: `http://localhost:5000/swagger`

---

### 2. Test Your API Endpoints

#### Using Swagger UI

1. Navigate to `http://localhost:8080/swagger`
2. Explore available endpoints
3. Test GET requests (no authentication needed for published posts)
4. For POST/PUT/DELETE, you'll need to add API Key authentication

#### Using cURL

**Get All Published Posts:**
```bash
curl http://localhost:8080/api/posts
```

**Get Single Post:**
```bash
curl http://localhost:8080/api/posts/1
```

**Create a Post (Requires API Key):**
```bash
curl -X POST http://localhost:8080/api/posts \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: your-api-key-here-change-in-production" \
  -d '{
    "title": "My First Post",
    "content": "This is my first blog post content",
    "isDraft": false
  }'
```

**Update a Post (Requires API Key):**
```bash
curl -X PUT http://localhost:8080/api/posts/1 \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: your-api-key-here-change-in-production" \
  -d '{
    "id": 1,
    "title": "Updated Title",
    "content": "Updated content",
    "isDraft": false
  }'
```

**Delete a Post (Requires API Key):**
```bash
curl -X DELETE http://localhost:8080/api/posts/1 \
  -H "X-Api-Key: your-api-key-here-change-in-production"
```

---

### 3. Configure Your API Key

#### Development Environment

Update `StarApi/appsettings.Development.json`:

```json
{
  "ApiKey": "dev-api-key-12345"
}
```

#### Production Environment

Create `.env` file in project root (already gitignored):

```env
API_KEY=your-secure-production-api-key-here
```

**Generate a secure API key:**
```bash
# Using PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))

# Using openssl (Git Bash/WSL)
openssl rand -base64 32
```

---

## 🚀 Frontend Integration (Next.js)

### API Endpoints for Your Next.js Frontend

**Base URL:** `http://localhost:8080` (development) or your production URL

| Method | Endpoint | Auth Required | Description |
|--------|----------|---------------|-------------|
| GET | `/api/posts` | No | Get all published posts (public users) or all posts (authenticated) |
| GET | `/api/posts/{id}` | No* | Get single post (*draft posts require auth) |
| POST | `/api/posts` | Yes | Create new post |
| PUT | `/api/posts/{id}` | Yes | Update existing post |
| DELETE | `/api/posts/{id}` | Yes | Delete post |

### Authentication Header

For authenticated requests, add this header:
```
X-Api-Key: your-api-key-here
```

### Example Next.js API Integration

```typescript
// lib/api.ts
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
const API_KEY = process.env.API_KEY;

export async function getPosts() {
  const res = await fetch(`${API_BASE_URL}/api/posts`);
  if (!res.ok) throw new Error('Failed to fetch posts');
  return res.json();
}

export async function getPost(id: number) {
  const res = await fetch(`${API_BASE_URL}/api/posts/${id}`);
  if (!res.ok) throw new Error('Failed to fetch post');
  return res.json();
}

export async function createPost(post: any) {
  const res = await fetch(`${API_BASE_URL}/api/posts`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': API_KEY || '',
    },
    body: JSON.stringify(post),
  });
  if (!res.ok) throw new Error('Failed to create post');
  return res.json();
}

export async function updatePost(id: number, post: any) {
  const res = await fetch(`${API_BASE_URL}/api/posts/${id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': API_KEY || '',
    },
    body: JSON.stringify(post),
  });
  if (!res.ok) throw new Error('Failed to update post');
  return res.status;
}

export async function deletePost(id: number) {
  const res = await fetch(`${API_BASE_URL}/api/posts/${id}`, {
    method: 'DELETE',
    headers: {
      'X-Api-Key': API_KEY || '',
    },
  });
  if (!res.ok) throw new Error('Failed to delete post');
  return res.status;
}
```

---

## 🔧 Recommended Improvements

### 1. Add DTOs (Data Transfer Objects)

Instead of exposing your entities directly, create DTOs:

```csharp
// Models/DTOs/CreatePostDto.cs
public class CreatePostDto
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public bool IsDraft { get; set; } = true;
}

// Models/DTOs/UpdatePostDto.cs
public class UpdatePostDto
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public bool IsDraft { get; set; }
}

// Models/DTOs/PostResponseDto.cs
public class PostResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public bool IsDraft { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
```

### 2. Add Input Validation

Install FluentValidation:
```bash
dotnet add package FluentValidation.AspNetCore
```

Create validators:
```csharp
public class CreatePostDtoValidator : AbstractValidator<CreatePostDto>
{
    public CreatePostDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required");
    }
}
```

### 3. Add Logging

Enhance logging in your controllers:
```csharp
_logger.LogInformation("Creating new post: {Title}", post.Title);
_logger.LogError(ex, "Error creating post");
```

### 4. Add Exception Handling Middleware

Create global error handling:
```csharp
// Middleware/GlobalExceptionHandlerMiddleware.cs
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            error = "An error occurred processing your request",
            detail = exception.Message
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
```

### 5. Add Pagination

For better performance with large datasets:
```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<Post>>> GetPosts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    var query = _context.Posts.AsQueryable();

    if (!(User.Identity?.IsAuthenticated ?? false))
    {
        query = query.Where(p => !p.IsDraft);
    }

    var total = await query.CountAsync();
    var posts = await query
        .OrderByDescending(p => p.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Ok(new PagedResult<Post>
    {
        Items = posts,
        TotalCount = total,
        Page = page,
        PageSize = pageSize
    });
}
```

### 6. Add Categories/Tags

Extend your Post model:
```csharp
public class Post
{
    // ... existing properties
    public List<string> Tags { get; set; } = new();
    public string? Category { get; set; }
}
```

### 7. Add Search Functionality

```csharp
[HttpGet("search")]
public async Task<ActionResult<IEnumerable<Post>>> SearchPosts([FromQuery] string q)
{
    var posts = await _context.Posts
        .Where(p => !p.IsDraft && (p.Title.Contains(q) || p.Content.Contains(q)))
        .OrderByDescending(p => p.PublishedAt)
        .ToListAsync();

    return Ok(posts);
}
```

### 8. Add HTTPS in Production

Update `compose.yaml` for production:
```yaml
environment:
  - ASPNETCORE_URLS=https://+:443;http://+:80
  - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/cert.pfx
  - ASPNETCORE_Kestrel__Certificates__Default__Password=YourPassword
```

---

## 📝 Database Migrations

### Creating Migrations

Whenever you modify your models:

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project StarApi

# Apply migrations
dotnet ef database update --project StarApi

# Rollback to previous migration
dotnet ef database update PreviousMigrationName --project StarApi

# Remove last migration (if not applied)
dotnet ef migrations remove --project StarApi
```

---

## 🧪 Testing

### Install Testing Packages

```bash
dotnet add StarApi.Tests package xUnit
dotnet add StarApi.Tests package Moq
dotnet add StarApi.Tests package Microsoft.AspNetCore.Mvc.Testing
```

### Example Unit Test

```csharp
public class PostsControllerTests
{
    [Fact]
    public async Task GetPosts_ReturnsOkResult()
    {
        // Arrange
        var mockContext = new Mock<ApplicationDbContext>();
        var controller = new PostsController(mockContext.Object, Mock.Of<ILogger<PostsController>>());

        // Act
        var result = await controller.GetPosts();

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }
}
```

---

## 🚢 Deployment

### Deploy to Production

1. **Update `appsettings.Production.json`:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=your-prod-db;Database=starblog;Username=xxx;Password=xxx"
     }
   }
   ```

2. **Build Docker Image:**
   ```bash
   docker build -t starapi:latest -f StarApi/Dockerfile .
   ```

3. **Push to Registry:**
   ```bash
   docker tag starapi:latest your-registry/starapi:latest
   docker push your-registry/starapi:latest
   ```

4. **Deploy using Docker Compose or Kubernetes**

---

## 📚 Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Docker Compose](https://docs.docker.com/compose/)
- [Swagger/OpenAPI](https://swagger.io/docs/)

---

## ✅ Quick Checklist

- [ ] Start the application (`docker compose up -d`)
- [ ] Test endpoints via Swagger UI
- [ ] Configure API key for authentication
- [ ] Create some test posts via API
- [ ] Verify database has data (`docker exec -it starapi-db psql -U postgres -d starblog`)
- [ ] Set up Next.js frontend project
- [ ] Integrate API calls in Next.js
- [ ] Add DTOs and validation
- [ ] Implement pagination
- [ ] Add search functionality
- [ ] Write unit tests
- [ ] Deploy to production

---

## 🆘 Need Help?

Check these files for reference:
- `DATABASE_SETUP.md` - Database configuration details
- `README.md` - Project overview
- `StarApi/appsettings.json` - Configuration settings
- `StarApi/Program.cs` - Application startup
- `compose.yaml` - Docker configuration

Happy coding! 🚀

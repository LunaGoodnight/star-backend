# Star API - Blog Backend

A .NET 9 blog backend API with PostgreSQL database, designed to run in Docker containers.

## Features

- ✅ RESTful API for blog posts
- ✅ Draft/Published post support
- ✅ API Key authentication for admin operations
- ✅ PostgreSQL database with auto-migrations
- ✅ Docker & Docker Compose setup
- ✅ Swagger API documentation

## API Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/posts` | List all posts (public: published only, admin: all posts) | No |
| GET | `/api/posts/{id}` | Get one post by ID | No |
| POST | `/api/posts` | Create new post | Yes (Admin) |
| PUT | `/api/posts/{id}` | Update post | Yes (Admin) |
| DELETE | `/api/posts/{id}` | Delete post | Yes (Admin) |

## Post Model

```json
{
  "id": 1,
  "title": "Post Title",
  "content": "Post content...",
  "isDraft": true,
  "createdAt": "2025-10-05T00:00:00Z",
  "updatedAt": "2025-10-05T00:00:00Z",
  "publishedAt": null
}
```

## Setup & Running

### 1. Configure Environment

```bash
# Create .env file from example
cp .env.example .env

# Edit .env and set your API key
# API_KEY=your-secret-api-key-here
```

### 2. Start Services

```bash
# Build and start containers
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Stop and remove volumes (deletes database)
docker-compose down -v
```

### 3. Access API

- **API Base URL**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **PostgreSQL**: localhost:5432

## Authentication

Admin operations (POST, PUT, DELETE) require API Key authentication.

Include the API key in request headers:

```bash
X-API-Key: your-secret-api-key-here
```

### Example cURL Requests

**Get all posts (public)**
```bash
curl http://localhost:8080/api/posts
```

**Create a post (admin)**
```bash
curl -X POST http://localhost:8080/api/posts \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-secret-api-key-here" \
  -d '{
    "title": "My First Post",
    "content": "Hello World!",
    "isDraft": false
  }'
```

**Update a post (admin)**
```bash
curl -X PUT http://localhost:8080/api/posts/1 \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-secret-api-key-here" \
  -d '{
    "id": 1,
    "title": "Updated Title",
    "content": "Updated content",
    "isDraft": false
  }'
```

**Delete a post (admin)**
```bash
curl -X DELETE http://localhost:8080/api/posts/1 \
  -H "X-API-Key: your-secret-api-key-here"
```

## Draft System

- New posts default to `isDraft: true`
- Public users can only see posts where `isDraft: false`
- Admin users (authenticated) can see all posts including drafts
- When a post is published (`isDraft: false`), `publishedAt` timestamp is automatically set

## Database

- **Engine**: PostgreSQL 16
- **Database Name**: starblog
- **User**: postgres
- **Password**: postgres (change in production!)
- **Data Persistence**: Data is stored in Docker volume `postgres_data`

## Project Structure

```
StarApi/
├── Controllers/
│   └── PostsController.cs      # API endpoints
├── Data/
│   └── ApplicationDbContext.cs # EF Core DbContext
├── Middleware/
│   └── ApiKeyAuthenticationMiddleware.cs # API Key auth
├── Models/
│   └── Post.cs                 # Post entity
├── Program.cs                  # App configuration
├── appsettings.json           # Configuration
├── Dockerfile                 # Docker image
└── StarApi.csproj            # Project file

compose.yaml                   # Docker Compose config
.env.example                  # Environment variables template
README.md                     # This file
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Port=5432;Database=starblog;Username=postgres;Password=postgres"
  },
  "ApiKey": "your-api-key-here-change-in-production"
}
```

### Environment Variables

Environment variables override appsettings.json:

```bash
ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=starblog;Username=postgres;Password=postgres
ApiKey=your-secret-api-key
```

## Deployment to VPS

1. **Copy files to VPS**
```bash
scp -r StarApi user@your-vps:/path/to/deployment
```

2. **Set environment variables**
```bash
# Create .env file with production values
API_KEY=your-production-api-key
```

3. **Run with Docker Compose**
```bash
docker-compose up -d
```

4. **Configure reverse proxy (optional)**
Use nginx or traefik to expose the API with a domain name and HTTPS.

## Security Notes

⚠️ **Before deploying to production:**

1. Change the default API key in `.env`
2. Change PostgreSQL password in `compose.yaml`
3. Use environment variables instead of hardcoded secrets
4. Consider using HTTPS with a reverse proxy
5. Implement rate limiting
6. Add input validation

## Tech Stack

- **.NET 9** - Framework
- **ASP.NET Core** - Web API
- **Entity Framework Core** - ORM
- **PostgreSQL** - Database
- **Docker** - Containerization
- **Swagger/OpenAPI** - API Documentation

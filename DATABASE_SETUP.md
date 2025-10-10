# Database Setup and Creation

This document explains how and where the database is created in the StarApi project.

## Database Creation Flow

The database is **automatically created and migrated** when the application starts. This happens through Entity Framework Core migrations.

---

## 1. Database Configuration

### Location: `StarApi/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Port=5432;Database=starblog;Username=postgres;Password=postgres"
  }
}
```

**Database Details:**
- **Type**: PostgreSQL
- **Host**: `db` (Docker service name)
- **Port**: `5432`
- **Database Name**: `starblog`
- **Username**: `postgres`
- **Password**: `postgres`

---

## 2. DbContext Registration

### Location: `StarApi/Program.cs:13-15`

```csharp
// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
```

This registers the `ApplicationDbContext` with PostgreSQL (Npgsql) provider.

---

## 3. Automatic Migration on Startup

### Location: `StarApi/Program.cs:34-39`

```csharp
// Run migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
```

**What `db.Database.Migrate()` does:**
1. Checks if the database exists
2. Creates the database if it doesn't exist
3. Applies all pending migrations to update the schema
4. Ensures the database schema matches your models

---

## 4. Database Schema Definition

### Location: `StarApi/Data/ApplicationDbContext.cs`

```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Post> Posts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.IsDraft).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
```

**Tables Created:**
- `Posts` table with the following columns:
  - `Id` (int, Primary Key, Auto-increment)
  - `Title` (varchar(200), Required)
  - `Content` (text, Required)
  - `IsDraft` (boolean, Default: true)
  - `CreatedAt` (timestamp, Default: CURRENT_TIMESTAMP)
  - `UpdatedAt` (timestamp, Default: CURRENT_TIMESTAMP)
  - `PublishedAt` (timestamp, Nullable)

---

## 5. Docker Database Setup

### Location: `compose.yaml`

```yaml
services:
  db:
    image: postgres:16-alpine
    container_name: starapi-db
    environment:
      POSTGRES_DB: starblog
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
```

**Docker Configuration:**
- Uses PostgreSQL 16 Alpine image
- Persists data in `postgres_data` volume
- Exposes port 5432 for external connections

---

## Working with Migrations

### Create a New Migration

When you modify your models, create a new migration:

```bash
dotnet ef migrations add MigrationName --project StarApi
```

### Apply Migrations Manually

```bash
dotnet ef database update --project StarApi
```

### Remove Last Migration

```bash
dotnet ef migrations remove --project StarApi
```

### View Migration SQL

```bash
dotnet ef migrations script --project StarApi
```

---

## Database Creation Timeline

1. **Application Starts** → `Program.cs` executes
2. **Services Configured** → DbContext registered with connection string
3. **App Built** → Middleware pipeline configured
4. **Migration Scope Created** → Service scope for database operations
5. **`db.Database.Migrate()` Called** → Database created and schema applied
6. **Application Ready** → API endpoints accessible

---

## Important Notes

- ✅ Database is created **automatically on first run**
- ✅ Migrations run **automatically on every startup**
- ✅ No manual database creation needed
- ⚠️ Ensure PostgreSQL is running before starting the application
- ⚠️ Connection string must be correct in configuration
- ⚠️ Initial migration must be created first: `dotnet ef migrations add InitialCreate`

---

## Troubleshooting

### Database Not Created?

1. Check if PostgreSQL is running:
   ```bash
   docker compose ps
   ```

2. Verify connection string in `appsettings.json`

3. Check if migrations exist:
   ```bash
   ls StarApi/Migrations
   ```

4. Create initial migration if missing:
   ```bash
   dotnet ef migrations add InitialCreate --project StarApi
   ```

### Connection Failed?

- Ensure PostgreSQL container is healthy
- Check port 5432 is not in use by another process
- Verify credentials match between `appsettings.json` and `compose.yaml`

---

## Summary

The database creation in StarApi is **fully automated** through:
1. Entity Framework Core migrations
2. Automatic migration on startup (`Program.cs:38`)
3. Docker Compose for PostgreSQL setup

You simply need to:
1. Create migrations when models change
2. Run `docker compose up` or `dotnet run`
3. Database is created and ready!

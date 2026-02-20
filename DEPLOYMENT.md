# StarApi Deployment Guide

This guide provides instructions for deploying StarApi to your VPS without port conflicts.

---

## 🔌 Port Configuration (VPS Safe)

Your StarApi has been configured to avoid conflicts with existing services on your VPS.

### Current VPS Port Usage

| Service | Port | Container Name |
|---------|------|----------------|
| g13-ocean (Next.js) | 3000 | g13-ocean_web |
| meme (Next.js) | 3001 | meme_meme_1 |
| mock-socket | 1024 | mock-socket_mock-socket_1 |
| meme-backend API | 5001 | meme-backend_api_1 |
| g13-api | 5188 | g13-api_app_1 |
| meme MySQL | 3307 | meme-backend_db_1 |

### StarApi Port Configuration (No Conflicts)

| Service | Host Port | Container Port | Container Name |
|---------|-----------|----------------|----------------|
| **StarApi API** | **5002** | 8080 | starapi |
| **PostgreSQL** | **5433** | 5432 | starapi-db |

**Access URLs:**
- API: `http://your-vps-ip:5002`
- Swagger: `http://your-vps-ip:5002/swagger`
- PostgreSQL: `your-vps-ip:5433`

---

## 🚀 Deployment Steps

### 1. Prepare Your VPS

Connect to your VPS:
```bash
ssh root@your-vps-ip
```

Create project directory:
```bash
mkdir -p /opt/star-api
cd /opt/star-api
```

### 2. Transfer Files to VPS

From your local machine:
```bash
# Using SCP
scp -r D:/side-project/star-api/StarApi/* root@your-vps-ip:/opt/star-api/

# Or using rsync (recommended)
rsync -avz --exclude 'bin' --exclude 'obj' --exclude 'node_modules' \
  D:/side-project/star-api/StarApi/ root@your-vps-ip:/opt/star-api/
```

### 3. Configure Environment Variables

Create `.env` file on VPS:
```bash
cd /opt/star-api
nano .env
```

Add the following content:
```env
# Production API Key (generate a secure one)
API_KEY=your-secure-production-api-key-here

# Optional: Database credentials
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your-secure-db-password
POSTGRES_DB=starblog
```

**Generate a secure API key:**
```bash
# On your VPS (if openssl is available)
openssl rand -base64 32
```

### 4. Build and Start Services

```bash
cd /opt/star-api

# Build and start in detached mode
docker compose up -d --build

# View logs
docker compose logs -f

# Check status
docker compose ps
```

Expected output:
```
NAME                IMAGE               STATUS              PORTS
starapi             starapi             Up                  0.0.0.0:5002->8080/tcp
starapi-db          postgres:16-alpine  Up (healthy)        0.0.0.0:5433->5432/tcp
```

### 5. Verify Deployment

Test the API:
```bash
# Check if API is responding
curl http://localhost:5002/api/posts

# Check Swagger UI
curl http://localhost:5002/swagger/index.html
```

From your browser:
```
http://your-vps-ip:5002/swagger
```

### 6. Test Database Connection

```bash
# Connect to PostgreSQL
docker exec -it starapi-db psql -U postgres -d starblog

# Inside PostgreSQL shell:
\dt                    # List tables
SELECT * FROM "Posts"; # Query posts
\q                     # Exit
```

---

## 🔧 Docker Compose Configuration

Your `compose.yaml` is configured with:

```yaml
services:
  db:
    image: postgres:16-alpine
    container_name: starapi-db
    restart: unless-stopped
    ports:
      - "5433:5432"  # External:Internal
    networks:
      - starapi-network

  starapi:
    image: starapi
    container_name: starapi
    restart: unless-stopped
    ports:
      - "5002:8080"  # External:Internal
    networks:
      - starapi-network
    environment:
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=starblog;Username=postgres;Password=postgres

networks:
  starapi-network:
    driver: bridge
```

**Key Features:**
- ✅ Isolated network (`starapi-network`)
- ✅ Auto-restart (`restart: unless-stopped`)
- ✅ Health checks for database
- ✅ Persistent data with volumes

---

## 🛡️ Security Recommendations

### 1. Configure Firewall (UFW)

```bash
# Allow SSH (if not already)
ufw allow 22/tcp

# Allow StarApi API
ufw allow 5002/tcp

# Enable firewall
ufw enable
```

### 2. Update API Key

Edit `.env` file:
```bash
nano /opt/star-api/.env
```

Change `API_KEY` to a strong, random value.

### 3. Database Password

For production, change the PostgreSQL password:

Update `compose.yaml`:
```yaml
environment:
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-postgres}
```

Update `.env`:
```env
POSTGRES_PASSWORD=your-secure-password-here
```

Update connection string in `compose.yaml`:
```yaml
- ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=starblog;Username=postgres;Password=${POSTGRES_PASSWORD:-postgres}
```

Restart services:
```bash
docker compose down
docker compose up -d
```

### 4. HTTPS/SSL (Optional but Recommended)

Use Nginx as reverse proxy with Let's Encrypt:

```nginx
# /etc/nginx/sites-available/starapi
server {
    listen 80;
    server_name api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5002;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable site and get SSL:
```bash
ln -s /etc/nginx/sites-available/starapi /etc/nginx/sites-enabled/
certbot --nginx -d api.yourdomain.com
nginx -t && nginx -s reload
```

---

## 📊 Monitoring and Maintenance

### View Logs

```bash
# All logs
docker compose logs -f

# API logs only
docker compose logs -f starapi

# Database logs only
docker compose logs -f db

# Last 100 lines
docker compose logs --tail=100 starapi
```

### Container Management

```bash
# Check status
docker compose ps

# Restart services
docker compose restart

# Stop services
docker compose stop

# Start services
docker compose start

# Rebuild and restart
docker compose up -d --build

# Remove everything (including volumes)
docker compose down -v
```

### Database Backup

```bash
# Backup database
docker exec starapi-db pg_dump -U postgres starblog > backup_$(date +%Y%m%d).sql

# Restore database
cat backup_20250101.sql | docker exec -i starapi-db psql -U postgres starblog
```

### Disk Space Management

```bash
# Check disk usage
df -h

# Clean up Docker
docker system prune -a

# Clean up old images
docker image prune -a
```

---

## 🔄 Updating the Application

### 1. Pull Latest Code

```bash
cd /opt/star-api
git pull origin main  # If using git

# Or transfer new files via rsync/scp
```

### 2. Rebuild and Deploy

```bash
# Stop current services
docker compose down

# Rebuild with latest code
docker compose up -d --build

# Verify
docker compose ps
docker compose logs -f starapi
```

### 3. Database Migrations

See the detailed [Migration Workflow](#-migration-workflow) section below.

---

## 🧪 Testing Your Deployment

### 1. Health Check

```bash
curl http://localhost:5002/api/posts
```

Expected: `[]` or list of posts

### 2. Create a Test Post

```bash
curl -X POST http://localhost:5002/api/posts \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: your-api-key-here" \
  -d '{
    "title": "Test Post",
    "content": "Testing deployment",
    "isDraft": false
  }'
```

### 3. Retrieve Posts

```bash
curl http://localhost:5002/api/posts
```

### 4. Check Swagger UI

Open in browser:
```
http://your-vps-ip:5002/swagger
```

---

## 🌐 Connecting Your Next.js Frontend

Update your Next.js `.env` or `.env.local`:

```env
# Production
NEXT_PUBLIC_API_URL=http://your-vps-ip:5002

# With domain + HTTPS (recommended)
NEXT_PUBLIC_API_URL=https://api.yourdomain.com

# API Key (for admin operations)
API_KEY=your-secure-api-key
```

Example API integration:
```typescript
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5002';

export async function getPosts() {
  const res = await fetch(`${API_BASE_URL}/api/posts`);
  return res.json();
}
```

---

## ⚠️ Troubleshooting

### Port Already in Use

If you get port conflict errors:

```bash
# Check what's using the port
netstat -tulpn | grep :5002

# Or using lsof
lsof -i :5002

# Kill the process if needed
kill -9 <PID>
```

### Database Connection Failed

```bash
# Check if PostgreSQL is healthy
docker compose ps

# Check database logs
docker compose logs db

# Restart database
docker compose restart db
```

### Container Won't Start

```bash
# Check logs
docker compose logs starapi

# Check if image built correctly
docker images | grep starapi

# Rebuild from scratch
docker compose down
docker compose build --no-cache
docker compose up -d
```

### Migration Errors

```bash
# Access container and check migrations
docker exec -it starapi bash

# Inside container
ls -la /app/Migrations
```

---

## 📋 Quick Reference

### Common Commands

```bash
# Start services
docker compose up -d

# Stop services
docker compose down

# View logs
docker compose logs -f

# Restart API only
docker compose restart starapi

# Check status
docker compose ps

# Execute commands in container
docker exec -it starapi bash
docker exec -it starapi-db psql -U postgres -d starblog
```

### Service URLs

| Service | URL | Usage |
|---------|-----|-------|
| API | `http://your-vps-ip:5002` | API endpoints |
| Swagger | `http://your-vps-ip:5002/swagger` | API documentation |
| PostgreSQL | `your-vps-ip:5433` | Database connection |

---

## ✅ Deployment Checklist

- [ ] Transfer files to VPS (`/opt/star-api`)
- [ ] Create `.env` file with secure API key
- [ ] Build Docker images (`docker compose build`)
- [ ] Start services (`docker compose up -d`)
- [ ] Verify containers are running (`docker compose ps`)
- [ ] Check logs for errors (`docker compose logs`)
- [ ] Test API endpoint (`curl http://localhost:5002/api/posts`)
- [ ] Access Swagger UI in browser
- [ ] Configure firewall rules (port 5002)
- [ ] Set up HTTPS with Nginx + Let's Encrypt (recommended)
- [ ] Update Next.js frontend API URL
- [ ] Create database backup schedule
- [ ] Monitor logs and performance

---

## 🔄 Migration Workflow

This section explains how to deploy code changes with database migrations step by step.

### Overview

```
Local: Code Changes → Create Migration → Test → Commit & Push
  ↓
VPS: Git Pull → Docker Rebuild → Apply Migration → Restart → Verify
```

---

### Step 1: Make Code Changes (Local)

Edit your models, add new entities, etc.

**Why:** Development happens locally where you have IDE support, debugging, and fast iteration.

---

### Step 2: Create Migration (Local)

```bash
cd StarApi
dotnet ef migrations add <MigrationName>
```

**Example:**
```bash
dotnet ef migrations add AddCategory
```

**Why:**
- EF Core migrations track database schema changes as code files
- Version-controlled = you can see what changed and when
- Creating locally ensures it compiles before deploying

**Files created:**
```
Migrations/
├── 20260220120953_AddCategory.cs           # Up/Down migration logic
└── 20260220120953_AddCategory.Designer.cs  # Metadata snapshot
```

---

### Step 3: Test Locally (Recommended)

```bash
dotnet ef database update
dotnet run
```

**Why:**
- Verify migration applies without errors
- Confirm your app works with the new schema
- Catch issues before they hit production

---

### Step 4: Commit & Push

```bash
git add .
git commit -m "Add Category model with Post relationship"
git push origin main
```

**Why:**
- Git is your source of truth
- Migration files MUST be committed (VPS needs them)
- Good commit messages = documentation

---

### Step 5: Pull on VPS

```bash
ssh user@your-vps
cd /opt/star-api
git pull origin main
```

**Why:** Sync VPS with latest code including new migration files.

---

### Step 6: Rebuild Docker Image

```bash
docker compose build --no-cache
```

**Why:**
- `--no-cache` = fresh build with all new code
- Without rebuilding, container runs OLD code
- New image contains your updated application

**Faster alternative (uses cache):**
```bash
docker compose build
```

---

### Step 7: Apply Migration

#### Option A: Temporary Container (Recommended)

```bash
docker run --rm \
  --network starapi_starapi-network \
  -v $(pwd):/src \
  -w /src/StarApi \
  -e ConnectionStrings__DefaultConnection="Host=starapi-db;Port=5432;Database=starblog;Username=postgres;Password=YOUR_PASSWORD" \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet ef database update
```

**Why this approach:**
| Flag | Purpose |
|------|---------|
| `--rm` | Remove container after completion (clean) |
| `--network` | Connect to same network as your database |
| `-v $(pwd):/src` | Mount code so it can read migration files |
| SDK image | Has `dotnet ef` tools (runtime image doesn't) |

**Benefits:**
- Runs once and exits
- Doesn't bloat production image
- Full control over when migrations run

#### Option B: Auto-Migration at Startup

Add to `Program.cs` before `app.Run()`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
```

**Why this approach:**
- Zero manual steps
- Migrations apply automatically on every startup
- Good for simple deployments

**Trade-offs:**
- Slower startup time
- If migration fails, app won't start
- Less control (can't preview changes)

---

### Step 8: Restart Application

```bash
docker compose up -d
```

**Why:**
- `-d` = detached mode (runs in background)
- Recreates containers with new image
- If containers already running, this replaces them

**Alternative (explicit stop first):**
```bash
docker compose down
docker compose up -d
```

---

### Step 9: Verify

```bash
# Check containers are running
docker compose ps

# Check for errors in logs
docker compose logs -f starapi

# Test API endpoint
curl http://localhost:5002/api/posts
```

**Why:** Confirm:
- Containers running (not restarting in a loop)
- No errors in logs
- API responds correctly

---

### Quick Copy-Paste Commands

**On Local Machine:**
```bash
# 1. Create migration
dotnet ef migrations add AddCategory

# 2. Commit and push
git add .
git commit -m "Add Category model"
git push
```

**On VPS:**
```bash
# 1. Pull code
cd /opt/star-api
git pull

# 2. Rebuild
docker compose build --no-cache

# 3. Apply migration
docker run --rm \
  --network starapi_starapi-network \
  -v $(pwd):/src \
  -w /src/StarApi \
  -e ConnectionStrings__DefaultConnection="Host=starapi-db;Port=5432;Database=starblog;Username=postgres;Password=YOUR_PASSWORD" \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet ef database update

# 4. Restart
docker compose down && docker compose up -d

# 5. Verify
docker compose logs -f starapi
```

---

### Rollback a Migration

If something goes wrong:

```bash
# Rollback to previous migration
dotnet ef database update <PreviousMigrationName>

# Remove last migration file (only if NOT applied to production)
dotnet ef migrations remove
```

---

### Best Practices

| Practice | Reason |
|----------|--------|
| Test migrations locally first | Catch errors before production |
| Backup database before migrating | Safety net for rollback |
| Use meaningful names | `AddCategory` not `Migration1` |
| Never edit applied migrations | Creates inconsistency between environments |
| One feature per migration | Easier to debug and rollback |

---

## 🆘 Need Help?

- Check logs: `docker compose logs -f`
- Verify ports: `netstat -tulpn | grep -E '5002|5433'`
- Check containers: `docker compose ps`
- Review configuration: `cat compose.yaml`

Your StarApi is now ready for deployment without any port conflicts! 🚀

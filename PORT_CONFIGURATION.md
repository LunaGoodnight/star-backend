# StarApi - Port Configuration Guide

This document explains the port configuration for StarApi to ensure no conflicts with existing VPS services.

---

## 🔍 VPS Port Analysis

### Existing Services on Your VPS

Based on your current `docker ps` output, these ports are already in use:

| Port | Service | Container Name | Description |
|------|---------|----------------|-------------|
| **1024** | mock-socket | mock-socket_mock-socket_1 | WebSocket mock server |
| **3000** | g13-ocean | g13-ocean_web_1 | Next.js frontend (g13 project) |
| **3001** | meme | meme_meme_1 | Next.js frontend (meme project) |
| **3307** | meme MySQL | meme-backend_db_1 | MySQL database for meme backend |
| **5001** | meme-backend API | meme-backend_api_1 | .NET Core API (meme project) |
| **5188** | g13-api | g13-api_app_1 | API service (g13 project) |

### Ports to Avoid

❌ **Do NOT use these ports:**
- 1024 (mock-socket)
- 3000 (g13-ocean frontend)
- 3001 (meme frontend)
- 3306, 3307 (MySQL databases)
- 5000, 5001 (meme backend API)
- 5188 (g13-api)
- 5432 (default PostgreSQL - may conflict with system services)

---

## ✅ StarApi Port Configuration (Conflict-Free)

### Assigned Ports

| Service | Host Port | Container Port | Container Name | Status |
|---------|-----------|----------------|----------------|--------|
| **StarApi API** | **5002** | 8080 | starapi | ✅ Safe |
| **PostgreSQL Database** | **5433** | 5432 | starapi-db | ✅ Safe |

### Why These Ports?

1. **Port 5002** for API:
   - Avoids 5001 (meme-backend)
   - Avoids 5188 (g13-api)
   - Sequential numbering for easy memory (5001, 5002)
   - Commonly used for .NET APIs

2. **Port 5433** for PostgreSQL:
   - Avoids default 5432 (prevents system conflicts)
   - Avoids 3307 (MySQL)
   - Standard alternative PostgreSQL port

---

## 🗺️ Complete VPS Port Map (After Deployment)

```
┌─────────────────────────────────────────────────────────┐
│              Your VPS Port Layout                        │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  Port 1024  ─→  mock-socket (WebSocket)                │
│  Port 3000  ─→  g13-ocean (Next.js)                    │
│  Port 3001  ─→  meme (Next.js)                         │
│  Port 3307  ─→  meme-backend (MySQL)                   │
│  Port 5001  ─→  meme-backend (API)                     │
│  Port 5002  ─→  StarApi (API)          ← NEW ✨       │
│  Port 5188  ─→  g13-api (API)                          │
│  Port 5433  ─→  StarApi (PostgreSQL)   ← NEW ✨       │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### By Service Type

**Frontend Services:**
```
3000 → g13-ocean (Next.js)
3001 → meme (Next.js)
```

**Backend APIs:**
```
5001 → meme-backend (.NET)
5002 → StarApi (.NET)          ← NEW
5188 → g13-api
```

**Databases:**
```
3307 → MySQL (meme)
5433 → PostgreSQL (StarApi)    ← NEW
```

**Other Services:**
```
1024 → mock-socket
```

---

## 📝 Configuration Files

### compose.yaml

The Docker Compose configuration has been updated with conflict-free ports:

```yaml
services:
  db:
    image: postgres:16-alpine
    container_name: starapi-db
    restart: unless-stopped
    ports:
      - "5433:5432"  # Host:Container (5433 external, 5432 internal)
    networks:
      - starapi-network

  starapi:
    image: starapi
    container_name: starapi
    restart: unless-stopped
    ports:
      - "5002:8080"  # Host:Container (5002 external, 8080 internal)
    depends_on:
      db:
        condition: service_healthy
    networks:
      - starapi-network

networks:
  starapi-network:
    driver: bridge
```

**Key Features:**
- ✅ Isolated network for security
- ✅ Auto-restart on failure
- ✅ Health checks for database
- ✅ No port conflicts with existing services

---

## 🌐 Access URLs

### Development (Local)

When running locally:
```
API:          http://localhost:5002
Swagger UI:   http://localhost:5002/swagger
PostgreSQL:   localhost:5433
```

### Production (VPS)

After deploying to your VPS:
```
API:          http://your-vps-ip:5002
Swagger UI:   http://your-vps-ip:5002/swagger
PostgreSQL:   your-vps-ip:5433 (for external connections)
```

### With Domain + HTTPS (Recommended)

After setting up Nginx reverse proxy:
```
API:          https://api.yourdomain.com
Swagger UI:   https://api.yourdomain.com/swagger
```

---

## 🚀 Quick Start

### Local Development

```bash
# Start all services
docker compose up -d

# Check status
docker compose ps

# View logs
docker compose logs -f

# Access API
curl http://localhost:5002/api/posts

# Open Swagger UI
# Navigate to: http://localhost:5002/swagger
```

### VPS Deployment

```bash
# 1. Transfer files to VPS
rsync -avz --exclude 'bin' --exclude 'obj' \
  StarApi/ root@your-vps-ip:/opt/star-api/

# 2. SSH to VPS
ssh root@your-vps-ip

# 3. Navigate to project
cd /opt/star-api

# 4. Create .env file (important!)
nano .env
# Add: API_KEY=your-secure-api-key

# 5. Start services
docker compose up -d --build

# 6. Verify deployment
docker compose ps
curl http://localhost:5002/api/posts

# 7. Configure firewall
ufw allow 5002/tcp
```

---

## 🔒 Security Considerations

### 1. Firewall Configuration

Only expose necessary ports:

```bash
# Check current firewall rules
ufw status

# Allow StarApi API port
ufw allow 5002/tcp

# PostgreSQL port (only if external access needed)
# ufw allow 5433/tcp

# Enable firewall
ufw enable
```

### 2. Database Access

**Internal Access (Recommended):**
- Containers communicate via Docker network
- PostgreSQL not exposed to internet
- More secure

**External Access (If Needed):**
```bash
# Allow specific IP only
ufw allow from YOUR_IP to any port 5433

# Connect from external
psql -h your-vps-ip -p 5433 -U postgres -d starblog
```

### 3. API Key Protection

```bash
# Generate secure API key
openssl rand -base64 32

# Add to .env (never commit to git)
echo "API_KEY=$(openssl rand -base64 32)" >> .env
```

### 4. HTTPS Setup (Production)

Use Nginx as reverse proxy:

```nginx
# /etc/nginx/sites-available/starapi
server {
    listen 80;
    server_name api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5002;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Get SSL certificate:
```bash
certbot --nginx -d api.yourdomain.com
```

---

## 🔍 Verifying No Conflicts

### Before Deployment

Check if ports are available on your VPS:

```bash
# Check if port 5002 is free
netstat -tulpn | grep :5002
lsof -i :5002

# Check if port 5433 is free
netstat -tulpn | grep :5433
lsof -i :5433

# Both should return nothing (ports are free)
```

### After Deployment

Verify services are running:

```bash
# Check all running containers
docker ps

# Should show:
# starapi       - 0.0.0.0:5002->8080/tcp
# starapi-db    - 0.0.0.0:5433->5432/tcp

# Verify no conflicts
netstat -tulpn | grep -E '5002|5433'
```

### Test All Services

```bash
# Test StarApi
curl http://localhost:5002/api/posts

# Test existing services (should still work)
curl http://localhost:3000  # g13-ocean
curl http://localhost:3001  # meme
curl http://localhost:5001  # meme-backend
curl http://localhost:5188  # g13-api
```

---

## 🛠️ Troubleshooting

### Port Conflict Detected

If you still get port conflicts:

```bash
# Find what's using the port
netstat -tulpn | grep :5002

# Kill the process
kill -9 <PID>

# Or use a different port
# Edit compose.yaml and change "5002:8080" to "5003:8080"
```

### Service Won't Start

```bash
# Check logs
docker compose logs starapi
docker compose logs db

# Restart services
docker compose restart

# Complete rebuild
docker compose down
docker compose up -d --build
```

### Can't Connect to Database

```bash
# Check if database is healthy
docker compose ps

# Should show:
# starapi-db    Up (healthy)

# Check database logs
docker compose logs db

# Test connection
docker exec -it starapi-db psql -U postgres -d starblog
```

---

## 📊 Port Usage Summary

### StarApi Uses Only 2 Ports

✅ **Port 5002** - HTTP API
- API endpoints: `/api/posts`, etc.
- Swagger UI: `/swagger`
- Health checks

✅ **Port 5433** - PostgreSQL Database
- Internal container communication
- External connections (if needed)
- Database backups

### Total VPS Ports After Deployment

| Total Ports | Before | After | Added |
|-------------|--------|-------|-------|
| In Use      | 6      | 8     | +2    |

**Your VPS can easily handle this!** Most VPS instances support thousands of concurrent connections.

---

## 🔗 Integration with Next.js Frontend

Update your Next.js environment variables:

### Development (.env.local)

```env
NEXT_PUBLIC_API_URL=http://localhost:5002
API_KEY=your-api-key-here
```

### Production (.env.production)

```env
# With IP
NEXT_PUBLIC_API_URL=http://your-vps-ip:5002

# With domain (recommended)
NEXT_PUBLIC_API_URL=https://api.yourdomain.com

# API Key (for admin operations)
API_KEY=your-production-api-key
```

### API Client Example

```typescript
// lib/api.ts
const API_URL = process.env.NEXT_PUBLIC_API_URL;

export async function getPosts() {
  const res = await fetch(`${API_URL}/api/posts`);
  return res.json();
}

export async function createPost(post: any) {
  const res = await fetch(`${API_URL}/api/posts`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': process.env.API_KEY || '',
    },
    body: JSON.stringify(post),
  });
  return res.json();
}
```

---

## ✅ Pre-Deployment Checklist

Before deploying to your VPS:

- [ ] Verify ports 5002 and 5433 are not in use
- [ ] Update `.env` file with secure API key
- [ ] Test locally with `docker compose up`
- [ ] Review `compose.yaml` configuration
- [ ] Ensure `DEPLOYMENT.md` steps are clear
- [ ] Backup existing VPS services (optional)
- [ ] Plan for HTTPS setup (optional)

During deployment:

- [ ] Transfer files to `/opt/star-api`
- [ ] Create `.env` file on VPS
- [ ] Run `docker compose up -d --build`
- [ ] Check logs for errors
- [ ] Test API endpoints
- [ ] Configure firewall rules
- [ ] Update Next.js frontend API URL

After deployment:

- [ ] Verify all services still working
- [ ] Test StarApi endpoints
- [ ] Monitor logs for issues
- [ ] Set up database backup schedule
- [ ] Document any custom configurations

---

## 📞 Support & References

### Related Documentation

- `DATABASE_SETUP.md` - Database creation and configuration
- `DEPLOYMENT.md` - Complete deployment guide
- `NEXT_STEPS.md` - Development roadmap
- `README.md` - Project overview

### Useful Commands

```bash
# Check all ports in use
netstat -tulpn

# Check specific port
lsof -i :5002

# Docker commands
docker compose ps
docker compose logs -f
docker compose restart
docker compose down
docker compose up -d

# Database backup
docker exec starapi-db pg_dump -U postgres starblog > backup.sql
```

---

## 🎉 Summary

Your StarApi is configured with:

✅ **No port conflicts** with existing VPS services
✅ **Ports 5002** (API) and **5433** (PostgreSQL)
✅ **Isolated Docker network** for security
✅ **Auto-restart** on failure
✅ **Health checks** for reliability
✅ **Ready for production** deployment

You can now safely deploy to your VPS without affecting your existing services! 🚀

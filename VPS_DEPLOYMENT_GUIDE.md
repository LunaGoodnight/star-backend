# VPS Deployment Guide - StarApi

This guide will walk you through deploying your StarApi application to a VPS (Virtual Private Server) with Docker, Nginx, and SSL/HTTPS support.

---

## Prerequisites

- A VPS with Ubuntu 20.04+ or Debian 11+
- Root or sudo access to the VPS
- A domain name pointed to your VPS IP address
- SSH access to your VPS

---

## Step 1: Install Required Software on VPS

Connect to your VPS via SSH:

```bash
ssh user@your-vps-ip
```

### Install Docker

```bash
# Download Docker installation script
curl -fsSL https://get.docker.com -o get-docker.sh

# Run the installation script
sudo sh get-docker.sh

# Add your user to docker group (optional, to run docker without sudo)
sudo usermod -aG docker $USER

# Log out and back in for group changes to take effect
```

### Install Docker Compose

```bash
# Update package list
sudo apt update

# Install Docker Compose
sudo apt install docker-compose -y
```

### Verify Installation

```bash
# Check Docker version
docker --version

# Check Docker Compose version
docker-compose --version
```

Expected output:
```
Docker version 24.x.x
docker-compose version 1.29.x
```

---

## Step 2: Upload Your Project to VPS

### Option A: Using Git (Recommended)

```bash
# Navigate to your preferred directory
cd /home/your-user/

# Clone your repository
git clone https://github.com/your-username/star-api.git

# Navigate to project directory
cd star-api/StarApi
```

### Option B: Using SCP/SFTP

From your **local machine** (Windows):

```bash
# Using SCP
scp -r D:\side-project\star-api\StarApi user@your-vps-ip:/home/your-user/

# Or use an SFTP client like FileZilla, WinSCP
```

---

## Step 3: Create Environment Variables File

Create the `.env` file on your VPS:

```bash
# Navigate to project directory
cd /home/your-user/star-api/StarApi

# Create .env file
nano .env
```

Paste the following content:

```bash
# Database credentials
POSTGRES_PASSWORD=JTv12ZVMXiddUCpp+tDEKV45JqMhW/PrJasKXTSyp9w=

# API Authentication
API_KEY=ndtxPclnW91si+YmRdiVMC1+rXlGz0wDZg8RVrCgOf4=

# DigitalOcean Spaces (S3-compatible storage) - Optional
# Uncomment and configure when you need file upload functionality
# SPACES_ENDPOINT=https://nyc3.digitaloceanspaces.com
# SPACES_ACCESS_KEY=your-access-key
# SPACES_SECRET_KEY=your-secret-key
# SPACES_BUCKET=your-bucket-name
# SPACES_CDN_BASE_URL=
```

**Save and exit:**
- Press `Ctrl + X`
- Press `Y` to confirm
- Press `Enter` to save

### Set Proper Permissions

```bash
# Protect the .env file
chmod 600 .env

# Verify permissions (should show -rw-------)
ls -la .env
```

---

## Step 4: Update CORS Configuration for Production

Edit the `appsettings.json` file:

```bash
nano StarApi/appsettings.json
```

Find the `AllowedOrigins` section and update it with your actual frontend domain:

```json
"AllowedOrigins": [
  "https://yourdomain.com",
  "https://www.yourdomain.com"
]
```

**Important:** Replace `yourdomain.com` with your actual domain name.

Save and exit (`Ctrl + X`, `Y`, `Enter`).

---

## Step 5: Start the Application with Docker

```bash
# Make sure you're in the StarApi directory
cd /home/your-user/star-api/StarApi

# Start the containers in detached mode
docker-compose up -d

# Wait for containers to start (about 30 seconds)
```

### Verify Containers are Running

```bash
# List running containers
docker ps

# You should see two containers:
# - starapi (your API application)
# - starapi-db (PostgreSQL database)
```

### Check Application Logs

```bash
# View API logs
docker logs starapi

# View database logs
docker logs starapi-db

# Follow logs in real-time (Ctrl+C to exit)
docker logs -f starapi
```

**Successful startup should show:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:8080
info: Microsoft.Hosting.Lifetime[0]
      Application started.
```

### Test Local Connection

```bash
# Test if API responds on localhost
curl http://localhost:5002/api/posts

# Should return: [] (empty array, or your posts if any exist)
```

---

## Step 6: Set Up Nginx Reverse Proxy

### Install Nginx

```bash
sudo apt update
sudo apt install nginx -y

# Check Nginx status
sudo systemctl status nginx
```

### Create Nginx Configuration

```bash
# Create a new site configuration
sudo nano /etc/nginx/sites-available/starapi
```

Paste this configuration:

```nginx
server {
    listen 80;
    server_name api.yourdomain.com;

    # Security headers
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Proxy configuration
    location / {
        proxy_pass http://localhost:5002;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;

        # Timeout settings
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Optional: Disable access to sensitive files
    location ~ /\. {
        deny all;
    }
}
```

**Important:** Replace `api.yourdomain.com` with your actual domain.

### Enable the Site

```bash
# Create symbolic link to enable site
sudo ln -s /etc/nginx/sites-available/starapi /etc/nginx/sites-enabled/

# Test Nginx configuration
sudo nginx -t

# If test passes, restart Nginx
sudo systemctl restart nginx
```

### Test HTTP Connection

```bash
# Test from VPS
curl http://api.yourdomain.com/api/posts

# Or from your browser
# Visit: http://api.yourdomain.com/api/posts
```

---

## Step 7: Set Up SSL/HTTPS with Certbot

### Install Certbot

```bash
# Install Certbot and Nginx plugin
sudo apt install certbot python3-certbot-nginx -y
```

### Obtain SSL Certificate

```bash
# Run Certbot (replace with your domain)
sudo certbot --nginx -d api.yourdomain.com

# Follow the prompts:
# 1. Enter your email address
# 2. Agree to Terms of Service (Y)
# 3. Choose whether to share email (Y/N)
# 4. Choose redirect option: 2 (Redirect HTTP to HTTPS)
```

### Set Up Auto-Renewal

```bash
# Test auto-renewal
sudo certbot renew --dry-run

# If successful, auto-renewal is configured
# Certificates will auto-renew before expiration
```

### Verify HTTPS

```bash
# Test HTTPS connection
curl https://api.yourdomain.com/api/posts

# Or visit in browser:
# https://api.yourdomain.com/api/posts
```

---

## Step 8: Configure Firewall

```bash
# Enable UFW (Uncomplicated Firewall)
sudo ufw allow 22/tcp      # SSH
sudo ufw allow 80/tcp      # HTTP
sudo ufw allow 443/tcp     # HTTPS

# Enable firewall
sudo ufw enable

# Check status
sudo ufw status
```

Expected output:
```
Status: active

To                         Action      From
--                         ------      ----
22/tcp                     ALLOW       Anywhere
80/tcp                     ALLOW       Anywhere
443/tcp                     ALLOW       Anywhere
```

---

## Step 9: Test Your API

### Test GET Request (Public Endpoint)

```bash
curl https://api.yourdomain.com/api/posts
```

Expected: `[]` or list of posts

### Test POST Request (Protected Endpoint)

```bash
curl -X POST https://api.yourdomain.com/api/posts \
  -H "Content-Type: application/json" \
  -H "X-API-Key: ndtxPclnW91si+YmRdiVMC1+rXlGz0wDZg8RVrCgOf4=" \
  -d '{"title":"My First Post","content":"Hello World!","isDraft":false}'
```

Expected: Returns the created post with ID

### Test Authentication Failure

```bash
curl -X POST https://api.yourdomain.com/api/posts \
  -H "Content-Type: application/json" \
  -H "X-API-Key: wrong-key" \
  -d '{"title":"Test","content":"Test"}'
```

Expected: `401 Unauthorized`

---

## Project Structure on VPS

```
/home/your-user/star-api/StarApi/
├── .env                          # ⚠️ Contains secrets (NOT in git)
├── .gitignore                    # ✅ Excludes .env from git
├── compose.yaml                  # Docker Compose configuration
├── StarApi/
│   ├── appsettings.json          # Application settings (CORS, etc.)
│   ├── Dockerfile                # Docker image definition
│   ├── Program.cs                # Application entry point
│   ├── Controllers/              # API endpoints
│   ├── Models/                   # Data models
│   ├── Services/                 # Business logic
│   └── ...
├── SECURITY_AUDIT_REPORT.md      # Security analysis
├── SECURITY_FIXES_APPLIED.md     # Security changes log
└── VPS_DEPLOYMENT_GUIDE.md       # This file
```

---

## Useful Docker Commands

### View Logs

```bash
# View all logs
docker logs starapi

# Follow logs in real-time
docker logs -f starapi

# View last 100 lines
docker logs --tail 100 starapi
```

### Restart Containers

```bash
# Restart specific container
docker restart starapi

# Restart all containers
docker-compose restart

# Stop and start (full restart)
docker-compose down
docker-compose up -d
```

### Update Application

```bash
# Pull latest code (if using Git)
git pull

# Rebuild and restart containers
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### Database Access

```bash
# Connect to PostgreSQL
docker exec -it starapi-db psql -U postgres -d starblog

# Inside PostgreSQL:
\dt                    # List tables
SELECT * FROM "Posts"; # Query posts
\q                     # Quit
```

### Remove Everything (Clean Slate)

```bash
# Stop and remove containers
docker-compose down

# Remove volumes (⚠️ DELETES DATABASE!)
docker-compose down -v

# Remove images
docker rmi starapi
```

---

## Troubleshooting

### Container Won't Start

```bash
# Check logs for errors
docker logs starapi

# Common issues:
# 1. Database not ready - Wait 30 seconds and check again
# 2. Port already in use - Change port in compose.yaml
# 3. Missing .env file - Create it following Step 3
```

### Can't Connect to Database

```bash
# Check database container
docker logs starapi-db

# Verify environment variables
docker exec starapi env | grep POSTGRES

# Test database connection
docker exec starapi-db pg_isready -U postgres
```

### API Returns 502 Bad Gateway

```bash
# Check if API is running
docker ps

# Check Nginx logs
sudo tail -f /var/log/nginx/error.log

# Check API logs
docker logs starapi

# Restart Nginx
sudo systemctl restart nginx
```

### SSL Certificate Issues

```bash
# Check certificate status
sudo certbot certificates

# Renew certificate manually
sudo certbot renew

# Check Nginx configuration
sudo nginx -t
```

### CORS Errors from Frontend

```bash
# Verify AllowedOrigins in appsettings.json
cat StarApi/appsettings.json | grep -A 3 "AllowedOrigins"

# Should match your frontend domain exactly
# Including protocol (http/https) and port if applicable
```

---

## Maintenance

### Update Dependencies

```bash
# Update Docker
sudo apt update
sudo apt upgrade docker-ce docker-ce-cli containerd.io

# Update application packages (inside container)
docker exec starapi dotnet list package --outdated
```

### Backup Database

```bash
# Create backup
docker exec starapi-db pg_dump -U postgres starblog > backup_$(date +%Y%m%d).sql

# Restore backup
docker exec -i starapi-db psql -U postgres starblog < backup_20241024.sql
```

### Monitor Disk Space

```bash
# Check disk usage
df -h

# Check Docker disk usage
docker system df

# Clean up unused Docker resources
docker system prune -a
```

### View System Resources

```bash
# Check container resource usage
docker stats

# Check system resources
htop
```

---

## Security Checklist

- [x] Strong database password (32+ characters)
- [x] API key secured in .env file
- [x] CORS configured with specific origins
- [x] HTTPS enabled with valid SSL certificate
- [x] Database not exposed to public internet
- [x] Firewall configured (SSH, HTTP, HTTPS only)
- [x] .env file has proper permissions (600)
- [x] Auto-renewal configured for SSL certificates
- [ ] Rate limiting (optional, recommended)
- [ ] Log monitoring (optional, recommended)
- [ ] Automated backups (optional, recommended)

---

## Important Security Notes

### Secrets Management

⚠️ **NEVER commit these files to Git:**
- `.env` (already in .gitignore ✅)
- Any files containing passwords, API keys, or credentials

⚠️ **Keep these secrets safe:**
- Database Password: `JTv12ZVMXiddUCpp+tDEKV45JqMhW/PrJasKXTSyp9w=`
- API Key: `ndtxPclnW91si+YmRdiVMC1+rXlGz0wDZg8RVrCgOf4=`

### Update CORS Before Going Live

The current `AllowedOrigins` is set to `http://localhost:1025` for development.

**Before deployment, update to:**
```json
"AllowedOrigins": [
  "https://yourdomain.com",
  "https://www.yourdomain.com"
]
```

---

## Next Steps After Deployment

1. **Test all API endpoints** from your frontend application
2. **Monitor logs** for any errors or issues
3. **Set up automated backups** for your database
4. **Consider adding rate limiting** (see SECURITY_AUDIT_REPORT.md)
5. **Set up monitoring/alerting** (optional: Grafana, Prometheus)
6. **Configure DigitalOcean Spaces** if you need file uploads

---

## Quick Reference

### Your API Endpoints

- **Base URL:** `https://api.yourdomain.com`
- **Get Posts:** `GET /api/posts`
- **Get Post:** `GET /api/posts/{id}`
- **Create Post:** `POST /api/posts` (requires API key)
- **Update Post:** `PUT /api/posts/{id}` (requires API key)
- **Delete Post:** `DELETE /api/posts/{id}` (requires API key)
- **Upload File:** `POST /api/uploads` (requires API key)
- **Delete File:** `DELETE /api/uploads/{key}` (requires API key)

### Authentication Header

```
X-API-Key: ndtxPclnW91si+YmRdiVMC1+rXlGz0wDZg8RVrCgOf4=
```

### Support Files

- `SECURITY_AUDIT_REPORT.md` - Full security analysis
- `SECURITY_FIXES_APPLIED.md` - Summary of security improvements
- `VPS_DEPLOYMENT_GUIDE.md` - This deployment guide

---

## Support

If you encounter issues:
1. Check the Troubleshooting section above
2. Review Docker logs: `docker logs starapi`
3. Review Nginx logs: `sudo tail -f /var/log/nginx/error.log`
4. Verify your .env file contains all required variables

---

**Deployment Guide Version:** 1.0
**Last Updated:** October 24, 2025
**Application:** StarApi Blog Backend API

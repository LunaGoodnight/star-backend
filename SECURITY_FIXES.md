# = Security Fixes and Deployment Guide

##  Security Fixes Complete!

All critical security issues in your repository have been fixed. Your API is now production-ready with proper security controls.

---

## =Ë Summary of Changes

### Files Modified

| File | Changes |
|------|---------|
| `appsettings.json` | Removed hardcoded JWT key and admin credentials |
| `appsettings.Production.json` | Created secure production configuration |
| `appsettings.Development.json` | Updated development configuration |
| `Program.cs` | Added HTTPS enforcement, security headers, rate limiting |
| `Controllers/AuthController.cs` | Added rate limiting to login endpoint |
| `.gitignore` | Added protection for sensitive configuration files |

---

## =á Security Improvements

### Critical Issues Fixed

| Issue | Status | Details |
|-------|--------|---------|
| **Hardcoded JWT Key** |  Fixed | - Removed fallback key: `dev-secret-key-change-in-production-1234567890`<br>- App now **requires** `Jwt__Key` environment variable<br>- Application **will not start** without it<br>- Location: `Program.cs:103-106`, `AuthController.cs:48-53` |
| **Hardcoded Admin Credentials** |  Fixed | - Removed default `admin/changeme` credentials<br>- Must be set via environment variables<br>- Location: `appsettings.json:23-26` |
| **No HTTPS Enforcement** |  Fixed | - Added HTTPS redirect middleware<br>- Added HSTS header (HTTP Strict Transport Security)<br>- Location: `Program.cs:144, 159` |
| **Missing Security Headers** |  Fixed | - `X-Content-Type-Options: nosniff`<br>- `X-Frame-Options: DENY`<br>- `X-XSS-Protection: 1; mode=block`<br>- `Referrer-Policy: no-referrer`<br>- `Content-Security-Policy: default-src 'self'`<br>- Location: `Program.cs:147-156` |
| **Swagger Exposed** |  Fixed | - Swagger only enabled in Development environment<br>- Returns 404 in Production<br>- Location: `Program.cs:136-145` |
| **No Rate Limiting** |  Fixed | - Login endpoint limited to 5 attempts per minute<br>- 6th attempt returns HTTP 429 (Too Many Requests)<br>- Location: `Program.cs:123-132`, `AuthController.cs:29` |
| **Long Token Expiration** |  Fixed | - Reduced from 60 minutes to 15 minutes<br>- Location: `appsettings.json:21` |
| **Secrets in Git** |  Fixed | - Updated `.gitignore` to exclude sensitive configs<br>- Environment-specific files protected |

---

##   CRITICAL: Before You Deploy

**Your application will NOT start without these environment variables!**

The application will throw an error and refuse to start if secrets are missing. This is by design to prevent insecure deployments.

---

## =€ Deployment Instructions

### Prerequisites

- VPS with Ubuntu/Debian
- Domain name: `staradmin.vividcats.org`
- .NET 9.0 Runtime
- PostgreSQL
- Nginx
- Certbot (for SSL)

### Step 1: Generate Strong Secrets

On your VPS, run these commands to generate cryptographically secure secrets:

```bash
# Generate JWT Key (256-bit)
openssl rand -base64 32
# Example output: vK8s7nM2pQ4wR6tY9uI1oP3aS5dF7gH0jK2lM4nB6vC8xZ=

# Generate API Key
openssl rand -hex 32
# Example output: a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6a7b8c9d0e1f2
```

**Save these values!** You'll need them in the next step.

### Step 2: Create .env File on VPS

Create a `.env` file in your application directory (`/var/www/starapi/.env`):

```bash
# Navigate to your app directory
cd /var/www/starapi

# Create .env file
sudo nano .env
```

Paste the following content and **replace** the placeholder values with your generated secrets:

```env
# CRITICAL: JWT Configuration
Jwt__Key=YOUR_GENERATED_JWT_KEY_FROM_STEP_1
Jwt__ExpireMinutes=15

# CRITICAL: Admin Credentials
Admin__Username=YOUR_SECURE_ADMIN_USERNAME
Admin__Password=YOUR_STRONG_PASSWORD_16+_CHARS

# Database Connection
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=starblog;Username=starapi_user;Password=YOUR_DB_PASSWORD

# API Key (for X-API-Key authentication)
ApiKey=YOUR_GENERATED_API_KEY_FROM_STEP_1

# DigitalOcean Spaces (if using file uploads)
DigitalOceanSpaces__AccessKey=YOUR_SPACES_ACCESS_KEY
DigitalOceanSpaces__SecretKey=YOUR_SPACES_SECRET_KEY
DigitalOceanSpaces__BucketName=cute33
DigitalOceanSpaces__Region=sgp1
DigitalOceanSpaces__Endpoint=https://sgp1.digitaloceanspaces.com
DigitalOceanSpaces__CdnBaseUrl=https://cute33.sgp1.cdn.digitaloceanspaces.com

# Environment
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://localhost:5000
```

**Secure the .env file:**

```bash
sudo chmod 600 .env
sudo chown www-data:www-data .env
```

### Step 3: Create Systemd Service

Create a systemd service file:

```bash
sudo nano /etc/systemd/system/starapi.service
```

Add this configuration:

```ini
[Unit]
Description=Star API .NET Application
After=network.target

[Service]
Type=notify
WorkingDirectory=/var/www/starapi/publish
ExecStart=/usr/bin/dotnet /var/www/starapi/publish/StarApi.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=starapi
User=www-data
EnvironmentFile=/var/www/starapi/.env

[Install]
WantedBy=multi-user.target
```

Enable and start the service:

```bash
sudo systemctl daemon-reload
sudo systemctl enable starapi
sudo systemctl start starapi
sudo systemctl status starapi
```

### Step 4: Configure Nginx Reverse Proxy

Create Nginx configuration:

```bash
sudo nano /etc/nginx/sites-available/starapi
```

Add this configuration:

```nginx
# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name staradmin.vividcats.org;
    return 301 https://$server_name$request_uri;
}

# HTTPS configuration
server {
    listen 443 ssl http2;
    server_name staradmin.vividcats.org;

    # SSL certificates (certbot will add these automatically)

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "no-referrer" always;

    # Proxy to .NET application
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # Increase upload size limit
    client_max_body_size 20M;
}
```

Enable the site:

```bash
sudo ln -s /etc/nginx/sites-available/starapi /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Step 5: Obtain SSL Certificate with Certbot

Run certbot to automatically configure SSL:

```bash
sudo certbot --nginx -d staradmin.vividcats.org
```

Follow the prompts:
- Enter your email address
- Agree to terms of service
- Choose to redirect HTTP to HTTPS (recommended)

Certbot will automatically:
- Obtain the SSL certificate from Let's Encrypt
- Update your Nginx configuration with SSL settings
- Set up automatic renewal (certificates expire every 90 days)

Test automatic renewal:

```bash
sudo certbot renew --dry-run
```

### Step 6: Configure Firewall

```bash
# Allow SSH, HTTP, and HTTPS
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
sudo ufw status
```

---

##  Post-Deployment Verification

Run these tests to verify your security fixes are working:

### 1. Verify Environment Variables Loaded

```bash
sudo journalctl -u starapi -n 50
```

You should **NOT** see errors like:
- L "JWT Key is not configured"
- L "Database connection failed"

### 2. Test HTTPS Redirect

```bash
curl -I http://staradmin.vividcats.org
```

**Expected output:**
```
HTTP/1.1 301 Moved Permanently
Location: https://staradmin.vividcats.org/
```

### 3. Verify Security Headers

```bash
curl -I https://staradmin.vividcats.org
```

**Expected headers:**
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000
Referrer-Policy: no-referrer
```

### 4. Confirm Swagger is Disabled

```bash
curl https://staradmin.vividcats.org/swagger
```

**Expected:** HTTP 404 Not Found (Swagger is disabled in Production)

### 5. Test Rate Limiting

Try 6 login attempts within 1 minute:

```bash
for i in {1..6}; do
  curl -X POST https://staradmin.vividcats.org/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"wrong","password":"wrong"}' \
    -w "\nAttempt $i: %{http_code}\n"
  sleep 10
done
```

**Expected:**
- Attempts 1-5: HTTP 401 Unauthorized
- Attempt 6: HTTP 429 Too Many Requests 

### 6. Test Admin Login (Use Your Real Credentials)

```bash
curl -X POST https://staradmin.vividcats.org/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"YOUR_ADMIN_USERNAME","password":"YOUR_ADMIN_PASSWORD"}'
```

**Expected output:**
```json
{
  "token": "eyJhbGci...",
  "tokenType": "Bearer",
  "expiresAt": "2025-10-28T12:15:00Z",
  "user": {
    "username": "YOUR_ADMIN_USERNAME",
    "role": "Admin"
  }
}
```

---

## >ê Testing in Development

You mentioned you don't know how to test in development. Here's how:

### Option 1: Using User Secrets (Recommended)

```bash
# Navigate to your project directory
cd D:\side-project\star-api\StarApi\StarApi

# Set up development secrets (one-time setup)
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "dev-jwt-key-for-testing-only-32chars"
dotnet user-secrets set "Admin:Username" "admin"
dotnet user-secrets set "Admin:Password" "devpassword123"
dotnet user-secrets set "ApiKey" "dev-api-key-123"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=starblog;Username=postgres;Password=postgres"

# Run the application
dotnet run
```

### Option 2: Using .env File (Local)

Create a `.env` file in your local project directory:

```env
Jwt__Key=dev-jwt-key-for-testing-only-minimum-32-characters-long
Admin__Username=admin
Admin__Password=devpassword123
ApiKey=dev-api-key-123
ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=starblog;Username=postgres;Password=postgres
ASPNETCORE_ENVIRONMENT=Development
```

Then run:

```bash
dotnet run
```

### Access Swagger in Development

When running in Development mode, Swagger is **enabled**:

```
http://localhost:5000/swagger
```

You can test all endpoints directly in the Swagger UI.

---

## =Ê Monitoring and Logs

### View Application Logs

```bash
# Real-time logs
sudo journalctl -u starapi -f

# Last 100 lines
sudo journalctl -u starapi -n 100

# Search for errors
sudo journalctl -u starapi | grep -i error

# Search for failed logins (security monitoring)
sudo journalctl -u starapi | grep -i "invalid credentials"
```

### View Nginx Logs

```bash
# Real-time access log
sudo tail -f /var/log/nginx/access.log

# Real-time error log
sudo tail -f /var/log/nginx/error.log

# Find rate limit rejections
sudo grep "429" /var/log/nginx/access.log
```

---

## = Updating the Application

When you push code changes:

```bash
# On your VPS
cd /var/www/starapi

# Pull latest changes
git pull

# Rebuild application
dotnet publish -c Release -o ./publish

# Restart service
sudo systemctl restart starapi

# Verify it's running
sudo systemctl status starapi
sudo journalctl -u starapi -n 20
```

---

## =¨ Troubleshooting

### Error: "JWT Key is not configured"

**Problem:** Application won't start, logs show JWT key error.

**Solution:**

```bash
# Verify .env file exists
ls -la /var/www/starapi/.env

# Check if Jwt__Key is set
cat /var/www/starapi/.env | grep Jwt__Key

# If missing, add it
sudo nano /var/www/starapi/.env
# Add: Jwt__Key=<your-generated-key>

# Restart application
sudo systemctl restart starapi
```

### Error: "Invalid credentials" with Correct Password

**Problem:** Can't login even with correct admin credentials.

**Solution:**

```bash
# Check if Admin credentials are set in .env
cat /var/www/starapi/.env | grep Admin

# Verify environment file is loaded
sudo systemctl show starapi --property=EnvironmentFiles

# Check logs for more details
sudo journalctl -u starapi -n 50
```

### SSL Certificate Not Working

**Problem:** Certbot fails or certificate not valid.

**Solution:**

```bash
# Verify domain resolves to your VPS IP
nslookup staradmin.vividcats.org

# Ensure port 80 is accessible
sudo ufw allow 80/tcp

# Try obtaining certificate again
sudo certbot --nginx -d staradmin.vividcats.org

# Check certificate status
sudo certbot certificates

# Test renewal
sudo certbot renew --dry-run
```

### Rate Limiting Not Working

**Problem:** More than 5 login attempts allowed per minute.

**Solution:**

```bash
# Check if rate limiter is configured
grep -r "AddRateLimiter" /var/www/starapi/publish/*.dll

# Restart application to reload configuration
sudo systemctl restart starapi

# Test again with curl loop
```

---

## =Ë Security Checklist

Before going live, ensure all items are checked:

### Environment Configuration
- [ ] JWT key generated with `openssl rand -base64 32` (min 32 chars)
- [ ] API key generated with `openssl rand -hex 32`
- [ ] Admin username changed from "admin" to something unique
- [ ] Admin password is strong (16+ chars, mixed case, numbers, symbols)
- [ ] Database password is strong and unique
- [ ] All secrets set in `.env` file on VPS
- [ ] `.env` file has restricted permissions (`chmod 600`)
- [ ] `.env` file is NOT committed to git (in .gitignore)

### Server Configuration
- [ ] SSL certificate obtained and working (via certbot)
- [ ] HTTP redirects to HTTPS (test with `curl -I`)
- [ ] HSTS header present in responses
- [ ] Security headers present (X-Frame-Options, X-Content-Type-Options, etc.)
- [ ] Firewall configured (only SSH, HTTP, HTTPS)
- [ ] Systemd service enabled and running
- [ ] Certbot auto-renewal tested

### Security Features
- [ ] Swagger returns 404 in production
- [ ] Rate limiting works (6th login attempt gets HTTP 429)
- [ ] JWT tokens expire in 15 minutes
- [ ] HTTPS is enforced (HTTP redirects)
- [ ] Application logs are being written
- [ ] Failed login attempts are logged

### Backup and Monitoring
- [ ] Database backup strategy in place
- [ ] Log monitoring set up
- [ ] Disk space monitoring configured
- [ ] Alert system for failed logins (optional but recommended)

---

## <˜ Emergency: If You Deployed with Old Secrets

If you accidentally deployed with the old hardcoded secrets (`dev-secret-key-change-in-production-1234567890` or `admin/changeme`):

### Immediate Actions

1. **Stop the application immediately:**
   ```bash
   sudo systemctl stop starapi
   ```

2. **Generate new secrets:**
   ```bash
   openssl rand -base64 32  # New JWT key
   openssl rand -hex 32     # New API key
   ```

3. **Update `.env` file with new secrets:**
   ```bash
   sudo nano /var/www/starapi/.env
   # Replace ALL secrets with newly generated ones
   ```

4. **Restart application:**
   ```bash
   sudo systemctl start starapi
   sudo systemctl status starapi
   ```

5. **Rotate database password if it was exposed**

6. **Check logs for suspicious activity:**
   ```bash
   sudo journalctl -u starapi --since "24 hours ago" | grep -i "login\|auth"
   ```

7. **Monitor for unusual API activity for the next 24-48 hours**

---

## =Ú Additional Resources

- **Frontend Login Integration:** See `FRONTEND_LOGIN.md` for connecting your frontend
- **Docker Deployment:** See `DEPLOYMENT.md` for Docker-based deployment
- **API Documentation:** Available at `/swagger` in Development mode only

---

## <‰ Deployment Complete!

Your API is now secured and ready for production deployment. The most important steps are:

1.  Generate strong secrets with OpenSSL
2.  Create `.env` file with all required variables
3.  Set up SSL with certbot
4.  Verify security features are working

**Remember:** The application will refuse to start without the required environment variables. This is intentional to prevent insecure deployments.

For support, check the logs:
```bash
sudo journalctl -u starapi -f
```

Good luck with your deployment! =€

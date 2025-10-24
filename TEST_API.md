# API Testing Guide

Use these commands to test your StarApi locally before deploying.

---

## Prerequisites

Make sure Docker is running:
```bash
docker-compose up -d
```

---

## Test 1: Public Endpoint (No Auth Required)

### Get All Posts
```bash
curl http://localhost:5002/api/posts
```

**Expected Response:**
```json
[]
```
Or a list of posts if you have any.

**Status Code:** `200 OK`

---

## Test 2: Protected Endpoint WITHOUT API Key (Should Fail)

### Try to Create Post Without Auth
```bash
curl -X POST http://localhost:5002/api/posts \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Test Post\",\"content\":\"This should fail\",\"isDraft\":false}"
```

**Expected Response:**
```
Unauthorized
```

**Status Code:** `401 Unauthorized` ✅ This is correct!

---

## Test 3: Protected Endpoint WITH API Key (Should Succeed)

### Create a Post With Auth
```bash
curl -X POST http://localhost:5002/api/posts \
  -H "Content-Type: application/json" \
  -H "X-API-Key: EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI=" \
  -d "{\"title\":\"My First Post\",\"content\":\"Hello World!\",\"isDraft\":false}"
```

**Expected Response:**
```json
{
  "id": 1,
  "title": "My First Post",
  "content": "Hello World!",
  "isDraft": false,
  "createdAt": "2025-10-24T...",
  "updatedAt": "2025-10-24T...",
  "publishedAt": "2025-10-24T..."
}
```

**Status Code:** `201 Created` ✅

---

## Test 4: Get Specific Post

```bash
curl http://localhost:5002/api/posts/1
```

**Expected Response:**
```json
{
  "id": 1,
  "title": "My First Post",
  "content": "Hello World!",
  ...
}
```

---

## Test 5: Update Post (With Auth)

```bash
curl -X PUT http://localhost:5002/api/posts/1 \
  -H "Content-Type: application/json" \
  -H "X-API-Key: EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI=" \
  -d "{\"id\":1,\"title\":\"Updated Title\",\"content\":\"Updated content\",\"isDraft\":false}"
```

**Expected Response:**
- No content returned

**Status Code:** `204 No Content` ✅

---

## Test 6: Delete Post (With Auth)

```bash
curl -X DELETE http://localhost:5002/api/posts/1 \
  -H "X-API-Key: EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI="
```

**Expected Response:**
- No content returned

**Status Code:** `204 No Content` ✅

---

## Test 7: CORS Test (From Browser)

Open browser console (F12) and run:

```javascript
// This should work (localhost:1025 is allowed)
fetch('http://localhost:5002/api/posts')
  .then(r => r.json())
  .then(data => console.log(data));
```

**Note:** You need to test this from a page running on `http://localhost:1025` since that's your allowed CORS origin.

---

## Test 8: File Upload (With Auth)

### Prepare a test image
Save any image as `test.jpg`

```bash
curl -X POST http://localhost:5002/api/uploads \
  -H "X-API-Key: EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI=" \
  -F "file=@test.jpg"
```

**Expected Response:**
```json
{
  "key": "blog/test-abc123.jpg",
  "url": "https://...",
  "contentType": "image/jpeg",
  "size": 12345
}
```

**Note:** This requires DigitalOcean Spaces to be configured in `.env`

---

## Test 9: Wrong API Key (Should Fail)

```bash
curl -X POST http://localhost:5002/api/posts \
  -H "Content-Type: application/json" \
  -H "X-API-Key: wrong-api-key-12345" \
  -d "{\"title\":\"Test\",\"content\":\"Test\"}"
```

**Expected Response:**
```
Unauthorized
```

**Status Code:** `401 Unauthorized` ✅

---

## Test 10: Database Connection

### Check if database is accessible
```bash
docker exec -it starapi-db psql -U postgres -d starblog -c "SELECT * FROM \"Posts\";"
```

**Expected Response:**
- List of posts in table format
- Or empty table if no posts created yet

---

## Test 11: View Logs

### API Logs
```bash
docker logs starapi
```

Look for:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:8080
```

### Database Logs
```bash
docker logs starapi-db
```

Look for:
```
database system is ready to accept connections
```

---

## Test 12: Health Check

### Check if both containers are healthy
```bash
docker ps
```

**Expected Output:**
```
CONTAINER ID   IMAGE        STATUS
abc123...      starapi      Up 2 minutes
def456...      postgres     Up 2 minutes (healthy)
```

---

## Using PowerShell (Windows Alternative)

If `curl` doesn't work, use PowerShell:

```powershell
# Get posts
Invoke-RestMethod -Uri http://localhost:5002/api/posts -Method Get

# Create post (with API key)
$headers = @{
    "Content-Type" = "application/json"
    "X-API-Key" = "EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI="
}
$body = @{
    title = "My First Post"
    content = "Hello World!"
    isDraft = $false
} | ConvertTo-Json

Invoke-RestMethod -Uri http://localhost:5002/api/posts -Method Post -Headers $headers -Body $body
```

---

## Using Postman or Insomnia (GUI Alternative)

### Setup:
1. Base URL: `http://localhost:5002`
2. Add header: `X-API-Key: EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI=`

### Test Endpoints:
- GET `/api/posts` - No auth needed
- POST `/api/posts` - Requires API key
- GET `/api/posts/{id}` - No auth needed
- PUT `/api/posts/{id}` - Requires API key
- DELETE `/api/posts/{id}` - Requires API key

---

## Troubleshooting

### Container won't start
```bash
docker logs starapi
docker logs starapi-db
```

### Can't connect to database
```bash
# Check if database is ready
docker exec starapi-db pg_isready -U postgres

# Check environment variables
docker exec starapi env | grep POSTGRES
```

### API returns 500 error
```bash
# Check API logs
docker logs starapi --tail 50

# Restart containers
docker-compose restart
```

### CORS error from frontend
- Make sure your frontend is running on `http://localhost:1025`
- Or update `AllowedOrigins` in `StarApi/appsettings.json`

---

## Quick Test Script

Save this as `test.sh` (Git Bash/WSL) or `test.ps1` (PowerShell):

```bash
#!/bin/bash
echo "Testing StarApi..."

echo "\n1. Testing public endpoint..."
curl -s http://localhost:5002/api/posts

echo "\n\n2. Testing auth failure..."
curl -s -w "\nStatus: %{http_code}\n" -X POST http://localhost:5002/api/posts \
  -H "Content-Type: application/json" \
  -d '{"title":"Test","content":"Test"}'

echo "\n\n3. Testing with auth..."
curl -s -X POST http://localhost:5002/api/posts \
  -H "Content-Type: application/json" \
  -H "X-API-Key: EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI=" \
  -d '{"title":"Test Post","content":"Hello World!","isDraft":false}'

echo "\n\nAll tests completed!"
```

---

## Success Checklist

- [ ] Docker containers are running
- [ ] Public endpoint returns 200
- [ ] Unauthorized request returns 401
- [ ] Authorized request creates post successfully
- [ ] Can retrieve created post
- [ ] Can update post with auth
- [ ] Can delete post with auth
- [ ] Database is accessible
- [ ] No errors in logs

---

**Your API Key for Testing:**
```
EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI=
```

Keep this safe! This is your new, secure API key.

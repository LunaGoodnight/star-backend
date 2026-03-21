#!/usr/bin/env python3
"""
Fix images with Chinese characters in URLs that failed during migration.
Re-downloads them with proper URL encoding and re-uploads.
Then patches the affected posts in star-backend.
"""

import json
import os
import re
import sys
import time
import hashlib
import urllib.request
import urllib.parse
import urllib.error
import html

STAR_API = "http://localhost:5002"
API_KEY = "EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI="
PROGRESS_FILE = "/srv/projects/migrate-blogs-progress.json"
IMAGE_CACHE_FILE = "/srv/projects/migrate-blogs-imagecache.json"


def api_request(method, path, data=None, is_upload=False, file_path=None, content_type=None):
    url = f"{STAR_API}{path}"
    headers = {"X-API-Key": API_KEY}

    if is_upload and file_path:
        boundary = "----MigrationBoundary" + hashlib.md5(str(time.time()).encode()).hexdigest()[:16]
        headers["Content-Type"] = f"multipart/form-data; boundary={boundary}"
        with open(file_path, "rb") as f:
            file_data = f.read()
        filename = os.path.basename(file_path)
        body = (
            f"--{boundary}\r\n"
            f'Content-Disposition: form-data; name="file"; filename="{filename}"\r\n'
            f"Content-Type: {content_type or 'image/jpeg'}\r\n"
            f"\r\n"
        ).encode() + file_data + f"\r\n--{boundary}--\r\n".encode()
        req = urllib.request.Request(url, data=body, headers=headers, method=method)
    elif data is not None:
        headers["Content-Type"] = "application/json"
        body = json.dumps(data).encode("utf-8")
        req = urllib.request.Request(url, data=body, headers=headers, method=method)
    else:
        req = urllib.request.Request(url, headers=headers, method=method)

    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            if resp.status in (200, 201):
                return json.loads(resp.read().decode("utf-8"))
            return {"status": resp.status}
    except urllib.error.HTTPError as e:
        error_body = e.read().decode("utf-8", errors="replace") if e.fp else ""
        print(f"  API Error {e.code}: {error_body[:200]}")
        return None
    except Exception as e:
        print(f"  API Exception: {e}")
        return None


def encode_url(url):
    """Properly encode a URL with non-ASCII characters."""
    parsed = urllib.parse.urlparse(url)
    # Encode the path component
    encoded_path = urllib.parse.quote(parsed.path, safe='/:@!$&\'()*+,;=')
    # Reconstruct
    return urllib.parse.urlunparse((
        parsed.scheme, parsed.netloc, encoded_path,
        parsed.params, parsed.query, parsed.fragment
    ))


def download_image(url, dest_path):
    encoded = encode_url(url)
    req = urllib.request.Request(encoded)
    req.add_header("User-Agent", "Mozilla/5.0 MigrationBot")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            with open(dest_path, "wb") as f:
                f.write(resp.read())
        return True
    except Exception as e:
        print(f"    Still failed: {url[:80]}: {e}")
        return False


def get_content_type(url):
    url_lower = url.lower().split("?")[0]
    if url_lower.endswith(".png"):
        return "image/png"
    elif url_lower.endswith(".gif"):
        return "image/gif"
    elif url_lower.endswith(".webp"):
        return "image/webp"
    return "image/jpeg"


def main():
    print("Fixing Chinese-character image URLs...")

    image_cache = {}
    if os.path.exists(IMAGE_CACHE_FILE):
        with open(IMAGE_CACHE_FILE, "r") as f:
            image_cache = json.load(f)

    # Get all posts from star-backend
    print("Fetching all posts...")
    posts = api_request("GET", "/api/posts")
    if not posts:
        print("ERROR: Cannot get posts")
        sys.exit(1)
    print(f"  Found {len(posts)} posts")

    fixed_count = 0
    failed_count = 0

    for i, post in enumerate(posts):
        content = post.get("content", "")
        # Find image URLs with non-ASCII characters
        img_pattern = r'<img[^>]+src=["\']([^"\']+)["\']'
        imgs = re.findall(img_pattern, content, re.I)

        needs_fix = False
        for img_url in imgs:
            # Skip already-migrated images
            if "cute33.sgp1.cdn.digitaloceanspaces.com" in img_url:
                continue
            if img_url.startswith("data:"):
                continue

            # Check if URL has non-ASCII chars (Chinese)
            try:
                img_url.encode('ascii')
                continue  # Pure ASCII, skip
            except UnicodeEncodeError:
                pass  # Has non-ASCII, needs fixing

            cache_key = img_url.split("?")[0]
            if cache_key in image_cache:
                new_url = image_cache[cache_key]
                content = content.replace(img_url, new_url)
                needs_fix = True
                continue

            # Download with proper encoding
            ext = "jpg"
            for e in ["png", "gif", "webp", "jpeg", "jpg"]:
                if img_url.lower().split("?")[0].endswith(f".{e}"):
                    ext = e if e != "jpeg" else "jpg"
                    break

            tmp_path = f"/tmp/fix_img_{hashlib.md5(img_url.encode()).hexdigest()[:12]}.{ext}"
            ct = get_content_type(img_url)

            if download_image(img_url, tmp_path):
                if os.path.getsize(tmp_path) < 100:
                    try: os.remove(tmp_path)
                    except: pass
                    continue

                result = api_request("POST", "/api/uploads?prefix=blog", is_upload=True,
                                     file_path=tmp_path, content_type=ct)
                if result and "url" in result:
                    new_url = result["url"]
                    image_cache[cache_key] = new_url
                    content = content.replace(img_url, new_url)
                    needs_fix = True
                    print(f"  Fixed: {img_url[:60]}... -> {new_url[-40:]}")
                else:
                    failed_count += 1

                try: os.remove(tmp_path)
                except: pass
                time.sleep(0.2)
            else:
                failed_count += 1

        if needs_fix:
            # Update the post
            update_data = {
                "id": post["id"],
                "title": post["title"],
                "content": content,
                "isDraft": post.get("isDraft", False),
                "categoryId": post.get("categoryId"),
                "publishedAt": post.get("publishedAt"),
                "thumbnail": post.get("thumbnail"),
            }
            result = api_request("PUT", f"/api/posts/{post['id']}", update_data)
            if result is not None:
                fixed_count += 1
                print(f"  Updated post {post['id']}: {post['title'][:40]}")
            else:
                print(f"  Failed to update post {post['id']}")

        if (i + 1) % 50 == 0:
            print(f"  Processed {i+1}/{len(posts)} posts...")
            with open(IMAGE_CACHE_FILE, "w") as f:
                json.dump(image_cache, f, ensure_ascii=False, indent=2)

    # Save cache
    with open(IMAGE_CACHE_FILE, "w") as f:
        json.dump(image_cache, f, ensure_ascii=False, indent=2)

    print(f"\nDone! Fixed {fixed_count} posts, {failed_count} images still failed.")


if __name__ == "__main__":
    main()

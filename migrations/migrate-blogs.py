#!/usr/bin/env python3
"""
Blog Migration Script: WordPress sites -> star-backend
Sources: tingmeow.pinkjelly.org, pinkjelly.org
Target: star-backend API at localhost:5002

Usage:
  python3 /srv/projects/migrate-blogs.py

Resume (skips already-migrated posts based on progress file):
  python3 /srv/projects/migrate-blogs.py
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
from datetime import datetime

# ─── Configuration ───────────────────────────────────────────────────────────

STAR_API = "http://localhost:5002"
API_KEY = "EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI="
PROGRESS_FILE = "/srv/projects/migrate-blogs-progress.json"
IMAGE_CACHE_FILE = "/srv/projects/migrate-blogs-imagecache.json"

SOURCES = [
    {
        "name": "tingmeow",
        "api_base": "https://tingmeow.pinkjelly.org/wp-json/wp/v2",
        "use_rest_route": False,
    },
    {
        "name": "pinkjelly",
        "api_base": "https://pinkjelly.org",
        "use_rest_route": True,
    },
]

# Category mapping: (source_name, wp_category_id) -> star_backend_category_id
# star-backend categories:
# 1:ACG, 2:日記, 3:以月光為契, 4:小說心得, 5:生活, 6:科技, 7:旅遊,
# 8:自然科學, 9:地理, 10:數學, 11:詩, 12:小說, 13:笑話, 14:文化,
# 15:星辰藥師, 16:萬界藥神, 17:灰鴉學院, 18:AI
CATEGORY_MAP = {
    # tingmeow (frontend dev blog)
    ("tingmeow", 1): 6,    # Uncategorized -> 科技
    ("tingmeow", 2): 2,    # 日記 -> 日記
    ("tingmeow", 3): 6,    # laravel -> 科技
    ("tingmeow", 5): 6,    # css -> 科技
    ("tingmeow", 6): 6,    # compass -> 科技
    ("tingmeow", 8): 5,    # 食記 -> 生活
    ("tingmeow", 9): 1,    # 遊戲 -> ACG
    ("tingmeow", 10): 6,   # jQuery -> 科技
    ("tingmeow", 11): 6,   # 前端开发 -> 科技
    ("tingmeow", 16): 6,   # react -> 科技
    ("tingmeow", 35): 6,   # 全端開發 -> 科技
    # pinkjelly (personal blog)
    ("pinkjelly", 1): 5,    # Uncategorized -> 生活
    ("pinkjelly", 4): 5,    # 運動 -> 生活
    ("pinkjelly", 5): 5,    # 正妹 -> 生活
    ("pinkjelly", 6): 5,    # 音樂 -> 生活
    ("pinkjelly", 7): 4,    # 小說閱讀心得 -> 小說心得
    ("pinkjelly", 8): 1,    # 追劇 -> ACG
    ("pinkjelly", 10): 12,  # 人妻嘉玲的一生 -> 小說
    ("pinkjelly", 11): 2,   # 日記 -> 日記
    ("pinkjelly", 12): 12,  # 寶石魔法 -> 小說
    ("pinkjelly", 13): 12,  # 魔王的妻子哪有這麼好當 -> 小說
    ("pinkjelly", 14): 5,   # 理財 -> 生活
    ("pinkjelly", 15): 12,  # 在尋找妹妹的旅途中... -> 小說
    ("pinkjelly", 16): 4,   # 讀書心得 -> 小說心得
}

# ─── Helper Functions ────────────────────────────────────────────────────────

def load_progress():
    if os.path.exists(PROGRESS_FILE):
        with open(PROGRESS_FILE, "r") as f:
            return json.load(f)
    return {"migrated": {}, "failed": {}, "skipped_duplicates": []}


def save_progress(progress):
    with open(PROGRESS_FILE, "w") as f:
        json.dump(progress, f, ensure_ascii=False, indent=2)


def load_image_cache():
    if os.path.exists(IMAGE_CACHE_FILE):
        with open(IMAGE_CACHE_FILE, "r") as f:
            return json.load(f)
    return {}


def save_image_cache(cache):
    with open(IMAGE_CACHE_FILE, "w") as f:
        json.dump(cache, f, ensure_ascii=False, indent=2)


def api_request(method, path, data=None, is_upload=False, file_path=None, content_type=None):
    """Make a request to the star-backend API."""
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


def wp_fetch_posts(source, page=1, per_page=100):
    """Fetch posts from WordPress REST API."""
    if source["use_rest_route"]:
        url = f'{source["api_base"]}/index.php?rest_route=/wp/v2/posts&per_page={per_page}&page={page}&_embed=1'
    else:
        url = f'{source["api_base"]}/posts?per_page={per_page}&page={page}&_embed=1'

    req = urllib.request.Request(url)
    req.add_header("User-Agent", "Mozilla/5.0 MigrationBot")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        if e.code == 400:  # No more pages
            return []
        raise


def wp_fetch_all_posts(source):
    """Fetch all posts from a WordPress site."""
    all_posts = []
    page = 1
    while True:
        print(f"  Fetching page {page}...")
        posts = wp_fetch_posts(source, page=page)
        if not posts:
            break
        all_posts.extend(posts)
        page += 1
        time.sleep(0.3)
    return all_posts


def download_image(url, dest_path):
    """Download an image from a URL."""
    # Clean up WP proxy URLs - get original
    clean_url = url.split("?")[0] if "i0.wp.com" not in url else url

    req = urllib.request.Request(clean_url)
    req.add_header("User-Agent", "Mozilla/5.0 MigrationBot")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            with open(dest_path, "wb") as f:
                f.write(resp.read())
        return True
    except Exception as e:
        print(f"    Failed to download {clean_url[:80]}: {e}")
        return False


def get_content_type(url):
    """Guess content type from URL."""
    url_lower = url.lower().split("?")[0]
    if url_lower.endswith(".png"):
        return "image/png"
    elif url_lower.endswith(".gif"):
        return "image/gif"
    elif url_lower.endswith(".webp"):
        return "image/webp"
    elif url_lower.endswith(".avif"):
        return "image/avif"
    return "image/jpeg"


def upload_image(local_path, content_type="image/jpeg"):
    """Upload an image to star-backend and return the CDN URL."""
    result = api_request("POST", "/api/uploads?prefix=blog", is_upload=True,
                         file_path=local_path, content_type=content_type)
    if result and "url" in result:
        return result["url"]
    return None


def extract_image_urls(html_content):
    """Extract all image URLs from HTML content."""
    # Match src="..." in img tags
    pattern = r'<img[^>]+src=["\']([^"\']+)["\']'
    urls = re.findall(pattern, html_content, re.I)
    return urls


def migrate_images_in_content(content, source_name, image_cache):
    """Replace all external image URLs in content with re-uploaded ones."""
    img_urls = extract_image_urls(content)
    if not img_urls:
        return content, image_cache

    for original_url in img_urls:
        # Skip if already a star-backend URL
        if "cute33.sgp1.cdn.digitaloceanspaces.com" in original_url:
            continue
        # Skip data URIs
        if original_url.startswith("data:"):
            continue
        # Skip very small placeholder images
        if "1x1" in original_url or "pixel" in original_url:
            continue

        # Check cache
        cache_key = original_url.split("?")[0]  # Strip query params for cache key
        if cache_key in image_cache:
            new_url = image_cache[cache_key]
            content = content.replace(original_url, new_url)
            continue

        # Download
        ext = "jpg"
        url_path = original_url.split("?")[0].lower()
        for e in ["png", "gif", "webp", "avif", "jpeg", "jpg"]:
            if url_path.endswith(f".{e}"):
                ext = e
                break
        if ext == "jpeg":
            ext = "jpg"

        tmp_path = f"/tmp/migrate_img_{hashlib.md5(original_url.encode()).hexdigest()[:12]}.{ext}"
        ct = get_content_type(original_url)

        if download_image(original_url, tmp_path):
            # Check file size - skip if too small (likely broken)
            if os.path.getsize(tmp_path) < 100:
                print(f"    Skipping tiny image ({os.path.getsize(tmp_path)} bytes): {original_url[:60]}")
                try:
                    os.remove(tmp_path)
                except:
                    pass
                continue

            new_url = upload_image(tmp_path, ct)
            if new_url:
                image_cache[cache_key] = new_url
                content = content.replace(original_url, new_url)
                print(f"    Uploaded image: {original_url[:60]}... -> {new_url[-40:]}")
            else:
                print(f"    Failed to upload: {original_url[:60]}")

            try:
                os.remove(tmp_path)
            except:
                pass
        time.sleep(0.2)  # Rate limit

    return content, image_cache


def clean_html_content(content):
    """Clean up WordPress-specific HTML."""
    # Remove WordPress-specific classes and data attributes that aren't needed
    # but keep the HTML structure intact
    # Remove srcset (we only need the main src)
    content = re.sub(r'\s*srcset="[^"]*"', '', content)
    content = re.sub(r"\s*srcset='[^']*'", '', content)
    # Remove data-recalc-dims
    content = re.sub(r'\s*data-recalc-dims="[^"]*"', '', content)
    # Remove sizes attribute
    content = re.sub(r'\s*sizes="[^"]*"', '', content)
    # Clean up empty paragraphs
    content = re.sub(r'<p>\s*</p>', '', content)
    # Remove wp-image-XXX classes but keep other classes
    content = re.sub(r'wp-image-\d+', '', content)
    return content.strip()


def decode_html_entities(text):
    """Decode HTML entities in title."""
    return html.unescape(text)


def normalize_title(title):
    """Normalize title for comparison."""
    t = decode_html_entities(title).strip().lower()
    t = re.sub(r'\s+', ' ', t)
    return t


# ─── Main Migration ──────────────────────────────────────────────────────────

def main():
    print("=" * 60)
    print("Blog Migration: WordPress -> star-backend")
    print("=" * 60)

    # Load progress and image cache
    progress = load_progress()
    image_cache = load_image_cache()

    # Fetch existing posts from star-backend for duplicate detection
    print("\nFetching existing posts from star-backend...")
    existing = api_request("GET", "/api/posts")
    if existing is None:
        print("ERROR: Cannot connect to star-backend API. Is it running?")
        sys.exit(1)

    existing_titles = set()
    for p in existing:
        existing_titles.add(normalize_title(p["title"]))
    print(f"  Found {len(existing)} existing posts")

    # Also add previously migrated titles to existing set
    for key, info in progress["migrated"].items():
        existing_titles.add(normalize_title(info.get("title", "")))

    total_migrated = 0
    total_skipped = 0
    total_failed = 0

    for source in SOURCES:
        print(f"\n{'=' * 60}")
        print(f"Processing: {source['name']}")
        print(f"{'=' * 60}")

        # Fetch all posts
        posts = wp_fetch_all_posts(source)
        print(f"  Total posts found: {len(posts)}")

        for i, post in enumerate(posts):
            wp_id = post["id"]
            progress_key = f"{source['name']}:{wp_id}"
            title = decode_html_entities(post["title"]["rendered"]).strip()
            norm_title = normalize_title(title)

            # Skip if already migrated
            if progress_key in progress["migrated"]:
                continue

            # Skip empty titles
            if not title:
                title = f"Untitled ({source['name']} #{wp_id})"

            # Skip if duplicate exists
            if norm_title in existing_titles:
                progress["skipped_duplicates"].append(progress_key)
                total_skipped += 1
                if i % 50 == 0:
                    print(f"  [{i+1}/{len(posts)}] Skipping duplicate: {title[:40]}")
                continue

            print(f"  [{i+1}/{len(posts)}] Migrating: {title[:50]}")

            # Get content
            content = post["content"]["rendered"]
            if not content or not content.strip():
                print(f"    Empty content, skipping")
                progress["skipped_duplicates"].append(progress_key)
                total_skipped += 1
                continue

            # Clean HTML
            content = clean_html_content(content)

            # Migrate images
            content, image_cache = migrate_images_in_content(content, source["name"], image_cache)

            # Map category
            wp_cats = post.get("categories", [])
            wp_cat_id = wp_cats[0] if wp_cats else 1
            star_cat_id = CATEGORY_MAP.get((source["name"], wp_cat_id), 5)  # Default: 生活

            # Parse date
            post_date = post["date"]  # e.g., "2025-10-02T09:00:04"
            published_at = post_date + "Z" if "Z" not in post_date else post_date

            # Create post
            post_data = {
                "title": title,
                "content": content,
                "isDraft": False,
                "categoryId": star_cat_id,
                "publishedAt": published_at,
                "thumbnail": None,
            }

            # Try to get thumbnail from featured media or first image
            first_img = extract_image_urls(content)
            if first_img:
                # Use the first image (already migrated) as thumbnail
                for img_url in first_img:
                    if "cute33.sgp1.cdn.digitaloceanspaces.com" in img_url:
                        post_data["thumbnail"] = img_url
                        break

            result = api_request("POST", "/api/posts", post_data)
            if result and "id" in result:
                progress["migrated"][progress_key] = {
                    "title": title,
                    "star_id": result["id"],
                    "wp_id": wp_id,
                }
                existing_titles.add(norm_title)
                total_migrated += 1
            else:
                progress["failed"][progress_key] = {
                    "title": title,
                    "error": "API returned no ID",
                }
                total_failed += 1

            # Save progress every 10 posts
            if (total_migrated + total_failed) % 10 == 0:
                save_progress(progress)
                save_image_cache(image_cache)

            time.sleep(0.1)  # Small delay

    # Final save
    save_progress(progress)
    save_image_cache(image_cache)

    print(f"\n{'=' * 60}")
    print(f"Migration Complete!")
    print(f"{'=' * 60}")
    print(f"  Migrated:  {total_migrated}")
    print(f"  Skipped:   {total_skipped} (duplicates or empty)")
    print(f"  Failed:    {total_failed}")
    print(f"  Previously migrated: {len(progress['migrated']) - total_migrated}")
    print(f"\nProgress saved to: {PROGRESS_FILE}")
    print(f"Image cache saved to: {IMAGE_CACHE_FILE}")

    if total_failed > 0:
        print(f"\nFailed posts:")
        for key, info in progress["failed"].items():
            print(f"  {key}: {info['title'][:50]} - {info.get('error','')}")
        print(f"\nRe-run this script to retry failed posts.")


if __name__ == "__main__":
    main()

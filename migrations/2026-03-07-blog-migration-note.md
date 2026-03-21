# Blog Migration Note - 2026-03-07

## What was done

Migrated all blog posts from two WordPress sites into star-backend:

- **http://tingmeow.pinkjelly.org/** (682 posts, frontend dev notes)
- **http://pinkjelly.org/** (391 posts, personal blog)

Total: 1,041 posts migrated, 31 skipped (duplicates/empty), all images re-uploaded to DigitalOcean Spaces CDN.

## Files to delete (no longer needed)

```bash
rm /srv/projects/migrate-blogs.py
rm /srv/projects/fix-chinese-images.py
rm /srv/projects/migrate-blogs-progress.json
rm /srv/projects/migrate-blogs-imagecache.json
rm /srv/projects/2026-03-07-blog-migration-note.md
```

## Category mapping used

- tingmeow (mostly tech): Uncategorized/前端开发/react/jQuery/etc. -> 科技(6), 日記(2), 食記->生活(5), 遊戲->ACG(1)
- pinkjelly (personal): Uncategorized->生活(5), 日記(11)->日記(2), 小說閱讀心得->小說心得(4), 寶石魔法/人妻嘉玲/魔王的妻子->小說(12), 追劇->ACG(1), 讀書心得->小說心得(4)

## Notes

- Source WordPress sites are planned to be shut down.
- All images were re-uploaded to cute33.sgp1.cdn.digitaloceanspaces.com so they won't break.
- 149 images with Chinese characters in filenames were fixed in a second pass.

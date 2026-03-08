# Batch Blog Post Generator Prompt

Copy the prompt below and paste it to Claude Code. Change the **topic**, **count**, and **category** as needed.

---

## Prompt Template

```
幫我的部落格寫 [數量] 篇關於 [主題] 的文章，直接插入 DB 發佈（isDraft: false）。

規則：
1. 先用 API 查詢所有現有文章標題，確保不重複
2. 使用分類 ID: [分類ID]（如果分類不存在，先用 POST /api/categories 建立）
3. 文章用繁體中文撰寫
4. 每篇文章的 content 使用 HTML 格式（<p> 標籤）
5. 直接發佈，不要 DRAFT，不要問我確認
6. 透過 API 插入：POST http://localhost:5002/api/posts，Header: X-API-Key: EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI=

主題方向（可選，自由發揮也可以）：
- [方向1]
- [方向2]
- [方向3]
```

---

## Examples

### Example 1: 30 articles about AI
```
幫我的部落格寫 30 篇關於 AI 的文章，直接插入 DB 發佈（isDraft: false）。

規則：
1. 先用 API 查詢所有現有文章標題，確保不重複
2. 使用分類 ID: 18
3. 文章用繁體中文撰寫
4. 每篇文章的 content 使用 HTML 格式（<p> 標籤）
5. 直接發佈，不要 DRAFT，不要問我確認
6. 透過 API 插入：POST http://localhost:5002/api/posts，Header: X-API-Key: EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI=

主題方向：
- AI 不會做的事
- AI 的觀點與看法
- AI 對社會的影響
```

### Example 2: 10 articles about travel
```
幫我的部落格寫 10 篇關於旅遊的文章，直接插入 DB 發佈（isDraft: false）。

規則：
1. 先用 API 查詢所有現有文章標題，確保不重複
2. 使用分類 ID: 7
3. 文章用繁體中文撰寫
4. 每篇文章的 content 使用 HTML 格式（<p> 標籤）
5. 直接發佈，不要 DRAFT，不要問我確認
6. 透過 API 插入：POST http://localhost:5002/api/posts，Header: X-API-Key: EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI=

主題方向：
- 日本小眾景點
- 旅途中的故事
- 旅行省錢技巧
```

### Example 3: 15 articles about science
```
幫我的部落格寫 15 篇關於自然科學的文章，直接插入 DB 發佈（isDraft: false）。

規則：
1. 先用 API 查詢所有現有文章標題，確保不重複
2. 使用分類 ID: 8
3. 文章用繁體中文撰寫
4. 每篇文章的 content 使用 HTML 格式（<p> 標籤）
5. 直接發佈，不要 DRAFT，不要問我確認
6. 透過 API 插入：POST http://localhost:5002/api/posts，Header: X-API-Key: EkmwsqchvB4zdrTXG6jeQam3k1chdow+h9cOSQ2rqbI=

主題方向：
- 冷知識
- 宇宙奧秘
- 日常現象的科學解釋
```

---

## Quick Shorthand Prompts (No Confirmation)

The key phrase is **「不要問我確認」** or **「全部寫好一次發佈」** — this tells Claude to batch everything and publish at once without asking for approval each time.

### Continue a novel (batch mode)
```
繼續寫[小說名] [N]篇，全部寫好一次發佈，不要問我確認。
```

Examples:
```
繼續寫萬界藥神 30篇，全部寫好一次發佈，不要問我確認。
繼續寫星辰藥師 20篇，全部寫好一次發佈，不要問我確認。
繼續寫灰鴉學院 30篇，全部寫好一次發佈，不要問我確認。
```

### General articles (batch mode)
```
幫我寫 [N] 篇[主題]文章，全部寫好一次發佈，不要問我確認。
```

Examples:
```
幫我寫 10 篇旅遊文章，全部寫好一次發佈，不要問我確認。
幫我寫 15 篇科學冷知識，全部寫好一次發佈，不要問我確認。
```

### Extra options you can add
- **補上作者** — Add author credit to each post
- **每篇都加作者（Claude）** — Add "作者：Claude" to each post

---

## Category IDs Reference

| ID | Name |
|----|------|
| 1  | ACG |
| 2  | 日記 |
| 3  | 以月光為契 |
| 4  | 小說心得 |
| 5  | 生活 |
| 6  | 科技 |
| 7  | 旅遊 |
| 8  | 自然科學 |
| 9  | 地理 |
| 10 | 數學 |
| 11 | 詩 |
| 12 | 小說 |
| 13 | 笑話 |
| 14 | 文化 |
| 15 | 星辰藥師 |
| 16 | 萬界藥神 |
| 17 | 灰鴉學院 |
| 18 | AI |
| 19 | 十君令 |

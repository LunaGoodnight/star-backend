# Frontend Login Guide

This backend supports two ways to authenticate from a frontend:

- JWT-based login using the `/api/auth/login` endpoint (recommended for browser apps)
- API Key header for server-to-server or admin tools (not recommended to embed in a public frontend)

Use the JWT flow for any browser-based UI. Use the API Key only in secure, non-public environments.

---

## Table of Contents

- [1) JWT Login (recommended for frontend)](#1-jwt-login-recommended-for-frontend)
  - [Request](#request)
  - [Response](#response)
  - [Use the token](#use-the-token)
  - [Example: Fetch (vanilla JS)](#example-fetch-vanilla-js)
  - [Example: Axios](#example-axios-reactvueetc)
  - [Token storage guidance](#token-storage-guidance)
- [2) API Key header (non-browser/admin tooling)](#2-api-key-header-for-non-browseradmin-tooling)
- [3) Which endpoints are protected?](#3-which-endpoints-are-protected)
- [4) CORS and allowed origins](#4-cors-and-allowed-origins)
- [5) Base URL and ports](#5-base-url-and-ports)
- [6) Troubleshooting](#6-troubleshooting)
- [7) Learn more](#7-learn-more)

---

## 1) JWT Login (recommended for frontend)

Endpoint: `POST /api/auth/login`

### Request

Body (JSON):

```json
{
  "username": "admin",
  "password": "changeme"
}
```

Notes:
- Default credentials can be overridden with configuration keys:
  - `Admin:Username`
  - `Admin:Password`
- JWT settings (`Issuer`, `Audience`, `Key`, `ExpireMinutes`) live under the `Jwt` section (appsettings or environment variables).

### Response

Successful response example:

```json
{
  "token": "<jwt-token>",
  "tokenType": "Bearer",
  "expiresAt": "2025-10-25T12:34:56Z",
  "user": { "username": "admin", "role": "Admin" }
}
```

### Use the token

Add the token to the `Authorization` header for all protected requests:

```
Authorization: Bearer <jwt-token>
```

### Example: Fetch (vanilla JS)

```js
async function login(username, password) {
  const res = await fetch(`${BASE_URL}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || `Login failed (${res.status})`);
  }
  const data = await res.json();
  // Store token in memory or in httpOnly cookie via your backend. Avoid localStorage if possible.
  sessionStorage.setItem('starapi_token', data.token);
  return data;
}

async function authFetch(input, init = {}) {
  const token = sessionStorage.getItem('starapi_token');
  const headers = new Headers(init.headers || {});
  if (token) headers.set('Authorization', `Bearer ${token}`);
  return fetch(input, { ...init, headers });
}

// Usage:
// await login('admin', 'changeme');
// const res = await authFetch(`${BASE_URL}/api/posts`, {
//   method: 'POST',
//   headers: { 'Content-Type': 'application/json' },
//   body: JSON.stringify({ title: 'Hello', content: 'World', isDraft: false })
// });
```

### Example: Axios (React/Vue/etc.)

```js
import axios from 'axios';

export const api = axios.create({ baseURL: BASE_URL });

export async function login(username, password) {
  const { data } = await api.post('/api/auth/login', { username, password });
  api.defaults.headers.common['Authorization'] = `Bearer ${data.token}`;
  return data;
}

// Example authorized request:
// await login('admin', 'changeme');
// await api.post('/api/posts', { title: 'Hello', content: 'World', isDraft: false });
```

### Token storage guidance

- Prefer keeping the JWT in memory state (e.g., React context) or `sessionStorage`. Avoid `localStorage` to reduce XSS risk.
- For best security, implement an HTTP-only, secure cookie token pattern with a small backend helper. This project currently returns the token in JSON; if you need cookie-based auth, add an endpoint that sets an httpOnly cookie.

---

## 2) API Key header (for non-browser/admin tooling)

Do not embed your API key in a public frontend. Anyone can view the JS bundle and extract it.

To use API key auth (e.g., from server scripts or Postman), send header:

```
X-API-Key: your-secret-api-key
```

In code (Node/server-side):

```js
const res = await fetch(`${BASE_URL}/api/posts`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': process.env.STARAPI_KEY
  },
  body: JSON.stringify({ title: 'Server Post', content: 'From server', isDraft: false })
});
```

---

## 3) Which endpoints are protected?

- Public: `GET /api/posts`, `GET /api/posts/{id}`
- Protected (require JWT Bearer or API Key):
  - `POST /api/posts`
  - `PUT /api/posts/{id}`
  - `DELETE /api/posts/{id}`
  - `GET /api/auth/me`

---

## 4) CORS and allowed origins

The backend enables CORS with an allowlist. In development it defaults to:

- http://localhost:3000
- http://localhost:5173

If your frontend runs on a different origin/port, update `AllowedOrigins` in `appsettings.json` or environment variables.

---

## 5) Base URL and ports

Depending on how you run the API, the URL may differ:

- Docker Compose in this repo maps the API to `http://localhost:5002`
- `dotnet run` may use `http://localhost:5000` or `https://localhost:5001`
- See `PORT_CONFIGURATION.md` for details and adjust `BASE_URL` accordingly

Swagger UI is available at: `{BASE_URL}/swagger`

---

## 6) Troubleshooting

- 401 Unauthorized on protected routes:
  - Ensure `Authorization: Bearer <token>` is present, or `X-API-Key` header for API key flow.
  - Token may be expired; log in again.
- 403 / CORS error in browser console:
  - Add your frontend origin to `AllowedOrigins` and restart the API.
- Cannot log in with default credentials:
  - Check `Admin:Username` and `Admin:Password` configuration (`appsettings.json`, environment, or user-secrets).
- Verify the API is reachable at the expected `BASE_URL` and ports.

---

## 7) Learn more

- Explore interactive docs at `{BASE_URL}/swagger` to send login and authorized requests.
- Review `StarApi/StarApi/Controllers/AuthController.cs` for the exact login contract.
- Review `StarApi/StarApi/Program.cs` for CORS and authentication setup.

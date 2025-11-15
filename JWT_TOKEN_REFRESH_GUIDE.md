# JWT Token Refresh Guide

## When Should Frontend Refresh JWT Token?

There are multiple strategies for refreshing JWT tokens. The best approach is to combine them for optimal user experience.

---

## Strategy Comparison

| Strategy | How It Works | User Experience | Complexity | Recommended |
|----------|--------------|-----------------|------------|-------------|
| **Timer (Proactive)** | Frontend calculates expiration, sets timer | Seamless, no interruption | Medium | ✅ Yes |
| **401 Error (Reactive)** | Wait for server to reject request | Brief delay when expires | Simple | ✅ As backup |
| **On Startup** | Check token validity when app loads | Good initial check | Easy | ✅ Yes |
| **Silent Refresh** | Background automatic refresh | Best UX | Complex | ✅ Production |

---

## 1. Timer-Based Refresh (Proactive) - Recommended

Frontend **decodes JWT** to get expiration time and **sets a timer** to refresh BEFORE expiration.

### How JWT Contains Expiration

```javascript
// JWT Token Structure
// Header.Payload.Signature
eyJhbGci...  .  eyJzdWIi...  .  SflKxw...
```

**Decoded Payload:**
```json
{
  "sub": "user123",
  "iat": 1730505600,    // Issued At (Unix timestamp)
  "exp": 1730509200     // Expires At (Unix timestamp) ← Frontend reads this!
}
```

### Implementation

```javascript
import jwtDecode from 'jwt-decode';

function startRefreshTimer(token) {
  // Decode JWT to get expiration time
  const decoded = jwtDecode(token);

  // Calculate time until expiration
  const expiresAt = decoded.exp * 1000; // Convert to milliseconds
  const now = Date.now();
  const timeUntilExpiry = expiresAt - now;

  // Refresh 2-3 minutes before expiry (configurable)
  const REFRESH_BUFFER = 2 * 60 * 1000; // 2 minutes in ms
  const refreshIn = timeUntilExpiry - REFRESH_BUFFER;

  // Set timer
  const timerId = setTimeout(async () => {
    try {
      const newToken = await refreshToken();
      startRefreshTimer(newToken); // Schedule next refresh
    } catch (error) {
      console.error('Token refresh failed:', error);
      // Redirect to login
      window.location.href = '/login';
    }
  }, Math.max(refreshIn, 0));

  return timerId;
}

// Refresh token API call
async function refreshToken() {
  const refreshToken = localStorage.getItem('refreshToken');
  const response = await axios.post('/api/auth/refresh', {
    refreshToken
  });

  const newToken = response.data.token;
  localStorage.setItem('token', newToken);

  return newToken;
}

// Usage after login
const token = loginResponse.data.token;
startRefreshTimer(token);
```

### Timeline Example (15-minute token)

```
00:00 - Login → Get JWT (expires at 00:15)
00:00 - Calculate: refresh at 00:13 (2 min buffer)
00:00 - Set timer for 13 minutes
        ↓
        ... user continues using app ...
        ↓
00:13 - Timer triggers → Call /api/auth/refresh
00:13 - Get new JWT (expires at 00:28)
00:13 - Set new timer for 13 minutes
        ↓
        ... cycle repeats automatically ...
```

---

## 2. Handle 401 Unauthorized (Reactive) - Backup Strategy

Wait for server to return **401** when token expires, then refresh and retry.

### Implementation with Axios Interceptor

```javascript
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });

  failedQueue = [];
};

axios.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // If 401 and haven't retried yet
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        // Another request is already refreshing, queue this request
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then(token => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return axios(originalRequest);
        }).catch(err => {
          return Promise.reject(err);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const newToken = await refreshToken();

        // Update Authorization header
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        axios.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;

        processQueue(null, newToken);

        // Retry the original request
        return axios(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        // Refresh failed, redirect to login
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
```

### Timeline Example

```
00:00 - Login → Get JWT (expires at 00:15)
        ↓
        ... user continues using app ...
        ↓
00:15 - Make API request
00:15 - Server responds: 401 Unauthorized
00:15 - Interceptor catches 401
00:15 - Call /api/auth/refresh
00:15 - Get new JWT
00:15 - Retry the failed request with new token
00:15 - Request succeeds!
```

---

## 3. Check on Application Startup

Verify token validity when the app initializes.

```javascript
// React example
useEffect(() => {
  const token = localStorage.getItem('token');

  if (!token) {
    // No token, redirect to login
    window.location.href = '/login';
    return;
  }

  try {
    const decoded = jwtDecode(token);
    const now = Date.now() / 1000; // Current time in seconds

    if (decoded.exp < now) {
      // Token already expired, try to refresh
      refreshToken().catch(() => {
        window.location.href = '/login';
      });
    } else if (decoded.exp < now + (5 * 60)) {
      // Token expires within 5 minutes, refresh proactively
      refreshToken();
    } else {
      // Token is valid, start refresh timer
      startRefreshTimer(token);
    }
  } catch (error) {
    // Invalid token, redirect to login
    window.location.href = '/login';
  }
}, []);
```

---

## 4. Complete Token Manager Class

A comprehensive implementation combining all strategies:

```javascript
class TokenManager {
  constructor() {
    this.refreshTimer = null;
    this.isRefreshing = false;
    this.failedQueue = [];
  }

  // Start automatic refresh timer
  startRefreshTimer(token) {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
    }

    try {
      const decoded = jwtDecode(token);
      const expiresAt = decoded.exp * 1000;
      const now = Date.now();
      const timeUntilExpiry = expiresAt - now;

      // Refresh 2 minutes before expiry
      const refreshIn = Math.max(timeUntilExpiry - (2 * 60 * 1000), 0);

      this.refreshTimer = setTimeout(async () => {
        try {
          const newToken = await this.refreshToken();
          this.startRefreshTimer(newToken);
        } catch (error) {
          console.error('Auto refresh failed:', error);
          this.handleRefreshError();
        }
      }, refreshIn);

      console.log(`Token will be refreshed in ${Math.round(refreshIn / 1000)} seconds`);
    } catch (error) {
      console.error('Failed to decode token:', error);
      this.handleRefreshError();
    }
  }

  // Stop refresh timer
  stopRefreshTimer() {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  // Refresh token API call
  async refreshToken() {
    const refreshToken = localStorage.getItem('refreshToken');

    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const response = await axios.post('/api/auth/refresh', {
      refreshToken
    });

    const newToken = response.data.token;
    const newRefreshToken = response.data.refreshToken; // If using token rotation

    localStorage.setItem('token', newToken);
    if (newRefreshToken) {
      localStorage.setItem('refreshToken', newRefreshToken);
    }

    // Update axios default header
    axios.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;

    return newToken;
  }

  // Check token validity
  isTokenValid(token) {
    if (!token) return false;

    try {
      const decoded = jwtDecode(token);
      const now = Date.now() / 1000;
      return decoded.exp > now;
    } catch {
      return false;
    }
  }

  // Initialize on app startup
  async initialize() {
    const token = localStorage.getItem('token');

    if (!token) {
      return false; // No token, user needs to login
    }

    if (!this.isTokenValid(token)) {
      // Token expired, try to refresh
      try {
        const newToken = await this.refreshToken();
        this.startRefreshTimer(newToken);
        return true;
      } catch {
        return false; // Refresh failed, user needs to login
      }
    }

    // Token is valid, start timer
    this.startRefreshTimer(token);
    return true;
  }

  // Handle refresh error
  handleRefreshError() {
    this.stopRefreshTimer();
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    window.location.href = '/login';
  }

  // Logout
  logout() {
    this.stopRefreshTimer();
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    delete axios.defaults.headers.common['Authorization'];
  }
}

// Create singleton instance
const tokenManager = new TokenManager();

export default tokenManager;
```

### Usage Example

```javascript
// In your main app file (App.js, main.js, etc.)
import tokenManager from './utils/tokenManager';

// On app startup
async function initializeApp() {
  const isAuthenticated = await tokenManager.initialize();

  if (!isAuthenticated) {
    window.location.href = '/login';
  }
}

initializeApp();

// After login
async function handleLogin(username, password) {
  const response = await axios.post('/api/auth/login', {
    username,
    password
  });

  const { token, refreshToken } = response.data;

  localStorage.setItem('token', token);
  localStorage.setItem('refreshToken', refreshToken);

  axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;

  // Start automatic refresh
  tokenManager.startRefreshTimer(token);

  // Redirect to dashboard
  window.location.href = '/dashboard';
}

// On logout
function handleLogout() {
  tokenManager.logout();
  window.location.href = '/login';
}
```

---

## Best Practices

### 1. Token Expiration Times

| Token Type | Recommended Duration | Your Setting |
|------------|---------------------|--------------|
| Access Token | 15-60 minutes | 15 minutes ✓ |
| Refresh Token | 7-30 days | Configure as needed |

### 2. Refresh Timing

```javascript
// For 15-minute access token
const REFRESH_BUFFER = 2 * 60 * 1000; // Refresh 2 minutes before expiry

// For 60-minute access token
const REFRESH_BUFFER = 5 * 60 * 1000; // Refresh 5 minutes before expiry
```

### 3. Security Considerations

- ✅ Store **access token** in memory or localStorage
- ✅ Store **refresh token** in httpOnly cookie (most secure) or localStorage
- ✅ Use HTTPS only
- ✅ Implement token rotation (issue new refresh token on each refresh)
- ✅ Clear all tokens on logout
- ✅ Handle multiple tabs (use BroadcastChannel or localStorage events)
- ✅ Add rate limiting to refresh endpoint
- ✅ Implement token blacklisting for logout

### 4. Error Handling

```javascript
// Handle different error scenarios
try {
  const newToken = await refreshToken();
} catch (error) {
  if (error.response?.status === 401) {
    // Refresh token invalid or expired
    console.log('Session expired, please login again');
    redirectToLogin();
  } else if (error.response?.status === 429) {
    // Rate limit exceeded
    console.log('Too many refresh attempts, please try again later');
  } else {
    // Network or server error
    console.log('Unable to refresh token, please check your connection');
  }
}
```

### 5. Multi-Tab Synchronization

```javascript
// Sync token refresh across multiple tabs
class TokenManager {
  constructor() {
    // Listen for storage changes
    window.addEventListener('storage', (e) => {
      if (e.key === 'token' && e.newValue) {
        // Another tab refreshed the token
        this.stopRefreshTimer();
        this.startRefreshTimer(e.newValue);
      }
    });
  }
}
```

---

## Configuration for Your API

Based on your `appsettings.json`:

```json
{
  "Jwt": {
    "ExpireMinutes": 15
  }
}
```

**Recommended Frontend Configuration:**

```javascript
const TOKEN_CONFIG = {
  accessTokenExpiry: 15 * 60 * 1000,        // 15 minutes
  refreshBuffer: 2 * 60 * 1000,              // Refresh 2 minutes before expiry
  refreshTokenExpiry: 7 * 24 * 60 * 60 * 1000  // 7 days (configure on backend)
};
```

---

## Testing Your Implementation

### Test 1: Verify Token Expiration Reading

```javascript
const token = "your-jwt-token";
const decoded = jwtDecode(token);
console.log('Expires at:', new Date(decoded.exp * 1000));
console.log('Time until expiry:', (decoded.exp * 1000 - Date.now()) / 1000, 'seconds');
```

### Test 2: Test Automatic Refresh

```javascript
// Set a short expiry for testing (e.g., 2 minutes)
// Watch console for "Token will be refreshed in X seconds"
// Verify refresh happens automatically
```

### Test 3: Test 401 Handling

```javascript
// Let token expire naturally
// Make an API request
// Verify it automatically refreshes and retries
```

### Test 4: Test Multi-Tab Behavior

```javascript
// Open app in two tabs
// Let one tab refresh the token
// Verify both tabs update correctly
```

---

## Summary

### Recommended Approach: Hybrid Strategy

Use **all three strategies** together:

1. ✅ **Timer-based refresh** (main method) - Refresh 2-3 minutes before expiry
2. ✅ **Check on startup** - Validate token when app loads
3. ✅ **Handle 401 errors** (backup) - Catch any missed refreshes

### Quick Implementation Checklist

- [ ] Install `jwt-decode` library: `npm install jwt-decode`
- [ ] Create TokenManager class with timer logic
- [ ] Add axios interceptor for 401 handling
- [ ] Initialize TokenManager on app startup
- [ ] Start timer after successful login
- [ ] Stop timer and clear tokens on logout
- [ ] Test with short token expiry (2-3 minutes)
- [ ] Configure production expiry (15 minutes)
- [ ] Add multi-tab synchronization
- [ ] Implement proper error handling

### Visual Flow

```
User Login
    ↓
Store: token + refreshToken
    ↓
Decode JWT → Read 'exp' field
    ↓
Calculate: refreshTime = exp - 2min
    ↓
Set Timer
    ↓
┌───────────────────────────────┐
│  Normal Usage (13 minutes)    │
│  - Make API calls             │
│  - Timer running in background│
└───────────────────────────────┘
    ↓
Timer Triggers (at 13 min)
    ↓
POST /api/auth/refresh
    ↓
Get New Token
    ↓
Update Storage
    ↓
Set New Timer
    ↓
Cycle Repeats
```

---

## Additional Resources

- [JWT.io](https://jwt.io/) - Decode and verify JWT tokens
- [jwt-decode NPM](https://www.npmjs.com/package/jwt-decode) - JWT decoder library
- [RFC 7519](https://tools.ietf.org/html/rfc7519) - JWT specification

---

**Remember:** The key is to **decode the JWT to read the `exp` field**, then **set a timer** to refresh proactively, with **401 handling as a backup**!

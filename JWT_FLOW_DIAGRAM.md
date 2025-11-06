# JWT Authentication Flow Diagram

```
┌─────────┐                                              ┌─────────┐
│         │                                              │         │
│ Client  │                                              │ Server  │
│         │                                              │         │
└────┬────┘                                              └────┬────┘
     │                                                        │
     │  1. POST /login (username, password)                  │
     │───────────────────────────────────────────────────────>│
     │                                                        │
     │                                                        │  2. Validate
     │                                                        │     Credentials
     │                                                        │
     │  3. Return JWT Token                                  │
     │<───────────────────────────────────────────────────────│
     │    {                                                   │
     │      "token": "eyJhbGci...",                          │
     │      "refreshToken": "..."                            │
     │    }                                                   │
     │                                                        │
     │  4. Store JWT (localStorage/cookie)                   │
     │                                                        │
     │                                                        │
     │  5. API Request with JWT in Header                    │
     │     Authorization: Bearer eyJhbGci...                 │
     │───────────────────────────────────────────────────────>│
     │                                                        │
     │                                                        │  6. Verify JWT
     │                                                        │     - Signature
     │                                                        │     - Expiration
     │                                                        │     - Claims
     │                                                        │
     │  7. Return Protected Resource                         │
     │<───────────────────────────────────────────────────────│
     │    { "data": [...] }                                  │
     │                                                        │
     │                                                        │
     │  8. If Token Expired: 401 Unauthorized                │
     │<───────────────────────────────────────────────────────│
     │                                                        │
     │                                                        │
     │  9. POST /refresh (refreshToken)                      │
     │───────────────────────────────────────────────────────>│
     │                                                        │
     │                                                        │  10. Validate
     │                                                        │      Refresh Token
     │                                                        │
     │  11. Return New JWT Token                             │
     │<───────────────────────────────────────────────────────│
     │     { "token": "eyJhbGci..." }                        │
     │                                                        │
     │                                                        │
     │  12. Continue Making Requests                         │
     │───────────────────────────────────────────────────────>│
     │                                                        │
```

## JWT Token Structure

```
Header.Payload.Signature

eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9  .  eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ  .  SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
└────────────── Header ──────────────┘    └──────────────────────────── Payload ────────────────────────────┘    └──────────── Signature ────────────┘
```

### Header
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

### Payload (Claims)
```json
{
  "sub": "1234567890",
  "name": "John Doe",
  "email": "john@example.com",
  "role": "admin",
  "iat": 1516239022,
  "exp": 1516242622
}
```

### Signature
```
HMACSHA256(
  base64UrlEncode(header) + "." +
  base64UrlEncode(payload),
  secret
)
```

## Flow Steps Explained

1. **Login Request**: Client sends credentials to the server
2. **Credential Validation**: Server validates username and password against database
3. **Token Generation**: Server creates JWT with user claims and signs it
4. **Token Storage**: Client stores JWT (typically in localStorage or httpOnly cookie)
5. **Authenticated Request**: Client includes JWT in Authorization header
6. **Token Verification**: Server validates the token's signature and expiration
7. **Resource Access**: If valid, server returns protected resource
8. **Token Expiration**: Server returns 401 if token is expired
9. **Token Refresh**: Client sends refresh token to get new access token
10. **Refresh Validation**: Server validates refresh token
11. **New Token**: Server issues new JWT access token
12. **Continue**: Client uses new token for subsequent requests

## Security Best Practices

- Store JWT securely (httpOnly cookies preferred over localStorage)
- Use HTTPS to prevent token interception
- Set appropriate expiration times (short for access tokens, longer for refresh tokens)
- Implement token rotation for refresh tokens
- Validate all token claims on the server
- Use strong signing algorithms (RS256 or HS256 with strong secret)
- Implement token blacklisting for logout functionality

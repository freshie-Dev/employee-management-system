# EMS authentication setup

## Run locally

1. Start your .NET 9 API at `https://localhost:7241`.
2. In this frontend folder, run `npm ci`, then `npm start`.
3. Visit `http://localhost:4200`. You will be redirected to `/login`.
4. Sign in with a real backend username and password. No demo credentials are included.

`angular.json` now enables `proxy.https.conf.json`. Requests to `/api/**` are forwarded to the .NET API during development. Restart `ng serve` after changing proxy settings. The proxy's `secure: false` accommodates the local development certificate; it is not a production setting.

## Implemented flow

- PrimeNG InputText, Password, Button and Message components with reactive-form validation.
- POST `/api/auth/login` sends `{ username, password }` without a bearer token.
- The returned access token, expiry, user identity and roles are stored in `AuthStore` and `sessionStorage`.
- Reloading the tab restores a valid session. Expired/malformed stored sessions are rejected. Passwords are never persisted.
- Authenticated requests to the configured API receive `Authorization: Bearer <accessToken>` automatically. Other origins and paths do not.
- Login success returns to the requested protected route, or `/dashboard`.
- Dashboard and Appearance sit inside a protected application shell with Sign out.
- Expiry timers and request/navigation checks handle expired tokens. Backend 401 responses end the corresponding session; 403 responses preserve it.
- Logout clears frontend state. The supplied backend contract has no token revocation/logout or refresh endpoint, so there are no calls to those endpoints.

The backend still validates JWTs and enforces roles. Browser storage and route guards are not authorization boundaries. `sessionStorage` is readable by same-origin JavaScript; this implements the existing bearer-token contract. A server-managed HttpOnly cookie approach would require backend changes.

## Make protected API requests

Continue using `ApiClient` inside feature services. Interceptors apply automatically:

```ts
private readonly api = inject(ApiClient);

getDepartments() {
  return this.api.get<Department[]>('departments');
}
```

No token headers or repeated error mapping are needed in feature services. Feature CRUD methods/models remain to be implemented.

For role-restricted child routes use `canActivate: [roleGuard]` and `data: { roles: ['SYSTEM_ADMIN'] }`. These routes already inherit authentication from the shell. Role guard accepts any matching listed role.

Interceptor order is intentional: `errorInterceptor` wraps `authInterceptor`, allowing authentication to handle raw HTTP 401 errors before they become `ApiError` objects.

## Deployment

The Angular development proxy is not part of the production build. Either configure the hosting server to forward `/api` to the API, or override `API_CONFIG` with the absolute backend base URL and configure backend CORS for the frontend origin. The auth interceptor supports both configurations.

## Validation

- Production build passed. Initial bundle: 510.71 kB; existing warning limit: 500 kB. No budget limits were raised.
- 17 automated tests passed, covering login, token boundaries, absolute API URL, 401/403 handling, stale request errors, expiry, logout, session restoration, guards, and the application outlet.
- No live .NET API was available in this environment; verify real credentials and the response shape against your running backend.

Browser visual/smoke testing could not be completed because Chromium was unavailable and its download failed. Angular dev-server startup also hit a host network-interface limitation in this environment. These are separate from the successful production build and automated tests.

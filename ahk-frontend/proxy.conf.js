// Dev proxy: makes the SPA and API same-origin so the Identity cookie flows and no CORS config is needed.
// `secure: false` accepts the backend's self-signed dev certificate.
//
// The OIDC paths must be proxied too, and they are NOT under /api:
//   /signin-oidc, /signout-callback-oidc — the provider redirects the browser here, and the resulting
//     Identity cookie has to be set on the :4200 origin, so it must arrive through the proxy.
//   /mock-oidc — the development stand-in for the BME IdP served by the backend (MockOidc/).
//
// Because `changeOrigin: true` rewrites the Host header to the backend, the backend cannot infer the
// browser's origin; the redirect_uri is therefore pinned explicitly via Authentication:Oidc:RedirectUri.
const target = 'https://localhost:7443';

module.exports = [
  {
    context: ['/api', '/signin-oidc', '/signout-callback-oidc', '/mock-oidc'],
    target,
    secure: false,
    changeOrigin: true,
  },
];

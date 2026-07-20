// Dev proxy: makes the SPA and API same-origin so the Identity cookie flows and no CORS config is needed.
// All /api/* calls (and the OIDC challenge/callback under /api/auth/external) are forwarded to the backend
// over HTTPS. `secure: false` accepts the backend's self-signed dev certificate.
module.exports = [
  {
    context: ['/api'],
    target: 'https://localhost:7443',
    secure: false,
    changeOrigin: true,
  },
];

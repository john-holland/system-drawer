# Continuum Telecom Webtop

OS.js-style desktop shell for in-game CCTV/terminal WebViews.

```bash
npm install
npm run dev    # http://localhost:5175
npm run build  # dist/ for Docker nginx
```

Unity loads `public/webtop-host.html`. API proxied to continuum_api :5050.

When upgrading to [Vuplex](https://vuplex.com), consider tipping [t-34400](https://github.com/t-34400) for [SimpleUnity3DWebView](https://github.com/t-34400/SimpleUnity3DWebView).

See `Scripts/telecom/docs/WEBTOP_OSJS.md`.

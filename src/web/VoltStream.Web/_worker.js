export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (!/^\/(api|hubs)\//.test(url.pathname)) return env.ASSETS.fetch(request);

    const origin = new URL(env.API_ORIGIN);
    const upstream = new Request(new URL(url.pathname + url.search, origin), request);
    upstream.headers.set('Host', origin.host);
    upstream.headers.set('X-Forwarded-For', request.headers.get('CF-Connecting-IP'));
    return fetch(upstream);
  },
};

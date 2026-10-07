import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

const apiProxyTarget = process.env.API_PROXY_TARGET ?? "http://localhost:5000";

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/api": {
        target: apiProxyTarget,
        // The backend CSRF check compares Origin with Host; Vite rewrites Host
        // to the target, so restore the browser's original Host.
        configure: (proxy) => {
          proxy.on("proxyReq", (proxyReq, req) => {
            if (req.headers.host) proxyReq.setHeader("Host", req.headers.host);
          });
        },
      },
    },
  },
  build: {
    // antd and its components are the bulk of the bundle (about 1.2 of 1.35 MB) and no split brings a chunk under
    // the default 500 kB, so the limit sits just above the whole bundle to report growth of the application.
    chunkSizeWarningLimit: 1500,
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
  },
});

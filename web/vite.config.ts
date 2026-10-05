import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

const apiProxyTarget = process.env.API_PROXY_TARGET ?? "http://localhost:5000";

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: { "/api": apiProxyTarget },
  },
  build: {
    chunkSizeWarningLimit: 1500,
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
  },
});

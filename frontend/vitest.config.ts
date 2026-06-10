import react from "@vitejs/plugin-react";
import { defineConfig, mergeConfig } from "vitest/config";
import viteConfig from "./vite.config.js";

export default mergeConfig(
  viteConfig,
  defineConfig({
    plugins: [react()],
    test: {
      environment: "jsdom",
      setupFiles: ["src/test-setup.ts"],
      include: ["src/**/*.spec.{ts,tsx}"],
      exclude: ["e2e/**", "node_modules/**"],
    },
  }),
);

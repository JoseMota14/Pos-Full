import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// export default defineConfig({
//   plugins: [react()],
//   server: {
//     port: 5173,
//     proxy: {
//       "/api": "http://localhost:5000",
//       "/hubs": {
//         target: "http://localhost:5000",
//         ws: true
//       }
//     }
//   }
// });

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "https://localhost:44307",
        changeOrigin: true,
        secure: false
      },
      "/hubs": {
        target: "https://localhost:44307",
        changeOrigin: true,
        secure: false,
        ws: true
      }
    }
  }
});
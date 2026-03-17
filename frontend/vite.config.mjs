import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import basicSsl from "@vitejs/plugin-basic-ssl";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
    plugins: [react(), basicSsl(), tailwindcss()],
    server: {
        host: "localhost",
        port: 5173,
        https: true
    },
    preview: {
        host: "localhost",
        port: 4173,
        https: true
    },
    build: {
        outDir: "../src/Wfm.DemandModule.Api/wwwroot",
        emptyOutDir: true
    }
});

import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
    plugins: [react(), tailwindcss()],
    build: {
        outDir: "../src/Wfm.DemandModule.Api/wwwroot",
        emptyOutDir: true
    }
});

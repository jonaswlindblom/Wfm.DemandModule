import { useMemo, useState } from "react";
import { AppBar, Toolbar, Typography, Box, TextField, Button, Chip, MenuItem } from "@mui/material";
import { buildApiUrl, getApiBase, setApiBase } from "../api/client";

export default function TopBar() {
  const [token, setToken] = useState<string>(() => localStorage.getItem("dm_token") ?? "");
  const [apiBase, setApiBaseState] = useState<string>(() => getApiBase());
  const [userId, setUserId] = useState("jonas");
  const [role, setRole] = useState("Admin");
  const [status, setStatus] = useState("");

  const tokenState = useMemo(() => {
    if (!token) return { label: "No token", color: "default" as const };
    return { label: "Token set", color: "success" as const };
  }, [token]);

  async function issueToken() {
      setStatus("Issuing token...");
      try {
      const response = await fetch(buildApiUrl(apiBase, "/auth/token"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, role }),
      });

      if (!response.ok) {
        const text = await response.text();
        throw new Error(text || `HTTP ${response.status}`);
      }

      const body = await response.json() as { accessToken: string };
      setToken(body.accessToken);
      localStorage.setItem("dm_token", body.accessToken);
      setStatus("Token issued and saved.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Failed to issue token.");
    }
  }

  return (
    <AppBar position="static">
      <Toolbar sx={{ gap: 2, alignItems: "center", flexWrap: "wrap", py: 1 }}>
        <Typography variant="h6" sx={{ whiteSpace: "nowrap" }}>
          Demand Module Prototype
        </Typography>

        <Box sx={{ display: "flex", gap: 1, alignItems: "center", flex: 1 }}>
          <TextField
            size="small"
            label="API Base"
            value={apiBase}
            onChange={(e) => setApiBaseState(e.target.value)}
            sx={{ minWidth: 360, bgcolor: "rgba(255,255,255,0.12)", borderRadius: 1 }}
            InputLabelProps={{ style: { color: "rgba(255,255,255,0.8)" } }}
            inputProps={{ style: { color: "white" } }}
          />
          <Button
            variant="outlined"
            color="inherit"
            onClick={() => {
              setApiBase(apiBase.trim());
              window.location.reload();
            }}
          >
            Set API
          </Button>
        </Box>

        <Chip label={tokenState.label} color={tokenState.color} variant="filled" />

        <TextField
          size="small"
          label="User"
          value={userId}
          onChange={(e) => setUserId(e.target.value)}
          sx={{ minWidth: 120, bgcolor: "rgba(255,255,255,0.12)", borderRadius: 1 }}
          InputLabelProps={{ style: { color: "rgba(255,255,255,0.8)" } }}
          inputProps={{ style: { color: "white" } }}
        />

        <TextField
          select
          size="small"
          label="Role"
          value={role}
          onChange={(e) => setRole(e.target.value)}
          sx={{ minWidth: 140, bgcolor: "rgba(255,255,255,0.12)", borderRadius: 1 }}
          InputLabelProps={{ style: { color: "rgba(255,255,255,0.8)" } }}
          inputProps={{ style: { color: "white" } }}
        >
          <MenuItem value="Admin">Admin</MenuItem>
          <MenuItem value="Planner">Planner</MenuItem>
          <MenuItem value="Viewer">Viewer</MenuItem>
        </TextField>

        <Button variant="contained" color="inherit" onClick={issueToken}>
          Get token
        </Button>

        <TextField
          size="small"
          label="JWT Token"
          value={token}
          onChange={(e) => setToken(e.target.value)}
          sx={{ minWidth: 420, bgcolor: "rgba(255,255,255,0.12)", borderRadius: 1 }}
          InputLabelProps={{ style: { color: "rgba(255,255,255,0.8)" } }}
          inputProps={{ style: { color: "white" } }}
        />
        <Button
          variant="contained"
          color="secondary"
          onClick={() => {
            localStorage.setItem("dm_token", token.trim());
            window.location.reload();
          }}
        >
          Save token
        </Button>
        <Button
          variant="outlined"
          color="inherit"
          onClick={() => {
            localStorage.removeItem("dm_token");
            window.location.reload();
          }}
        >
          Clear
        </Button>

        {status ? (
          <Typography variant="caption" sx={{ color: "rgba(255,255,255,0.85)" }}>
            {status}
          </Typography>
        ) : null}
      </Toolbar>
    </AppBar>
  );
}

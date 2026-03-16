import { useMemo, useState } from "react";
import { AppBar, Toolbar, Typography, Box, TextField, Button, Chip } from "@mui/material";
import { getApiBase, setApiBase } from "../api/client";

export default function TopBar() {
  const [token, setToken] = useState<string>(() => localStorage.getItem("dm_token") ?? "");
  const [apiBase, setApiBaseState] = useState<string>(() => getApiBase());

  const tokenState = useMemo(() => {
    if (!token) return { label: "No token", color: "default" as const };
    return { label: "Token set", color: "success" as const };
  }, [token]);

  return (
    <AppBar position="static">
      <Toolbar sx={{ gap: 2, alignItems: "center" }}>
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
      </Toolbar>
    </AppBar>
  );
}

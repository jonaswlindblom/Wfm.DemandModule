import { useState } from "react";
import { api } from "../api/client";
import { Button, Card, CardContent, TextField, Typography } from "@mui/material";
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from "recharts";

type LatestMappingResp = { version: { id: string } };
type Bucket = { activityId: string; intervalStartUtc: string; hours: number };

export default function SimulationPage() {
  const [streamId, setStreamId] = useState("");
  const [simId, setSimId] = useState("");
  const [data, setData] = useState<any[]>([]);
  const [status, setStatus] = useState("");

  async function run() {
    setStatus("Loading...");
    const latest = await api<LatestMappingResp>(`/streams/${streamId}/mappings/latest`);

    const now = new Date();
    const from = new Date(now.getTime() - 24 * 3600 * 1000).toISOString();
    const to = new Date(now.getTime() + 1 * 3600 * 1000).toISOString();

    const create = await api<{ simulationId: string }>(`/simulations`, {
      method: "POST",
      body: JSON.stringify({ streamId, mappingVersionId: latest.version.id, fromUtc: from, toUtc: to, intervalMinutes: 60 })
    });

    setSimId(create.simulationId);

    const buckets = await api<Bucket[]>(`/simulations/${create.simulationId}/buckets`);

    const grouped: Record<string, number> = {};
    for (const b of buckets) grouped[b.intervalStartUtc] = (grouped[b.intervalStartUtc] ?? 0) + b.hours;

    const rows = Object.entries(grouped).map(([t, hours]) => ({ t, hours }));
    setData(rows);
    setStatus("Done");
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h5" gutterBottom>Simulation</Typography>
        <Typography variant="body2">Kör historik och visar workload (summa) per timme.</Typography>

        <TextField fullWidth sx={{ mt: 2 }} label="StreamId" value={streamId} onChange={(e) => setStreamId(e.target.value)} />
        <Button sx={{ mt: 2 }} variant="contained" onClick={run} disabled={!streamId}>Run simulation (last 24h)</Button>

        <Typography sx={{ mt: 2 }}>Status: {status} {simId ? `(SimId=${simId})` : ""}</Typography>

        <div style={{ width: "100%", height: 320, marginTop: 16 }}>
          <ResponsiveContainer>
            <LineChart data={data}>
              <XAxis dataKey="t" hide />
              <YAxis />
              <Tooltip />
              <Line type="monotone" dataKey="hours" />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  );
}

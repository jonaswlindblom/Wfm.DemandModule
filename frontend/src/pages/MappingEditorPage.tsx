import { useState } from "react";
import { api } from "../api/client";
import { Button, Card, CardContent, TextField, Typography } from "@mui/material";

type Stream = { id: string; name: string; industry: string; sourceSystem: string };
type Activity = { id: string; code: string; name: string };

export default function MappingEditorPage() {
  const [streamId, setStreamId] = useState("");
  const [status, setStatus] = useState<string>("");

  async function createDemoMapping() {
    setStatus("Working...");

    const stream = await api<Stream>("/streams", {
      method: "POST",
      body: JSON.stringify({ name: "Demo Camping", sourceSystem: "PMS", industry: "camping" })
    });

    const activities = await api<Activity[]>("/activities");
    const reception = activities.find(a => a.code === "Reception");
    const housekeeping = activities.find(a => a.code === "Housekeeping");

    if (!reception || !housekeeping) {
      throw new Error("Seed activities missing. Run sql/002_seed_demo_data.sql first.");
    }

    await api("/streams/" + stream.id + "/mappings", {
      method: "POST",
      body: JSON.stringify({
        name: "Initial camping mapping",
        rules: [
          {
            name: "Booking created (camping)",
            eventType: "CampingBookingCreated",
            conditionExpression: null,
            sortOrder: 1,
            activities: [
              { activityId: reception.id, baseHours: 0.3, unitExpression: "count($.addOns)", perUnitHours: 0.05, multiplierExpression: null },
              { activityId: housekeeping.id, baseHours: 0.0, unitExpression: "stayNights($.checkInDate,$.checkOutDate)", perUnitHours: 0.6, multiplierExpression: "cabinTypeFactor($.cabinType)" }
            ]
          }
        ]
      })
    });

    setStreamId(stream.id);
    setStatus(`Created demo stream + mapping. StreamId=${stream.id}`);
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h5" gutterBottom>Mapping Editor</Typography>
        <Typography variant="body2" gutterBottom>
          Skapar demo-stream + mapping för camping enligt kravet.
        </Typography>

        <Button variant="contained" onClick={createDemoMapping}>Create Demo Camping Mapping</Button>

        <TextField
          fullWidth
          sx={{ mt: 2 }}
          label="StreamId"
          value={streamId}
          onChange={(e) => setStreamId(e.target.value)}
        />

        <Typography sx={{ mt: 2, whiteSpace: "pre-wrap" }}>{status}</Typography>
      </CardContent>
    </Card>
  );
}

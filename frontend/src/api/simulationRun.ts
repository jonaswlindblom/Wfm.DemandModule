import { api } from "./client";
import type { SimulationRunResponse } from "./types";

export function getSimulationRun(from: string, to: string, intervalMinutes: number) {
  const query = new URLSearchParams({
    from,
    to,
    intervalMinutes: intervalMinutes.toString(),
  });

  return api<SimulationRunResponse>(`/simulation/run?${query.toString()}`);
}

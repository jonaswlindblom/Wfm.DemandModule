import { api } from "./client";
import type { SimulationOverviewResponse } from "./types";

export function getSimulationOverview(date: string) {
  const query = new URLSearchParams({ date });
  return api<SimulationOverviewResponse>(`/simulation/overview?${query.toString()}`);
}

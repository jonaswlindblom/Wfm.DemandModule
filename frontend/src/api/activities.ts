import { api } from "./client";
import type { Activity } from "./types";

export function getActivities() {
  return api<Activity[]>("/activities");
}

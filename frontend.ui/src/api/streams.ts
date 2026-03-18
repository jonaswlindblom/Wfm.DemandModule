import { api } from "./client";
import type { Stream } from "./types";

export function getStreams() {
  return api<Stream[]>("/streams");
}

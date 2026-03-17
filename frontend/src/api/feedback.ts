import { api } from "./client";
import type { FeedbackResponse } from "./types";

export async function getFeedback(mappingVersionId?: string): Promise<FeedbackResponse> {
  const params = new URLSearchParams();
  params.set("take", "12");

  if (mappingVersionId) {
    params.set("mappingVersionId", mappingVersionId);
  }

  return api<FeedbackResponse>(`/feedback?${params.toString()}`);
}

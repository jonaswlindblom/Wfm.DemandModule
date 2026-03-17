import { api } from "./client";
import type {
  CreateMappingRequest,
  LatestMappingResponse,
  MappingVersionResponse,
  MappingVersionsResponse,
} from "./types";

export function getMappingVersions(streamId: string) {
  return api<MappingVersionsResponse>(`/streams/${streamId}/mappings`);
}

export function getActiveMapping(streamId: string) {
  return api<LatestMappingResponse>(`/streams/${streamId}/mappings/active`);
}

export function createMappingVersion(streamId: string, body: CreateMappingRequest) {
  return api<MappingVersionResponse>(`/streams/${streamId}/mappings`, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function activateMappingVersion(streamId: string, versionId: string) {
  return api<MappingVersionResponse>(`/streams/${streamId}/mappings/${versionId}/activate`, {
    method: "POST",
  });
}

export function rollbackMappingVersion(streamId: string, versionId: string) {
  return api<MappingVersionResponse>(`/streams/${streamId}/mappings/${versionId}/rollback`, {
    method: "POST",
  });
}

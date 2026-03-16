import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getActivities } from "../api/activities";
import { activateMappingVersion, createMappingVersion, getActiveMapping, getMappingVersions, rollbackMappingVersion } from "../api/mappings";
import { getStreams } from "../api/streams";

const defaultDraft = {
  name: "CampingBookingCreated vNext",
  ruleName: "CampingBookingCreated base rule",
  receptionActivityId: "",
  housekeepingActivityId: "",
};

export default function MappingEditorPage() {
  const queryClient = useQueryClient();
  const [selectedStreamId, setSelectedStreamId] = useState("");
  const [draftName, setDraftName] = useState(defaultDraft.name);
  const [statusMessage, setStatusMessage] = useState("");

  const streamsQuery = useQuery({
    queryKey: ["streams"],
    queryFn: getStreams,
  });

  useEffect(() => {
    if (!selectedStreamId && streamsQuery.data?.length) {
      setSelectedStreamId(streamsQuery.data[0].id);
    }
  }, [selectedStreamId, streamsQuery.data]);

  const versionsQuery = useQuery({
    queryKey: ["mapping-versions", selectedStreamId],
    queryFn: () => getMappingVersions(selectedStreamId),
    enabled: Boolean(selectedStreamId),
  });

  const activeQuery = useQuery({
    queryKey: ["mapping-active", selectedStreamId],
    queryFn: () => getActiveMapping(selectedStreamId),
    enabled: Boolean(selectedStreamId),
    retry: false,
  });

  const activitiesQuery = useQuery({
    queryKey: ["activities"],
    queryFn: getActivities,
  });

  const activityOptions = useMemo(
    () => (activitiesQuery.data ?? []).filter((item) => item.isActive),
    [activitiesQuery.data],
  );

  const createMutation = useMutation({
    mutationFn: () =>
      createMappingVersion(selectedStreamId, {
        name: draftName,
        rules: [
          {
            name: defaultDraft.ruleName,
            eventType: "CampingBookingCreated",
            conditionExpression: null,
            sortOrder: 1,
            activities: [
              {
                activityId:
                  activityOptions.find((item) => item.code.toLowerCase().includes("reception"))?.id
                  ?? activityOptions[0]?.id
                  ?? "00000000-0000-0000-0000-000000000001",
                baseHours: 0.3,
                unitExpression: "count($.addOns)",
                perUnitHours: 0.05,
                multiplierExpression: null,
              },
              {
                activityId:
                  activityOptions.find((item) => item.code.toLowerCase().includes("house"))?.id
                  ?? activityOptions[1]?.id
                  ?? activityOptions[0]?.id
                  ?? "00000000-0000-0000-0000-000000000001",
                baseHours: 0,
                unitExpression: "stayNights($.checkInDate,$.checkOutDate)",
                perUnitHours: 0.6,
                multiplierExpression: "cabinTypeFactor($.cabinType)",
              },
            ],
          },
        ],
      }),
    onSuccess: async () => {
      setStatusMessage("Ny version skapad.");
      await invalidateMappingQueries(queryClient, selectedStreamId);
    },
    onError: (error) => {
      setStatusMessage(error instanceof Error ? error.message : "Kunde inte skapa version.");
    },
  });

  const activateMutation = useMutation({
    mutationFn: (versionId: string) => activateMappingVersion(selectedStreamId, versionId),
    onSuccess: async () => {
      setStatusMessage("Version aktiverad.");
      await invalidateMappingQueries(queryClient, selectedStreamId);
    },
    onError: (error) => {
      setStatusMessage(error instanceof Error ? error.message : "Kunde inte aktivera version.");
    },
  });

  const rollbackMutation = useMutation({
    mutationFn: (versionId: string) => rollbackMappingVersion(selectedStreamId, versionId),
    onSuccess: async () => {
      setStatusMessage("Rollback skapad som ny version.");
      await invalidateMappingQueries(queryClient, selectedStreamId);
    },
    onError: (error) => {
      setStatusMessage(error instanceof Error ? error.message : "Kunde inte rollbacka version.");
    },
  });

  return (
    <div className="h-full overflow-auto bg-slate-50/40 p-6">
      <div className="mx-auto flex max-w-6xl flex-col gap-6">
        <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <div className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Mapping Editor</div>
              <h2 className="mt-2 text-2xl font-bold text-slate-900">Riktiga versioner från backend</h2>
              <p className="mt-2 max-w-2xl text-sm text-slate-500">
                Hämta aktiv mapping-version, skapa ny version och aktivera eller rollbacka tidigare versioner.
              </p>
            </div>
            <div className="flex min-w-[280px] flex-col gap-2">
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Stream</label>
              <select
                value={selectedStreamId}
                onChange={(event) => setSelectedStreamId(event.target.value)}
                className="rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 shadow-sm outline-none ring-0"
              >
                <option value="">Välj stream</option>
                {(streamsQuery.data ?? []).map((stream) => (
                  <option key={stream.id} value={stream.id}>
                    {stream.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>

        <div className="grid gap-6 lg:grid-cols-[1.05fr_1.55fr]">
          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-xs font-semibold uppercase tracking-wide text-slate-400">Aktiv Version</div>
                <h3 className="mt-1 text-lg font-semibold text-slate-900">
                  {activeQuery.data?.version?.name ?? "Ingen aktiv version"}
                </h3>
              </div>
              {activeQuery.data?.version?.isActive && (
                <span className="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">
                  Aktiv
                </span>
              )}
            </div>

            <div className="mt-4 space-y-3 text-sm text-slate-600">
              <div className="rounded-xl bg-slate-50 px-4 py-3">
                <div className="text-xs uppercase tracking-wide text-slate-400">Version</div>
                <div className="mt-1 font-medium text-slate-900">
                  {activeQuery.data?.version?.versionNumber ? `v${activeQuery.data.version.versionNumber}` : "-"}
                </div>
              </div>
              <div className="rounded-xl bg-slate-50 px-4 py-3">
                <div className="text-xs uppercase tracking-wide text-slate-400">Regler</div>
                <div className="mt-1 font-medium text-slate-900">{activeQuery.data?.rules.length ?? 0}</div>
              </div>
              <div className="rounded-xl bg-slate-50 px-4 py-3">
                <div className="text-xs uppercase tracking-wide text-slate-400">Rule Activities</div>
                <div className="mt-1 font-medium text-slate-900">{activeQuery.data?.ruleActivities.length ?? 0}</div>
              </div>
            </div>

            <div className="mt-5 rounded-2xl border border-dashed border-slate-200 p-4">
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Skapa Ny Version</label>
              <input
                value={draftName}
                onChange={(event) => setDraftName(event.target.value)}
                className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-700 outline-none"
                placeholder="Namn på ny mapping-version"
              />
              <button
                onClick={() => createMutation.mutate()}
                disabled={!selectedStreamId || createMutation.isPending}
                className="mt-3 rounded-xl bg-slate-900 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {createMutation.isPending ? "Skapar..." : "Skapa version"}
              </button>
            </div>

            <div className="mt-4 min-h-6 text-sm text-slate-500">
              {streamsQuery.isLoading && "Hämtar streams..."}
              {activeQuery.isLoading && selectedStreamId && "Hämtar aktiv version..."}
              {statusMessage}
            </div>
          </section>

          <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-xs font-semibold uppercase tracking-wide text-slate-400">Versioner</div>
                <h3 className="mt-1 text-lg font-semibold text-slate-900">Historik och aktivering</h3>
              </div>
              <div className="text-sm text-slate-500">{versionsQuery.data?.versions.length ?? 0} versioner</div>
            </div>

            <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200">
              <table className="w-full text-left text-sm">
                <thead className="bg-slate-50 text-slate-500">
                  <tr>
                    <th className="px-4 py-3 font-semibold">Version</th>
                    <th className="px-4 py-3 font-semibold">Namn</th>
                    <th className="px-4 py-3 font-semibold">Status</th>
                    <th className="px-4 py-3 font-semibold">Skapad</th>
                    <th className="px-4 py-3 font-semibold text-right">Åtgärd</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 bg-white">
                  {(versionsQuery.data?.versions ?? []).map((version) => (
                    <tr key={version.id}>
                      <td className="px-4 py-3 font-semibold text-slate-900">v{version.versionNumber}</td>
                      <td className="px-4 py-3 text-slate-700">{version.name}</td>
                      <td className="px-4 py-3">
                        {version.isActive ? (
                          <span className="rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">Aktiv</span>
                        ) : (
                          <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">Inaktiv</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-slate-500">{new Date(version.createdAtUtc).toLocaleString("sv-SE")}</td>
                      <td className="px-4 py-3">
                        <div className="flex justify-end gap-2">
                          <button
                            onClick={() => activateMutation.mutate(version.id)}
                            disabled={version.isActive || activateMutation.isPending}
                            className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 disabled:cursor-not-allowed disabled:opacity-40"
                          >
                            Aktivera
                          </button>
                          <button
                            onClick={() => rollbackMutation.mutate(version.id)}
                            disabled={rollbackMutation.isPending}
                            className="rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white disabled:cursor-not-allowed disabled:opacity-40"
                          >
                            Rollback
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                  {!versionsQuery.isLoading && !(versionsQuery.data?.versions.length) && (
                    <tr>
                      <td colSpan={5} className="px-4 py-10 text-center text-sm text-slate-500">
                        Inga mapping-versioner hittades för vald stream.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}

async function invalidateMappingQueries(queryClient: ReturnType<typeof useQueryClient>, streamId: string) {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: ["mapping-versions", streamId] }),
    queryClient.invalidateQueries({ queryKey: ["mapping-active", streamId] }),
  ]);
}

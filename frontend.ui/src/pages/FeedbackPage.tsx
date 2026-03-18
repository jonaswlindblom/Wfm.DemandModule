import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { MessageSquareMore, SlidersHorizontal, TrendingUp } from "lucide-react";
import { getFeedback } from "../api/feedback";

export default function FeedbackPage() {
  const [mappingVersionId, setMappingVersionId] = useState("");

  const feedbackQuery = useQuery({
    queryKey: ["feedback", mappingVersionId],
    queryFn: () => getFeedback(mappingVersionId || undefined),
  });

  const summary = useMemo(() => {
    const entries = feedbackQuery.data?.entries ?? [];
    const profiles = feedbackQuery.data?.profiles ?? [];
    const averageFactor = profiles.length
      ? profiles.reduce((sum, item) => sum + item.factor, 0) / profiles.length
      : 0;

    return {
      entryCount: entries.length,
      profileCount: profiles.length,
      averageFactor,
    };
  }, [feedbackQuery.data]);

  return (
    <div className="h-full overflow-auto bg-slate-50/40 p-6">
      <div className="mx-auto flex max-w-6xl flex-col gap-6">
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <div className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Feedback</div>
              <h2 className="mt-2 text-2xl font-bold text-slate-900">Kalibrering och senaste utfall</h2>
              <p className="mt-2 max-w-2xl text-sm text-slate-500">
                Visa senaste feedback från backend och de kalibreringsfaktorer som just nu påverkar workload-beräkningen.
              </p>
            </div>
            <label className="flex min-w-[280px] flex-col gap-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
              Mapping Version Id
              <input
                value={mappingVersionId}
                onChange={(event) => setMappingVersionId(event.target.value)}
                placeholder="Valfritt filter"
                className="rounded-xl border border-slate-200 px-3 py-2 text-sm font-normal normal-case text-slate-700 outline-none"
              />
            </label>
          </div>
        </section>

        <section className="grid gap-4 md:grid-cols-3">
          <KpiCard icon={MessageSquareMore} label="Feedback Entries" value={summary.entryCount.toString()} />
          <KpiCard icon={SlidersHorizontal} label="Calibration Profiles" value={summary.profileCount.toString()} />
          <KpiCard icon={TrendingUp} label="Avg Factor" value={summary.profileCount ? summary.averageFactor.toFixed(2) : "-"} />
        </section>

        {feedbackQuery.isLoading && (
          <div className="rounded-2xl border border-slate-200 bg-white px-5 py-10 text-center text-sm text-slate-500 shadow-sm">
            Hämtar feedback från backend...
          </div>
        )}

        {feedbackQuery.isError && (
          <div className="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm text-rose-700 shadow-sm">
            Kunde inte läsa feedback. Kontrollera token eller backend-svar.
          </div>
        )}

        {!feedbackQuery.isLoading && !feedbackQuery.isError && (
          <section className="grid gap-6 lg:grid-cols-[1.25fr_0.95fr]">
            <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-xs font-semibold uppercase tracking-wide text-slate-400">Recent Entries</div>
                  <h3 className="mt-1 text-lg font-semibold text-slate-900">Senaste manuella utfall</h3>
                </div>
                <div className="text-sm text-slate-500">{feedbackQuery.data?.entries.length ?? 0} rader</div>
              </div>

              <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200">
                <table className="w-full text-left text-sm">
                  <thead className="bg-slate-50 text-slate-500">
                    <tr>
                      <th className="px-4 py-3 font-semibold">Aktivitet</th>
                      <th className="px-4 py-3 font-semibold">Version</th>
                      <th className="px-4 py-3 font-semibold text-right">Actual</th>
                      <th className="px-4 py-3 font-semibold">Intervall</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 bg-white">
                    {(feedbackQuery.data?.entries ?? []).map((entry) => (
                      <tr key={entry.id}>
                        <td className="px-4 py-3">
                          <div className="font-medium text-slate-900">{entry.activityName}</div>
                          <div className="text-xs text-slate-500">{entry.comment || "Ingen kommentar"}</div>
                        </td>
                        <td className="px-4 py-3 text-slate-600">{entry.mappingVersionName}</td>
                        <td className="px-4 py-3 text-right font-medium text-slate-900">{entry.actualHours.toFixed(2)} h</td>
                        <td className="px-4 py-3 text-slate-500">{new Date(entry.intervalStartUtc).toLocaleString("sv-SE")}</td>
                      </tr>
                    ))}
                    {!feedbackQuery.data?.entries.length && (
                      <tr>
                        <td colSpan={4} className="px-4 py-8 text-center text-sm text-slate-500">
                          Ingen feedback registrerad ännu.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <div>
                <div className="text-xs font-semibold uppercase tracking-wide text-slate-400">Calibration</div>
                <h3 className="mt-1 text-lg font-semibold text-slate-900">Aktiva profiler</h3>
              </div>

              <div className="mt-4 space-y-3">
                {(feedbackQuery.data?.profiles ?? []).map((profile) => (
                  <div key={profile.id} className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="font-medium text-slate-900">{profile.activityName}</div>
                        <div className="text-xs text-slate-500">{profile.mappingVersionName}</div>
                      </div>
                      <span className="rounded-full bg-indigo-50 px-3 py-1 text-xs font-semibold text-indigo-700">
                        Faktor {profile.factor.toFixed(2)}
                      </span>
                    </div>
                    <div className="mt-3 flex items-center justify-between text-xs text-slate-500">
                      <span>Lambda {profile.lambda.toFixed(2)}</span>
                      <span>{new Date(profile.updatedAtUtc).toLocaleString("sv-SE")}</span>
                    </div>
                  </div>
                ))}
                {!feedbackQuery.data?.profiles.length && (
                  <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-8 text-center text-sm text-slate-500">
                    Inga kalibreringsprofiler hittades.
                  </div>
                )}
              </div>
            </div>
          </section>
        )}
      </div>
    </div>
  );
}

function KpiCard({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof MessageSquareMore;
  label: string;
  value: string;
}) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex items-center justify-between">
        <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">{label}</span>
        <Icon className="h-4 w-4 text-indigo-600" />
      </div>
      <div className="mt-3 text-2xl font-bold text-slate-900">{value}</div>
    </div>
  );
}

import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { BarChart3, Calendar, Clock3, LineChart, TimerReset } from "lucide-react";
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { getSimulationRun } from "../api/simulationRun";

const defaultFrom = "2026-03-16T00:00:00Z";
const defaultTo = "2026-03-17T00:00:00Z";

export default function SimulationPage() {
  const [from, setFrom] = useState(defaultFrom);
  const [to, setTo] = useState(defaultTo);
  const [intervalMinutes, setIntervalMinutes] = useState(60);

  const runQuery = useQuery({
    queryKey: ["simulation-run", from, to, intervalMinutes],
    queryFn: () => getSimulationRun(from, to, intervalMinutes),
  });

  const chartData = useMemo(() => {
    const map = new Map<string, { label: string; [key: string]: string | number }>();

    for (const series of runQuery.data?.series ?? []) {
      for (const point of series.points) {
        const label = new Date(point.intervalStartUtc).toLocaleString("sv-SE", {
          month: "2-digit",
          day: "2-digit",
          hour: "2-digit",
          minute: "2-digit",
        });
        const current = map.get(point.intervalStartUtc) ?? { label };
        current[series.activityCode] = point.hours;
        map.set(point.intervalStartUtc, current);
      }
    }

    return [...map.entries()].sort((a, b) => a[0].localeCompare(b[0])).map(([, value]) => value);
  }, [runQuery.data]);

  return (
    <div className="h-full overflow-auto bg-slate-50/40 p-6">
      <div className="mx-auto flex max-w-6xl flex-col gap-6">
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <div className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-400">Simulation Run</div>
              <h2 className="mt-2 text-2xl font-bold text-slate-900">Workload för datumintervall</h2>
              <p className="mt-2 max-w-2xl text-sm text-slate-500">
                Kör backend-simuleringen för CampingBookingCreated och visa tidsserie, totals och summary.
              </p>
            </div>
            <div className="grid gap-3 md:grid-cols-3">
              <label className="flex flex-col gap-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                Från
                <input className="rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-700" value={from} onChange={(e) => setFrom(e.target.value)} />
              </label>
              <label className="flex flex-col gap-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                Till
                <input className="rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-700" value={to} onChange={(e) => setTo(e.target.value)} />
              </label>
              <label className="flex flex-col gap-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                Intervall
                <select
                  className="rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-700"
                  value={intervalMinutes}
                  onChange={(e) => setIntervalMinutes(Number(e.target.value))}
                >
                  <option value={15}>15 min</option>
                  <option value={30}>30 min</option>
                  <option value={60}>60 min</option>
                  <option value={120}>120 min</option>
                </select>
              </label>
            </div>
          </div>
        </section>

        <section className="grid gap-4 md:grid-cols-4">
          <KpiCard icon={Clock3} label="Total Hours" value={runQuery.data?.summary.totalHours?.toFixed(2) ?? "-"} />
          <KpiCard icon={BarChart3} label="Activities" value={runQuery.data?.summary.activityCount?.toString() ?? "-"} />
          <KpiCard icon={TimerReset} label="Peak Hours" value={runQuery.data?.summary.peakIntervalHours?.toFixed(2) ?? "-"} />
          <KpiCard
            icon={Calendar}
            label="Primary Driver"
            value={runQuery.data?.summary.primaryDriver ?? "-"}
            secondary={runQuery.data?.summary.peakIntervalStartUtc ? new Date(runQuery.data.summary.peakIntervalStartUtc).toLocaleString("sv-SE") : "-"}
          />
        </section>

        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-4 flex items-center gap-2">
            <LineChart className="h-4 w-4 text-indigo-600" />
            <h3 className="text-lg font-semibold text-slate-900">Tidsserie per aktivitet</h3>
          </div>

          <div className="h-[320px]">
            {runQuery.isLoading && <div className="flex h-full items-center justify-center text-sm text-slate-500">Kör simulering...</div>}
            {runQuery.isError && <div className="flex h-full items-center justify-center text-sm text-rose-600">Kunde inte läsa simulation/run.</div>}
            {!runQuery.isLoading && !runQuery.isError && (
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
                  <XAxis dataKey="label" tick={{ fontSize: 11, fill: "#64748b" }} />
                  <YAxis tick={{ fontSize: 11, fill: "#64748b" }} />
                  <Tooltip />
                  {(runQuery.data?.series ?? []).map((series, index) => (
                    <Area
                      key={series.activityCode}
                      type="monotone"
                      dataKey={series.activityCode}
                      stroke={index === 0 ? "#4f46e5" : "#0f766e"}
                      fill={index === 0 ? "rgba(79,70,229,0.18)" : "rgba(15,118,110,0.18)"}
                      strokeWidth={2}
                    />
                  ))}
                </AreaChart>
              </ResponsiveContainer>
            )}
          </div>
        </section>

        <section className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
          <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <h3 className="text-lg font-semibold text-slate-900">Totals per aktivitet</h3>
            <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200">
              <table className="w-full text-left text-sm">
                <thead className="bg-slate-50 text-slate-500">
                  <tr>
                    <th className="px-4 py-3 font-semibold">Aktivitet</th>
                    <th className="px-4 py-3 font-semibold text-right">Total workload</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {(runQuery.data?.totals ?? []).map((item) => (
                    <tr key={item.activityCode}>
                      <td className="px-4 py-3 font-medium text-slate-900">{item.activityCode}</td>
                      <td className="px-4 py-3 text-right text-slate-700">{item.totalHours.toFixed(2)} h</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <h3 className="text-lg font-semibold text-slate-900">Sammanfattning</h3>
            <div className="mt-4 space-y-3">
              <SummaryRow label="From" value={from} />
              <SummaryRow label="To" value={to} />
              <SummaryRow label="Interval" value={`${intervalMinutes} min`} />
              <SummaryRow label="Peak Interval" value={runQuery.data?.summary.peakIntervalStartUtc ? new Date(runQuery.data.summary.peakIntervalStartUtc).toLocaleString("sv-SE") : "-"} />
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}

function KpiCard({
  icon: Icon,
  label,
  value,
  secondary,
}: {
  icon: typeof Clock3;
  label: string;
  value: string;
  secondary?: string;
}) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex items-center justify-between">
        <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">{label}</span>
        <Icon className="h-4 w-4 text-indigo-600" />
      </div>
      <div className="mt-3 text-2xl font-bold text-slate-900">{value}</div>
      {secondary && <div className="mt-1 text-xs text-slate-500">{secondary}</div>}
    </div>
  );
}

function SummaryRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between rounded-xl bg-slate-50 px-4 py-3">
      <span className="text-sm text-slate-500">{label}</span>
      <span className="text-sm font-medium text-slate-900">{value}</span>
    </div>
  );
}

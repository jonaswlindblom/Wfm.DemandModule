import { type ReactNode, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  BarChart3,
  Calendar as CalendarIcon,
  CheckCircle2,
  ChevronRight,
  Clock,
  ShieldAlert,
  Sparkles,
  TrendingUp,
  Users,
  ShoppingCart,
  FileCode,
  CloudSun,
  Zap,
  type LucideIcon,
} from "lucide-react";
import {
  Area,
  Bar,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
  BarChart as RechartsBarChart,
} from "recharts";
import { getSimulationOverview } from "../api/simulationOverview";
import type { SimulationOverviewPeriod, SimulationOverviewStream } from "../api/types";

const DASHBOARD_DATE = "2026-03-16";

type Period = "day" | "week" | "month";
type ViewMode = "curve" | "activity";
type Variance = "låg" | "medel" | "hög";
type SuggestionType = "ökning" | "minskning";

type ChartPoint = {
  label: string;
  baseline: number;
  ai_demand: number;
  actual: number | null;
  visitors: number;
  sales: number;
  variance: Variance;
};

type ActivityPoint = {
  label: string;
  foh: number;
  ops: number;
  fb: number;
  cleaning: number;
  security: number;
};

type Suggestion = {
  id: number;
  title: string;
  reason: string;
  impact: string;
  confidence: number;
  type: SuggestionType;
};

type StripKpi = {
  label: string;
  value: string;
  sub: string;
  icon: LucideIcon;
  color: "blue" | "amber" | "purple" | "emerald" | "slate";
};

type Kpis = {
  total: string;
  delta: string;
};

type StreamItem = SimulationOverviewStream & { icon: LucideIcon };

const streamIcons: Record<string, LucideIcon> = {
  visitors: Users,
  sales: ShoppingCart,
  bookings: FileCode,
  weather: CloudSun,
  campaigns: Zap,
};

const stripIcons: Record<string, LucideIcon> = {
  clock: Clock,
  "trending-up": TrendingUp,
  sparkles: Sparkles,
  "shield-alert": ShieldAlert,
  "check-circle": CheckCircle2,
};

const varianceMap: Record<string, Variance> = {
  low: "låg",
  medium: "medel",
  high: "hög",
};

const Badge = ({
  children,
  color = "blue",
}: {
  children: ReactNode;
  color?: "blue" | "green" | "purple" | "red" | "yellow" | "gray";
}) => {
  const colorClasses = {
    blue: "bg-blue-50 text-blue-700 border-blue-200",
    green: "bg-emerald-50 text-emerald-700 border-emerald-200",
    purple: "bg-purple-50 text-purple-700 border-purple-200",
    red: "bg-rose-50 text-rose-700 border-rose-200",
    yellow: "bg-amber-50 text-amber-700 border-amber-200",
    gray: "bg-slate-50 text-slate-700 border-slate-200",
  };

  return (
    <span className={`inline-flex items-center rounded-md px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider border ${colorClasses[color]}`}>
      {children}
    </span>
  );
};

const Card = ({
  children,
  title,
  action,
  className = "",
  noPadding = false,
}: {
  children: ReactNode;
  title?: ReactNode;
  action?: ReactNode;
  className?: string;
  noPadding?: boolean;
}) => (
  <div className={`bg-white rounded-lg border border-slate-200 shadow-sm flex flex-col ${className}`}>
    {(title || action) && (
      <div className="px-4 py-3 border-b border-slate-100 flex justify-between items-center rounded-t-lg shrink-0 bg-white">
        {title && <h3 className="font-semibold text-slate-700 text-xs uppercase tracking-wide flex items-center gap-2">{title}</h3>}
        {action && <div>{action}</div>}
      </div>
    )}
    <div className={`flex-1 flex flex-col min-h-0 ${noPadding ? "" : "p-4"}`}>{children}</div>
  </div>
);

function formatHours(value: number) {
  return Number.isInteger(value)
    ? value.toLocaleString("sv-SE")
    : value.toLocaleString("sv-SE", { minimumFractionDigits: 1, maximumFractionDigits: 1 });
}

function formatDelta(value: number) {
  return `${value >= 0 ? "+" : ""}${value.toLocaleString("sv-SE", { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%`;
}

function periodData(period: SimulationOverviewPeriod) {
  const chart: ChartPoint[] = period.chart.map((item) => ({
    label: item.label,
    baseline: item.baseline,
    ai_demand: item.aiDemand,
    actual: item.actual,
    visitors: item.visitors,
    sales: item.sales,
    variance: varianceMap[item.variance],
  }));

  const activity: ActivityPoint[] = period.activityMix.map((item) => ({
    label: item.label,
    foh: item.foh,
    ops: item.ops,
    fb: item.fb,
    cleaning: item.cleaning,
    security: item.security,
  }));

  const suggestions: Suggestion[] = period.suggestions.map((item) => ({
    id: item.id,
    title: item.title,
    reason: item.reason,
    impact: item.impact,
    confidence: item.confidence,
    type: item.type === "increase" ? "ökning" : "minskning",
  }));

  const strip: StripKpi[] = period.strip.map((item) => ({
    label: item.label,
    value: item.value,
    sub: item.sub,
    icon: stripIcons[item.icon],
    color: item.color,
  }));

  const kpis: Kpis = {
    total: formatHours(period.summary.totalHours),
    delta: formatDelta(period.summary.deltaPercent),
  };

  return { chart, activity, suggestions, strip, kpis, rangeLabel: period.rangeLabel, driver: period.summary.primaryDriver };
}

export default function SimulationOverviewPage() {
  const [period, setPeriod] = useState<Period>("day");
  const [viewMode, setViewMode] = useState<ViewMode>("curve");
  const [activeStreams, setActiveStreams] = useState<string[]>(["visitors"]);

  const overviewQuery = useQuery({
    queryKey: ["simulation-overview", DASHBOARD_DATE],
    queryFn: () => getSimulationOverview(DASHBOARD_DATE),
  });

  const selectedPeriod = useMemo(() => {
    const periods = overviewQuery.data?.periods;
    if (!periods) {
      return null;
    }

    switch (period) {
      case "day":
        return periods.day;
      case "week":
        return periods.week;
      case "month":
        return periods.month;
    }
  }, [overviewQuery.data, period]);

  const mapped = selectedPeriod ? periodData(selectedPeriod) : null;

  const chartData = mapped?.chart ?? [];
  const activityData = mapped?.activity ?? [];
  const currentSuggestions = mapped?.suggestions ?? [];
  const currentStripData = mapped?.strip ?? [];
  const currentKpis = mapped?.kpis ?? { total: "-", delta: "-" };
  const rangeLabel = mapped?.rangeLabel ?? DASHBOARD_DATE;
  const driver = mapped?.driver ?? "backend-data";
  const streams: StreamItem[] = (overviewQuery.data?.streams ?? []).map((stream) => ({
    ...stream,
    icon: streamIcons[stream.id] ?? FileCode,
  }));

  const toggleStream = (id: string) => {
    setActiveStreams((current) => (current.includes(id) ? current.filter((item) => item !== id) : [...current, id]));
  };

  return (
    <div className="flex flex-col h-full overflow-hidden p-6 gap-4">
      <div className="flex items-center justify-between shrink-0">
        <div>
          <div className="flex items-center gap-2 text-sm text-slate-400 mb-1">
            <span>Workforce</span>
            <ChevronRight className="w-3 h-3" />
            <span>Planering</span>
            <ChevronRight className="w-3 h-3" />
            <span className="text-slate-600 font-medium">Efterfrågan</span>
          </div>
          <h2 className="text-2xl font-bold text-slate-900">Efterfrågeöversikt</h2>
        </div>

        <div className="flex items-center gap-4 bg-white p-1 rounded-lg border border-slate-200 shadow-sm">
          <div className="flex bg-slate-100 rounded-md p-0.5">
            {["Dag", "Vecka", "Månad"].map((label) => {
              const key = label === "Dag" ? "day" : label === "Vecka" ? "week" : "month";
              const isActive = period === key;
              return (
                <button
                  key={key}
                  onClick={() => setPeriod(key as Period)}
                  className={`px-4 py-1.5 text-xs font-semibold rounded-md transition-all ${isActive ? "bg-white text-indigo-600 shadow-sm" : "text-slate-500 hover:text-slate-700"}`}
                >
                  {label}
                </button>
              );
            })}
          </div>
          <div className="h-6 w-px bg-slate-200" />
          <button className="flex items-center gap-2 px-3 text-sm font-medium text-slate-700 hover:bg-slate-50 rounded py-1.5">
            <CalendarIcon className="w-4 h-4 text-slate-500" />
            <span>{rangeLabel}</span>
          </button>
        </div>
      </div>

      <div className="flex-[0_0_42%] min-h-[300px]">
        <Card className="h-full" noPadding>
          <div className="flex flex-row h-full">
            <div className="w-1/4 min-w-[240px] border-r border-slate-100 p-6 flex flex-col justify-center bg-slate-50/30">
              <div className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">Totala Timmar</div>
              <div className="flex items-baseline gap-2 mb-1">
                <span className="text-5xl font-bold text-slate-900 tracking-tight transition-all duration-300">{currentKpis.total}</span>
                <span className="text-sm font-medium text-slate-400">h</span>
              </div>
              <div className="flex items-center gap-2 mb-6">
                <Badge color="purple">AI-Justerad</Badge>
                <span className="text-xs font-bold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded">{currentKpis.delta}</span>
              </div>
              <div className="text-xs text-slate-400 leading-relaxed">
                {overviewQuery.isLoading && <>Hämtar översikt från backend...</>}
                {overviewQuery.isError && <>Kunde inte hämta backend-data. Layouten visas med tomt overview-state.</>}
                {overviewQuery.isSuccess && <>Prognosen inkluderar <strong>{currentSuggestions.length}</strong> aktiva AI-justeringar baserade på {driver}.</>}
              </div>
            </div>

            <div className="flex-1 flex flex-col min-w-0">
              <div className="px-6 py-4 border-b border-slate-50 flex justify-between items-center">
                <div className="flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full bg-indigo-500"></span>
                  <h3 className="font-semibold text-slate-700 text-sm">Prognosvisualisering</h3>
                </div>
                <div className="flex bg-slate-100 p-0.5 rounded-lg">
                  <button
                    onClick={() => setViewMode("curve")}
                    className={`flex items-center gap-2 px-3 py-1 text-xs font-medium rounded-md transition-all ${viewMode === "curve" ? "bg-white text-indigo-700 shadow-sm" : "text-slate-500 hover:text-slate-700"}`}
                  >
                    <TrendingUp className="w-3 h-3" /> Kurva
                  </button>
                  <button
                    onClick={() => setViewMode("activity")}
                    className={`flex items-center gap-2 px-3 py-1 text-xs font-medium rounded-md transition-all ${viewMode === "activity" ? "bg-white text-indigo-700 shadow-sm" : "text-slate-500 hover:text-slate-700"}`}
                  >
                    <BarChart3 className="w-3 h-3" /> Aktivitet
                  </button>
                </div>
              </div>

              <div className="flex-1 p-4 relative min-h-0">
                {overviewQuery.isLoading && (
                  <div className="absolute inset-4 rounded-lg border border-dashed border-slate-200 bg-slate-50/70 flex items-center justify-center text-sm text-slate-500">
                    Laddar översikt...
                  </div>
                )}
                {overviewQuery.isError && (
                  <div className="absolute left-4 right-4 top-4 z-10 rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800">
                    Backend overview misslyckades. Kontrollera token eller API-base.
                  </div>
                )}
                <ResponsiveContainer width="100%" height="100%">
                  {viewMode === "curve" ? (
                    <ComposedChart data={chartData} margin={{ top: 10, right: 30, left: 10, bottom: 0 }}>
                      <defs>
                        <linearGradient id="overview-ai-fill" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="5%" stopColor="#8b5cf6" stopOpacity={0.1} />
                          <stop offset="95%" stopColor="#8b5cf6" stopOpacity={0} />
                        </linearGradient>
                      </defs>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                      <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fontSize: 11, fill: "#64748b" }} dy={10} />
                      <YAxis yAxisId="left" axisLine={false} tickLine={false} tick={{ fontSize: 11, fill: "#64748b" }} />
                      <YAxis yAxisId="right" orientation="right" axisLine={false} tickLine={false} hide={!activeStreams.length} />
                      <Tooltip contentStyle={{ borderRadius: "8px", border: "none", boxShadow: "0 4px 6px -1px rgb(0 0 0 / 0.1)" }} />
                      <Legend verticalAlign="top" height={36} iconType="circle" />
                      <Area yAxisId="left" type="monotone" dataKey="baseline" name="Regelbas" stroke="#cbd5e1" fill="transparent" strokeDasharray="5 5" strokeWidth={2} />
                      <Area yAxisId="left" type="monotone" dataKey="ai_demand" name="AI-Prognos" stroke="#8b5cf6" fill="url(#overview-ai-fill)" strokeWidth={3} />
                      {activeStreams.includes("visitors") && <Line yAxisId="right" type="monotone" dataKey="visitors" name="Besökare" stroke="#10b981" strokeWidth={2} dot={false} opacity={0.6} />}
                      {activeStreams.includes("sales") && <Line yAxisId="right" type="monotone" dataKey="sales" name="Försäljning" stroke="#f59e0b" strokeWidth={2} dot={false} opacity={0.6} />}
                    </ComposedChart>
                  ) : (
                    <RechartsBarChart data={activityData} margin={{ top: 10, right: 30, left: 10, bottom: 0 }}>
                      <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                      <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fontSize: 11, fill: "#64748b" }} />
                      <YAxis axisLine={false} tickLine={false} tick={{ fontSize: 11, fill: "#64748b" }} />
                      <Tooltip cursor={{ fill: "#f8fafc" }} contentStyle={{ borderRadius: "8px" }} />
                      <Legend iconType="circle" />
                      <Bar dataKey="foh" name="Front" stackId="a" fill="#6366f1" radius={[0, 0, 2, 2]} />
                      <Bar dataKey="ops" name="Drift" stackId="a" fill="#3b82f6" />
                      <Bar dataKey="fb" name="F&B" stackId="a" fill="#8b5cf6" />
                      <Bar dataKey="cleaning" name="Städ" stackId="a" fill="#06b6d4" radius={[2, 2, 0, 0]} />
                    </RechartsBarChart>
                  )}
                </ResponsiveContainer>
              </div>
            </div>
          </div>
        </Card>
      </div>

      <div className="shrink-0 grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
        {currentStripData.map((kpi) => {
          const Icon = kpi.icon;
          const colorClass =
            kpi.color === "blue" ? "text-blue-600 bg-blue-50" :
            kpi.color === "amber" ? "text-amber-600 bg-amber-50" :
            kpi.color === "purple" ? "text-purple-600 bg-purple-50" :
            kpi.color === "emerald" ? "text-emerald-600 bg-emerald-50" :
            "text-slate-600 bg-slate-100";

          return (
            <div key={kpi.label} className="bg-white border border-slate-200 rounded-lg p-3 flex items-center justify-between shadow-sm">
              <div>
                <div className="text-[10px] font-bold text-slate-400 uppercase tracking-wide mb-0.5">{kpi.label}</div>
                <div className="flex items-baseline gap-1.5">
                  <span className="text-lg font-bold text-slate-900">{kpi.value}</span>
                  <span className="text-xs font-medium text-slate-500">{kpi.sub}</span>
                </div>
              </div>
              <div className={`p-2 rounded-lg ${colorClass}`}>
                <Icon className="w-4 h-4" />
              </div>
            </div>
          );
        })}
      </div>

      <div className="flex-1 min-h-0 flex gap-4">
        <Card className="flex-[2] h-full" title="AI-Förslag" action={<Badge color="purple">{currentSuggestions.length} Aktiva</Badge>} noPadding>
          <div className="flex-1 overflow-y-auto p-3 space-y-2 custom-scrollbar">
            {currentSuggestions.map((suggestion) => (
              <div key={suggestion.id} className="p-3 bg-white border border-slate-100 rounded-lg shadow-sm hover:border-indigo-200 hover:shadow-md transition-all">
                <div className="flex justify-between items-start mb-1">
                  <div className="flex items-center gap-2">
                    <Badge color={suggestion.type === "ökning" ? "red" : "green"}>{suggestion.type === "ökning" ? "Ökning" : "Minskning"}</Badge>
                    <span className="text-xs font-semibold text-slate-800">{suggestion.title}</span>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs font-mono text-slate-500">
                    <Clock className="w-3 h-3 text-slate-400" />
                    {suggestion.impact}
                  </div>
                </div>
                <div className="text-xs text-slate-500 leading-relaxed">{suggestion.reason}</div>
              </div>
            ))}
          </div>
        </Card>

        <Card className="flex-1 h-full" title="Dataströmmar" noPadding>
          <div className="flex-1 overflow-y-auto p-2 space-y-2 custom-scrollbar">
            {streams.map((stream) => {
              const isActive = activeStreams.includes(stream.id);
              const Icon = stream.icon;

              return (
                <button
                  key={stream.id}
                  onClick={() => toggleStream(stream.id)}
                  className={`w-full text-left p-3 rounded-lg border transition-all flex items-center justify-between ${isActive ? "bg-indigo-50 border-indigo-200 shadow-sm" : "bg-white border-transparent hover:bg-slate-50"}`}
                >
                  <div className="flex items-center gap-3">
                    <div className={`p-1.5 rounded-md ${isActive ? "bg-indigo-100 text-indigo-600" : "bg-slate-100 text-slate-400"}`}>
                      <Icon className="w-4 h-4" />
                    </div>
                    <div>
                      <div className={`text-xs font-bold uppercase mb-0.5 ${isActive ? "text-indigo-900" : "text-slate-500"}`}>{stream.name}</div>
                      <div className="flex items-center gap-2">
                        <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                        <span className="text-[10px] text-slate-400">{stream.type}</span>
                        <span className="text-[10px] text-slate-400">{stream.value}</span>
                      </div>
                    </div>
                  </div>
                  <div className={`w-4 h-4 rounded border flex items-center justify-center ${isActive ? "bg-indigo-500 border-indigo-500" : "bg-white border-slate-300"}`}>
                    {isActive && <CheckCircle2 className="w-3 h-3 text-white" />}
                  </div>
                </button>
              );
            })}
          </div>
        </Card>
      </div>
    </div>
  );
}

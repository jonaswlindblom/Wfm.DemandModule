import React, { useState, useMemo } from 'react';
import SimulationOverviewPage from './pages/SimulationOverviewPage';
import MappingEditorPage from './pages/MappingEditorPage';
import SimulationPage from './pages/SimulationPage';
import FeedbackPage from './pages/FeedbackPage';
import {
    // Navigering & Struktur
    LayoutDashboard,
    FileCode,
    Activity,
    LineChart,
    Settings,
    ChevronRight,
    ChevronDown,
    Menu,
    X,
    Maximize2,

    // Indikatorer & Ãƒâ€¦tgÃƒÂ¤rder
    CheckCircle2,
    AlertTriangle,
    XCircle,
    Zap,
    Sparkles,
    Save,
    Play,
    History,
    ThumbsUp,
    ThumbsDown,
    Eye,
    EyeOff,
    Filter,
    ArrowRightLeft,
    Calendar as CalendarIcon,
    TrendingUp,
    Clock,
    ShieldAlert,
    BarChart3,
    Users,
    ShoppingCart,
    CloudSun,
    Layers
} from 'lucide-react';
import {
    ComposedChart,
    LineChart as RechartsLineChart,
    Line,
    Area,
    BarChart as RechartsBarChart,
    Bar,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    Legend,
    ResponsiveContainer,
    ReferenceDot
} from 'recharts';

// --- 1. MOCK DATA & KONFIGURATION (SVENSKA) ---

const SAVED_RULES = [
    { id: 'r1', name: 'Regnboost F&B', prompt: 'Om regn > 5mm och besÃƒÂ¶kare > 200, ÃƒÂ¶ka F&B-personal med 1.', status: 'aktiv', lastEdited: '2h sedan' },
    { id: 'r2', name: 'Lunchrusning', prompt: 'Starta fÃƒÂ¶rberedelseskiftet 1 timme tidigare om bokningar > 50.', status: 'aktiv', lastEdited: '1d sedan' },
    { id: 'r3', name: 'SÃƒÂ¤kerhetsnivÃƒÂ¥', prompt: 'Om kÃƒÂ¶tid > 15m, ÃƒÂ¶ppna extra sÃƒÂ¤kerhetslinje.', status: 'utkast', lastEdited: '3d sedan' },
];

const DATA_STREAMS = [
    { id: 'visitors', name: 'BesÃƒÂ¶ksrÃƒÂ¤knare', type: 'IoT Sensor', status: 'live', latency: '45ms', value: '432/h', health: 98, icon: Users },
    { id: 'sales', name: 'Kassatransaktioner', type: 'API', status: 'live', latency: '120ms', value: '24 kkr/h', health: 100, icon: ShoppingCart },
    { id: 'queue', name: 'KÃƒÂ¶tider', type: 'Kamera AI', status: 'fÃƒÂ¶rdrÃƒÂ¶jd', latency: '5m', value: '12m snitt', health: 85, icon: Clock },
    { id: 'weather', name: 'Lokalt VÃƒÂ¤der', type: 'Extern API', status: 'live', latency: '1s', value: 'Regn 2mm', health: 100, icon: CloudSun },
    { id: 'bookings', name: 'Bokningar', type: 'Intern DB', status: 'live', latency: '200ms', value: '45 aktiva', health: 100, icon: FileCode },
    { id: 'marketing', name: 'Kampanjer', type: 'CMS', status: 'live', latency: '10s', value: '2 Aktiva', health: 100, icon: Zap },
    { id: 'staff', name: 'StÃƒÂ¤mplingar', type: 'WFM', status: 'live', latency: '30s', value: '18 Inne', health: 99, icon: Users },
    { id: 'events', name: 'Lokala Event', type: 'Ext. Feed', status: 'saknas', latency: '-', value: '-', health: 0, icon: CalendarIcon },
];

// -- TIDSPERIODSDATA --

// DAG-LÃƒâ€žGE (Timmar)
const HOURLY_DATA = [
    { label: '08:00', baseline: 12, ai_demand: 12, actual: 11, visitors: 120, sales: 12, variance: 'lÃƒÂ¥g' },
    { label: '09:00', baseline: 15, ai_demand: 16, actual: 16, visitors: 200, sales: 18, variance: 'lÃƒÂ¥g' },
    { label: '10:00', baseline: 22, ai_demand: 24, actual: 23, visitors: 350, sales: 25, variance: 'medel' },
    { label: '11:00', baseline: 30, ai_demand: 35, actual: 36, visitors: 580, sales: 40, variance: 'hÃƒÂ¶g' },
    { label: '12:00', baseline: 45, ai_demand: 48, actual: null, visitors: 800, sales: 55, variance: 'medel' },
    { label: '13:00', baseline: 42, ai_demand: 46, actual: null, visitors: 750, sales: 50, variance: 'medel' },
    { label: '14:00', baseline: 30, ai_demand: 32, actual: null, visitors: 400, sales: 30, variance: 'lÃƒÂ¥g' },
    { label: '15:00', baseline: 25, ai_demand: 24, actual: null, visitors: 300, sales: 22, variance: 'lÃƒÂ¥g' },
    { label: '16:00', baseline: 25, ai_demand: 28, actual: null, visitors: 380, sales: 28, variance: 'medel' },
    { label: '17:00', baseline: 35, ai_demand: 40, actual: null, visitors: 600, sales: 45, variance: 'hÃƒÂ¶g' },
    { label: '18:00', baseline: 40, ai_demand: 42, actual: null, visitors: 650, sales: 48, variance: 'lÃƒÂ¥g' },
    { label: '19:00', baseline: 30, ai_demand: 30, actual: null, visitors: 400, sales: 35, variance: 'lÃƒÂ¥g' },
] as const;

// VECKO-LÃƒâ€žGE (Dagar)
const DAILY_DATA = [
    { label: 'MÃƒÂ¥n', baseline: 320, ai_demand: 330, actual: 325, visitors: 4200, sales: 320, variance: 'lÃƒÂ¥g' },
    { label: 'Tis', baseline: 310, ai_demand: 315, actual: 318, visitors: 4100, sales: 310, variance: 'lÃƒÂ¥g' },
    { label: 'Ons', baseline: 340, ai_demand: 360, actual: null, visitors: 4800, sales: 350, variance: 'medel' },
    { label: 'Tors', baseline: 380, ai_demand: 410, actual: null, visitors: 5200, sales: 400, variance: 'hÃƒÂ¶g' },
    { label: 'Fre', baseline: 450, ai_demand: 480, actual: null, visitors: 6500, sales: 550, variance: 'hÃƒÂ¶g' },
    { label: 'LÃƒÂ¶r', baseline: 520, ai_demand: 510, actual: null, visitors: 7200, sales: 600, variance: 'lÃƒÂ¥g' },
    { label: 'SÃƒÂ¶n', baseline: 400, ai_demand: 395, actual: null, visitors: 5500, sales: 420, variance: 'lÃƒÂ¥g' },
] as const;

// MÃƒâ€¦NADS-LÃƒâ€žGE (Veckor)
const WEEKLY_DATA = [
    { label: 'V.41', baseline: 2400, ai_demand: 2450, actual: 2440, visitors: 32000, sales: 2400, variance: 'lÃƒÂ¥g' },
    { label: 'V.42', baseline: 2550, ai_demand: 2600, actual: 2580, visitors: 34000, sales: 2550, variance: 'lÃƒÂ¥g' },
    { label: 'V.43', baseline: 2300, ai_demand: 2450, actual: null, visitors: 31000, sales: 2300, variance: 'hÃƒÂ¶g' },
    { label: 'V.44', baseline: 2800, ai_demand: 2900, actual: null, visitors: 38000, sales: 2900, variance: 'medel' },
] as const;

type Variance = "lÃƒÂ¥g" | "medel" | "hÃƒÂ¶g";

type ChartPoint = {
    label: string;
    baseline: number;
    ai_demand: number;
    actual: number | null;
    visitors: number;
    sales: number;
    variance: Variance;
};

const generateActivityData = (dataset: ReadonlyArray<ChartPoint>) =>
    dataset.map((d) => ({
        label: d.label,
        foh: Math.floor(d.ai_demand * 0.4),
        ops: Math.floor(d.ai_demand * 0.2),
        fb: Math.floor(d.ai_demand * 0.25),
        cleaning: Math.floor(d.ai_demand * 0.1),
        security: Math.floor(d.ai_demand * 0.05),
    }));

type Period = "day" | "week" | "month";

type Suggestion = {
    id: number;
    title: string;
    reason: string;
    impact: string;
    confidence: number;
    type: "ÃƒÂ¶kning" | "minskning";
};

type KPIs = { total: string; delta: string; peak: string; peakVal: string };

type StripKpi = {
    label: string;
    value: string;
    sub: string;
    icon: LucideIcon;
    color: "blue" | "amber" | "purple" | "emerald" | "slate";
};

// FÃƒâ€“RSLAGS-DATA
const SUGGESTIONS_MAP: Record<Period, Suggestion[]> = {
    day: [
        { id: 1, title: 'Ãƒâ€“ka F&B 12:00Ã¢â‚¬â€œ14:00', reason: 'PublikÃƒÂ¶kning pga regn.', impact: '+4.5h', confidence: 94, type: 'ÃƒÂ¶kning' },
        { id: 2, title: 'Minska StÃƒÂ¤d KvÃƒÂ¤ll', reason: 'LÃƒÂ¥gt besÃƒÂ¶ksantal.', impact: '-2.0h', confidence: 88, type: 'minskning' },
        { id: 3, title: 'Ãƒâ€“ppna SÃƒÂ¤kerhetslinje', reason: 'KÃƒÂ¶tider > 10m.', impact: '+3.0h', confidence: 76, type: 'ÃƒÂ¶kning' },
    ],
    week: [
        { id: 4, title: 'Helgbemanning LÃƒÂ¶rdag', reason: 'Lokalt event ÃƒÂ¶kar trycket.', impact: '+24h', confidence: 91, type: 'ÃƒÂ¶kning' },
        { id: 5, title: 'Minska Tisdag Morgon', reason: 'LÃƒÂ¥g historisk trend.', impact: '-8.0h', confidence: 85, type: 'minskning' },
    ],
    month: [
        { id: 6, title: 'HÃƒÂ¶stlovsjustering V.44', reason: 'Skollov ÃƒÂ¶kar besÃƒÂ¶kare.', impact: '+120h', confidence: 89, type: 'ÃƒÂ¶kning' },
        { id: 7, title: 'Optimera SchemalÃƒÂ¤ggning', reason: 'Effektiviseringspotential V.42.', impact: '-40h', confidence: 82, type: 'minskning' },
    ]
};

// KPI-DATA
const KPI_MAP: Record<Period, KPIs> = { 
    day: { total: '342.5', delta: '+8.4%', peak: '17:00', peakVal: '42h' },
    week: { total: '2,800', delta: '+4.2%', peak: 'Fredag', peakVal: '480h' },
    month: { total: '10,400', delta: '+2.1%', peak: 'V.44', peakVal: '2,900h' },
};

// KPI-STRIP-DATA
const KPI_STRIP_BY_PERIOD: Record<Period, StripKpi[]> = {
    day: [
        { label: 'Bemanningstopp', value: '17:00', sub: '42h', icon: Clock, color: 'blue' },
        { label: 'HÃƒÂ¶gst Varians', value: '11:00', sub: '+5h', icon: TrendingUp, color: 'amber' },
        { label: 'AI Konfidens', value: '94%', sub: 'HÃƒÂ¶g', icon: Sparkles, color: 'purple' },
        { label: 'Risk', value: 'LÃƒÂ¥g', sub: '<2%', icon: ShieldAlert, color: 'emerald' },
        { label: 'Buffert', value: '+5%', sub: 'Optimal', icon: CheckCircle2, color: 'slate' },
    ],
    week: [
        { label: 'Bemanningstopp', value: 'Fredag', sub: '480h', icon: Clock, color: 'blue' },
        { label: 'HÃƒÂ¶gst Varians', value: 'Fredag', sub: '+12%', icon: TrendingUp, color: 'amber' },
        { label: 'AI Konfidens', value: '89%', sub: 'Medel', icon: Sparkles, color: 'purple' },
        { label: 'Risk', value: 'Medel', sub: 'Fre em', icon: ShieldAlert, color: 'amber' },
        { label: 'Buffert', value: '+2%', sub: 'Tight', icon: CheckCircle2, color: 'slate' },
    ],
    month: [
        { label: 'Bemanningstopp', value: 'V.44', sub: '2,900h', icon: Clock, color: 'blue' },
        { label: 'HÃƒÂ¶gst Varians', value: 'V.43', sub: '-5%', icon: TrendingUp, color: 'amber' },
        { label: 'AI Konfidens', value: '92%', sub: 'HÃƒÂ¶g', icon: Sparkles, color: 'purple' },
        { label: 'Risk', value: 'LÃƒÂ¥g', sub: 'Stabil', icon: ShieldAlert, color: 'emerald' },
        { label: 'Buffert', value: '+8%', sub: 'God', icon: CheckCircle2, color: 'slate' },
    ]
};

const ACCURACY_HISTORY = [
    { date: '18 Okt', accuracy: 92, actual: 320, predicted: 315 },
    { date: '19 Okt', accuracy: 94, actual: 340, predicted: 338 },
    { date: '20 Okt', accuracy: 89, actual: 410, predicted: 380 },
    { date: '21 Okt', accuracy: 96, actual: 290, predicted: 295 },
    { date: '22 Okt', accuracy: 95, actual: 305, predicted: 302 },
    { date: '23 Okt', accuracy: 91, actual: 360, predicted: 390 },
    { date: '24 Okt', accuracy: 94, actual: 342, predicted: 342 },
];

// --- 2. KOMPONENTER ---


import type { LucideIcon } from "lucide-react";

type BadgeColor = "blue" | "green" | "purple" | "red" | "yellow" | "gray";

const Badge: React.FC<{
    children: React.ReactNode;
    color?: BadgeColor;
    className?: string;
}> = ({ children, color = "blue", className = "" }) => {
    const colorClasses: Record<BadgeColor, string> = {
        blue: "bg-blue-50 text-blue-700 border-blue-200",
        green: "bg-emerald-50 text-emerald-700 border-emerald-200",
        purple: "bg-purple-50 text-purple-700 border-purple-200",
        red: "bg-rose-50 text-rose-700 border-rose-200",
        yellow: "bg-amber-50 text-amber-700 border-amber-200",
        gray: "bg-slate-50 text-slate-700 border-slate-200",
    };

    return (
        <span
            className={`inline-flex items-center rounded-md px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider border ${colorClasses[color]} ${className}`}
        >
            {children}
        </span>
    );
};

const Card: React.FC<{
    children: React.ReactNode;
    className?: string;
    title?: React.ReactNode;
    action?: React.ReactNode; // <-- viktig: gÃƒÂ¶r optional
    noPadding?: boolean;
    headerClassName?: string;
}> = ({
    children,
    className = "",
    title,
    action,
    noPadding = false,
    headerClassName = "",
}) => (
        <div className={`bg-white rounded-lg border border-slate-200 shadow-sm flex flex-col ${className}`}>
            {(title || action) && (
                <div className={`px-4 py-3 border-b border-slate-100 flex justify-between items-center rounded-t-lg shrink-0 bg-white ${headerClassName}`}>
                    {title && (
                        <h3 className="font-semibold text-slate-700 text-xs uppercase tracking-wide flex items-center gap-2">
                            {title}
                        </h3>
                    )}
                    {action && <div>{action}</div>}
                </div>
            )}
            <div className={`flex-1 flex flex-col min-h-0 ${noPadding ? "" : "p-4"}`}>{children}</div>
        </div>
    );

// --- VY 1: AI REGELBYGGARE ---

const ScreenRuleBuilder = () => {
    const [prompt, setPrompt] = useState("Om regn > 5mm och besÃƒÂ¶kare > 200, ÃƒÂ¶ka F&B-personal med 1.");

    type GeneratedRule = {
        confidence: number;
        event_source: string;
        conditions: { field: string; op: string; val: string }[];
        actions: { target: string; type: string; val: number };
        time_logic: string;
    };

    const [generatedRule, setGeneratedRule] = useState<GeneratedRule | null>(null);

    const [isGenerating, setIsGenerating] = useState(false);

    const handleGenerate = () => {
        setIsGenerating(true);
        setTimeout(() => {
            setGeneratedRule({
                confidence: 0.98,
                event_source: "weather_api & visitor_counter",
                conditions: [
                    { field: "weather.precip", op: ">", val: "5mm" },
                    { field: "visitors.count", op: ">", val: "200" }
                ],
                actions: { target: "dept.fb", type: "increase_headcount", val: 1 },
                time_logic: "immediate_interval"
            });
            setIsGenerating(false);
        }, 1000);
    };

    return (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 h-full p-6">
            <div className="col-span-1 lg:col-span-5 flex flex-col gap-4 h-full">
                <Card title="Definiera Regel (Naturligt SprÃƒÂ¥k)" className="flex-1">
                    <div className="flex flex-col h-full">
                        <p className="text-sm text-slate-500 mb-4">
                            Beskriv din logik pÃƒÂ¥ svenska. AI:n ÃƒÂ¶versÃƒÂ¤tter det till systemregler.
                        </p>
                        <textarea
                            className="w-full h-40 p-4 bg-slate-50 border border-slate-200 rounded-lg focus:ring-2 focus:ring-indigo-500 text-sm leading-relaxed resize-none mb-4 font-mono"
                            value={prompt}
                            onChange={(e) => setPrompt(e.target.value)}
                            placeholder="T.ex. Om fÃƒÂ¶rsÃƒÂ¤ljningen ÃƒÂ¶verstiger 50 000 kr..."
                        />
                        <div className="flex justify-end mb-6">
                            <button
                                onClick={handleGenerate}
                                disabled={isGenerating}
                                className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors disabled:opacity-50"
                            >
                                {isGenerating ? <Sparkles className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
                                Generera Regel
                            </button>
                        </div>
                        <div className="mt-auto border-t border-slate-100 pt-4">
                            <h4 className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3">Tidigare Prompter</h4>
                            <div className="space-y-2 max-h-[200px] overflow-y-auto custom-scrollbar">
                                {SAVED_RULES.map(r => (
                                    <div key={r.id} className="p-2.5 rounded bg-slate-50 border border-slate-100 text-xs text-slate-600 hover:bg-indigo-50 hover:border-indigo-100 cursor-pointer transition-colors truncate">
                                        <span className="font-semibold text-slate-800">{r.name}:</span> {r.prompt}
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>
                </Card>
            </div>

            <div className="col-span-1 lg:col-span-7 h-full">
                <Card title="Strukturerad FÃƒÂ¶rhandsgranskning" className="h-full bg-slate-50/50" action={
                    generatedRule && <Badge color="green">Konfidens: 98%</Badge>
                }>
                    {!generatedRule ? (
                        <div className="h-full flex flex-col items-center justify-center text-slate-400">
                            <ArrowRightLeft className="w-10 h-10 mb-2 opacity-50" />
                            <p className="text-sm">VÃƒÂ¤ntar pÃƒÂ¥ indata...</p>
                        </div>
                    ) : (
                        <div className="space-y-6 animate-in fade-in slide-in-from-bottom-2">
                            <div className="grid grid-cols-2 gap-4">
                                <div className="bg-white p-3 rounded border border-slate-200">
                                    <div className="text-[10px] text-slate-400 uppercase font-bold mb-1">DatakÃƒÂ¤llor</div>
                                    <div className="text-sm font-mono text-indigo-700">{generatedRule.event_source}</div>
                                </div>
                                <div className="bg-white p-3 rounded border border-slate-200">
                                    <div className="text-[10px] text-slate-400 uppercase font-bold mb-1">Tidslogik</div>
                                    <div className="text-sm font-mono text-slate-700">{generatedRule.time_logic}</div>
                                </div>
                            </div>
                            <div>
                                <div className="text-[10px] text-slate-400 uppercase font-bold mb-2">Villkor</div>
                                <div className="bg-white rounded border border-slate-200 divide-y divide-slate-100">
                                    {generatedRule.conditions.map((c, i) => (
                                        <div key={i} className="p-3 flex items-center gap-3">
                                            <span className="font-mono text-xs bg-slate-100 px-1.5 py-0.5 rounded text-slate-600">{c.field}</span>
                                            <span className="text-xs font-bold text-slate-400">{c.op}</span>
                                            <span className="text-sm font-medium text-slate-900">{c.val}</span>
                                        </div>
                                    ))}
                                </div>
                            </div>
                            <div>
                                <div className="text-[10px] text-slate-400 uppercase font-bold mb-2">PÃƒÂ¥verkan</div>
                                <div className="bg-indigo-50 border border-indigo-100 rounded p-3 flex items-center gap-2">
                                    <Zap className="w-4 h-4 text-indigo-600" />
                                    <span className="text-sm text-indigo-900">
                                        {generatedRule.actions.type}
                                        <strong className="ml-1">+{generatedRule.actions.val} ({generatedRule.actions.target})</strong>
                                    </span>
                                </div>
                            </div>
                            <div className="flex gap-3 pt-4 mt-8 border-t border-slate-200">
                                <button className="flex-1 bg-slate-900 text-white py-2 rounded text-sm font-medium hover:bg-slate-800">Spara Regel</button>
                                <button className="px-4 py-2 border border-slate-200 text-slate-600 rounded text-sm font-medium hover:bg-slate-50">Testa</button>
                            </div>
                        </div>
                    )}
                </Card>
            </div>
        </div>
    );
};

// --- VY 2: DATASTRÃƒâ€“MSÃƒâ€“VERVAKNING ---

const ScreenMonitor = () => {
    const [showRaw, setShowRaw] = useState(false);

    return (
        <div className="flex flex-col h-full gap-6 p-6 overflow-hidden">
            <div className="grid grid-cols-5 gap-4 shrink-0">
                {DATA_STREAMS.slice(0, 5).map((s) => (
                    <div key={s.id} className="bg-white p-4 rounded-lg border border-slate-200 shadow-sm relative overflow-hidden">
                        <div className={`absolute left-0 top-0 bottom-0 w-1 ${s.status === 'live' ? 'bg-emerald-500' : 'bg-amber-500'}`} />
                        <div className="flex justify-between items-start mb-2 pl-2">
                            <span className="text-[10px] font-bold text-slate-500 uppercase">{s.name}</span>
                            <Activity className="w-3 h-3 text-slate-400" />
                        </div>
                        <div className="pl-2">
                            <div className="text-xl font-bold text-slate-900">{s.value}</div>
                            <div className="text-[10px] text-slate-400 mt-1">{s.type} Ã¢â‚¬Â¢ {s.latency}</div>
                        </div>
                    </div>
                ))}
            </div>

            <div className="flex-1 min-h-0 flex gap-6">
                <Card title="HÃƒÂ¤ndelsetidslinje" className="flex-1" noPadding action={
                    <div className="flex items-center gap-2">
                        <span className="text-xs text-slate-500">Visa rÃƒÂ¥data</span>

                        <button
                            onClick={() => setShowRaw(!showRaw)}
                            className={`w-8 h-4 rounded-full transition-colors relative ${showRaw ? "bg-indigo-600" : "bg-slate-200"
                                }`}
                        >
                            <div
                                className={`absolute top-0.5 left-0.5 w-3 h-3 bg-white rounded-full transition-transform ${showRaw ? "translate-x-4" : "translate-x-0"
                                    }`}
                            />
                        </button>

                    </div>
                }>
                    <div className="flex h-full">
                        <div className="flex-1 overflow-auto p-0 custom-scrollbar">
                            <table className="w-full text-left text-sm">
                                <thead className="bg-slate-50 border-b border-slate-200 sticky top-0">
                                    <tr>
                                        <th className="px-4 py-2 font-semibold text-slate-600 text-xs w-24">Tid</th>
                                        <th className="px-4 py-2 font-semibold text-slate-600 text-xs w-32">KÃƒÂ¤lla</th>
                                        <th className="px-4 py-2 font-semibold text-slate-600 text-xs">HÃƒÂ¤ndelse</th>
                                        <th className="px-4 py-2 font-semibold text-slate-600 text-xs text-right">Status</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-50">
                                    {Array.from({ length: 10 }).map((_, i) => (
                                        <tr key={i} className="hover:bg-slate-50">
                                            <td className="px-4 py-2 font-mono text-xs text-slate-500">14:32:{10 + i}</td>
                                            <td className="px-4 py-2 text-xs font-medium text-slate-700">POS_GATEWAY</td>
                                            <td className="px-4 py-2 text-xs text-slate-600">TRANSACTION_COMPLETE</td>
                                            <td className="px-4 py-2 text-right">
                                                <span className="inline-block w-2 h-2 rounded-full bg-emerald-500"></span>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            
                        </table>
                        </div>
                    </div>
                    {showRaw && (
                        <div className="w-80 border-l border-slate-200 bg-slate-900 text-slate-300 p-4 font-mono text-xs overflow-auto custom-scrollbar">
                            <div className="opacity-50 mb-2">// Raw Event Stream</div>
                            {`{ "id": "evt_293", "ts": 1698234220, "type": "txn" }`}
                        </div>
                    )}
            
        </Card>
      </div >
    </div >
  );
};

// --- VY 3: EFTERFRÃƒâ€¦GEÃƒâ€“VERSIKT (HUVUDVY) ---

const ScreenOverview = () => {
    type Period = "day" | "week" | "month";
    const [period, setPeriod] = useState<Period>("day"); // day, week, month
    type ViewMode = "curve" | "activity";
    const [viewMode, setViewMode] = useState<ViewMode>('curve'); // curve, activity
    const [activeStreams, setActiveStreams] = useState(['visitors']);

    // HÃƒÂ¤rled tillstÃƒÂ¥nd baserat pÃƒÂ¥ period
    const chartData = useMemo<ReadonlyArray<ChartPoint>>(() => {
        switch (period) {
            case "day": return HOURLY_DATA;
            case "week": return DAILY_DATA;
            case "month": return WEEKLY_DATA;
        }
    }, [period]);

    const activityData = useMemo(() => generateActivityData(chartData), [chartData]);

    const currentKPIs = KPI_MAP[period];
    const currentStripData = KPI_STRIP_BY_PERIOD[period]; // AnvÃƒÂ¤nder dynamisk data
    const currentSuggestions = SUGGESTIONS_MAP[period];

    const toggleStream = (id: string) => {
        setActiveStreams(prev =>
            prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
        );
    };


    return (
        <div className="flex flex-col h-full overflow-hidden p-6 gap-4">

            {/* HEADER (Intern) */}
            <div className="flex items-center justify-between shrink-0">
                <div>
                    <div className="flex items-center gap-2 text-sm text-slate-400 mb-1">
                        <span>Workforce</span>
                        <ChevronRight className="w-3 h-3" />
                        <span>Planering</span>
                        <ChevronRight className="w-3 h-3" />
                        <span className="text-slate-600 font-medium">EfterfrÃƒÂ¥gan</span>
                    </div>
                    <h2 className="text-2xl font-bold text-slate-900">EfterfrÃƒÂ¥geÃƒÂ¶versikt</h2>
                </div>

                <div className="flex items-center gap-4 bg-white p-1 rounded-lg border border-slate-200 shadow-sm">
                    <div className="flex bg-slate-100 rounded-md p-0.5">
                        {['Dag', 'Vecka', 'MÃƒÂ¥nad'].map(p => {
                            const key = p === 'Dag' ? 'day' : p === 'Vecka' ? 'week' : 'month';
                            const isActive = period === key;
                            return (
                                <button
                                    key={key}
                                    onClick={() => setPeriod(key)}
                                    className={`px-4 py-1.5 text-xs font-semibold rounded-md transition-all ${isActive ? 'bg-white text-indigo-600 shadow-sm' : 'text-slate-500 hover:text-slate-700'
                                        }`}
                                >
                                    {p}
                                </button>
                            );
                        })}
                    </div>
                    <div className="h-6 w-px bg-slate-200" />
                    <button className="flex items-center gap-2 px-3 text-sm font-medium text-slate-700 hover:bg-slate-50 rounded py-1.5">
                        <CalendarIcon className="w-4 h-4 text-slate-500" />
                        <span>
                            {period === 'day' ? '24 Okt' : period === 'week' ? 'V.43 (21-27 Okt)' : 'Oktober 2025'}
                        </span>
                    </button>
                </div>
            </div>

            {/* DASHBOARD CONTENT CONTAINER */}
            <div className="flex-1 flex flex-col min-h-0 gap-4">

                {/* ZON 1: TOPP (Total KPI + Huvudgraf) */}
                <div className="flex-[0_0_42%] min-h-[300px]">
                    <Card className="h-full" noPadding>
                        <div className="flex flex-row h-full"> {/* Wrapped children to ensure row layout */}
                            {/* VÃƒÂ¤nster: Stor KPI */}
                            <div className="w-1/4 min-w-[240px] border-r border-slate-100 p-6 flex flex-col justify-center bg-slate-50/30">
                                <div className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">Totala Timmar</div>
                                <div className="flex items-baseline gap-2 mb-1">
                                    <span className="text-5xl font-bold text-slate-900 tracking-tight transition-all duration-300">
                                        {currentKPIs.total}
                                    </span>
                                    <span className="text-sm font-medium text-slate-400">h</span>
                                </div>
                                <div className="flex items-center gap-2 mb-6">
                                    <Badge color="purple">AI-Justerad</Badge>
                                    <span className="text-xs font-bold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded">{currentKPIs.delta}</span>
                                </div>
                                <div className="text-xs text-slate-400 leading-relaxed">
                                    Prognosen inkluderar <strong>{currentSuggestions.length}</strong> aktiva AI-justeringar baserade pÃƒÂ¥ {period === 'day' ? 'vÃƒÂ¤der och bokningar' : period === 'week' ? 'historiska trender' : 'sÃƒÂ¤songsvariationer'}.
                                </div>
                            </div>

                            {/* HÃƒÂ¶ger: GrafomrÃƒÂ¥de */}
                            <div className="flex-1 flex flex-col min-w-0">
                                <div className="px-6 py-4 border-b border-slate-50 flex justify-between items-center">
                                    <div className="flex items-center gap-2">
                                        <span className="w-2 h-2 rounded-full bg-indigo-500"></span>
                                        <h3 className="font-semibold text-slate-700 text-sm">Prognosvisualisering</h3>
                                    </div>
                                    <div className="flex bg-slate-100 p-0.5 rounded-lg">
                                        <button
                                            onClick={() => setViewMode('curve')}
                                            className={`flex items-center gap-2 px-3 py-1 text-xs font-medium rounded-md transition-all ${viewMode === 'curve' ? 'bg-white text-indigo-700 shadow-sm' : 'text-slate-500 hover:text-slate-700'
                                                }`}
                                        >
                                            <LineChart className="w-3 h-3" /> Kurva
                                        </button>
                                        <button
                                            onClick={() => setViewMode('activity')}
                                            className={`flex items-center gap-2 px-3 py-1 text-xs font-medium rounded-md transition-all ${viewMode === 'activity' ? 'bg-white text-indigo-700 shadow-sm' : 'text-slate-500 hover:text-slate-700'
                                                }`}
                                        >
                                            <BarChart3 className="w-3 h-3" /> Aktivitet
                                        </button>
                                    </div>
                                </div>
                                <div className="flex-1 p-4 relative min-h-0">
                                    <ResponsiveContainer width="100%" height="100%">
                                        {viewMode === 'curve' ? (
                                            <ComposedChart data={[...chartData]} margin={{ top: 10, right: 30, left: 10, bottom: 0 }}>
                                                <defs>
                                                    <linearGradient id="colorAi" x1="0" y1="0" x2="0" y2="1">
                                                        <stop offset="5%" stopColor="#8b5cf6" stopOpacity={0.1} />
                                                        <stop offset="95%" stopColor="#8b5cf6" stopOpacity={0} />
                                                    </linearGradient>
                                                </defs>
                                                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                                                <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fontSize: 11, fill: '#64748b' }} dy={10} />
                                                <YAxis yAxisId="left" axisLine={false} tickLine={false} tick={{ fontSize: 11, fill: '#64748b' }} />
                                                <YAxis yAxisId="right" orientation="right" axisLine={false} tickLine={false} hide={!activeStreams.length} />
                                                <Tooltip contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }} />
                                                <Legend verticalAlign="top" height={36} iconType="circle" />

                                                <Area yAxisId="left" type="monotone" dataKey="baseline" name="Regelbas" stroke="#cbd5e1" fill="transparent" strokeDasharray="5 5" strokeWidth={2} />
                                                <Area yAxisId="left" type="monotone" dataKey="ai_demand" name="AI-Prognos" stroke="#8b5cf6" fill="url(#colorAi)" strokeWidth={3} />

                                                {activeStreams.includes('visitors') && (
                                                    <Line yAxisId="right" type="monotone" dataKey="visitors" name="BesÃƒÂ¶kare" stroke="#10b981" strokeWidth={2} dot={false} opacity={0.6} />
                                                )}
                                                {activeStreams.includes('sales') && (
                                                    <Line yAxisId="right" type="monotone" dataKey="sales" name="FÃƒÂ¶rsÃƒÂ¤ljning" stroke="#f59e0b" strokeWidth={2} dot={false} opacity={0.6} />
                                                )}
                                            </ComposedChart>
                                        ) : (
                                                <RechartsBarChart data={[...activityData]} margin={{ top: 10, right: 30, left: 10, bottom: 0 }}>
                                                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                                                <XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fontSize: 11, fill: '#64748b' }} />
                                                <YAxis axisLine={false} tickLine={false} tick={{ fontSize: 11, fill: '#64748b' }} />
                                                <Tooltip cursor={{ fill: '#f8fafc' }} contentStyle={{ borderRadius: '8px' }} />
                                                <Legend iconType="circle" />
                                                <Bar dataKey="foh" name="Front" stackId="a" fill="#6366f1" radius={[0, 0, 2, 2]} />
                                                <Bar dataKey="ops" name="Drift" stackId="a" fill="#3b82f6" />
                                                <Bar dataKey="fb" name="F&B" stackId="a" fill="#8b5cf6" />
                                                <Bar dataKey="cleaning" name="StÃƒÂ¤d" stackId="a" fill="#06b6d4" radius={[2, 2, 0, 0]} />
                                            </RechartsBarChart>
                                        )}
                                    </ResponsiveContainer>
                                </div>
                            </div>
                        </div>
                    </Card>
                </div>

                {/* ZON 2: KPI STRIP */}
                <div className="shrink-0 grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
                    {currentStripData.map((kpi, idx) => { // Uppdaterad fÃƒÂ¶r att anvÃƒÂ¤nda dynamisk data
                        const Icon = kpi.icon;
                        const colorClass = kpi.color === 'blue' ? 'text-blue-600 bg-blue-50' :
                            kpi.color === 'amber' ? 'text-amber-600 bg-amber-50' :
                                kpi.color === 'purple' ? 'text-purple-600 bg-purple-50' :
                                    kpi.color === 'emerald' ? 'text-emerald-600 bg-emerald-50' : 'text-slate-600 bg-slate-100';
                        return (
                            <div key={idx} className="bg-white border border-slate-200 rounded-lg p-3 flex items-center justify-between shadow-sm">
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

                {/* ZON 3 & 4: NEDRE DELNING (AI-FÃƒÂ¶rslag & DatastrÃƒÂ¶mmar) */}
                <div className="flex-1 min-h-0 flex gap-4">

                    {/* ZON 3: AI-FÃƒâ€“RSLAG */}
                    <Card
                        className="flex-[2] h-full"
                        title="AI-FÃƒÂ¶rslag"
                        action={<Badge color="purple">{currentSuggestions.length} Aktiva</Badge>}
                        noPadding
                    >
                        <div className="flex-1 overflow-y-auto p-3 space-y-2 custom-scrollbar">
                            {currentSuggestions.map(s => (
                                <div key={s.id} className="p-3 bg-white border border-slate-100 rounded-lg shadow-sm hover:border-indigo-200 hover:shadow-md transition-all group">
                                    <div className="flex justify-between items-start mb-1">
                                        <div className="flex items-center gap-2">
                                            <Badge color={s.type === 'ÃƒÂ¶kning' ? 'red' : 'green'}>{s.type === 'ÃƒÂ¶kning' ? 'Ãƒâ€“kning' : 'Minskning'}</Badge>
                                            <span className="text-xs font-semibold text-slate-800">{s.title}</span>
                                        </div>
                                        <div className="flex items-center gap-1.5 text-xs font-mono text-slate-500">
                                            <Clock className="w-3 h-3 text-slate-400" />
                                            {s.impact}
                                        </div>
                                    </div>
                                    <div className="flex justify-between items-end">
                                        <div className="text-xs text-slate-500 max-w-[80%] leading-relaxed">{s.reason}</div>
                                        <div className="flex gap-2 opacity-100 lg:opacity-0 group-hover:opacity-100 transition-opacity">
                                            <button className="text-[10px] font-bold text-slate-400 hover:text-slate-600 uppercase tracking-wider px-2 py-1">Avvisa</button>
                                            <button className="text-[10px] font-bold text-indigo-600 hover:text-indigo-800 bg-indigo-50 hover:bg-indigo-100 uppercase tracking-wider px-3 py-1 rounded">GodkÃƒÂ¤nn</button>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </Card>

                    {/* ZON 4: DATASTRÃƒâ€“MMAR */}
                    <Card className="flex-1 h-full" title="DatastrÃƒÂ¶mmar" noPadding>
                        <div className="flex-1 overflow-y-auto p-2 space-y-2 custom-scrollbar">
                            {DATA_STREAMS.map(s => {
                                const isActive = activeStreams.includes(s.id);
                                const StreamIcon = s.icon;
                                return (
                                    <button
                                        key={s.id}
                                        onClick={() => toggleStream(s.id)}
                                        className={`w-full text-left p-3 rounded-lg border transition-all flex items-center justify-between group ${isActive
                                                ? 'bg-indigo-50 border-indigo-200 shadow-sm'
                                                : 'bg-white border-transparent hover:bg-slate-50'
                                            }`}
                                    >
                                        <div className="flex items-center gap-3">
                                            <div className={`p-1.5 rounded-md ${isActive ? 'bg-indigo-100 text-indigo-600' : 'bg-slate-100 text-slate-400'}`}>
                                                <StreamIcon className="w-4 h-4" />
                                            </div>
                                            <div>
                                                <div className={`text-xs font-bold uppercase mb-0.5 ${isActive ? 'text-indigo-900' : 'text-slate-500'}`}>{s.name}</div>
                                                <div className="flex items-center gap-1.5">
                                                    <span className={`w-1.5 h-1.5 rounded-full ${s.status === 'live' ? 'bg-emerald-500' : 'bg-amber-500'}`}></span>
                                                    <span className="text-[10px] text-slate-400">{s.status}</span>
                                                </div>
                                            </div>
                                        </div>
                                        <div className={`w-4 h-4 rounded border flex items-center justify-center transition-colors ${isActive ? 'bg-indigo-500 border-indigo-500' : 'bg-white border-slate-300'}`}>
                                            {isActive && <CheckCircle2 className="w-3 h-3 text-white" />}
                                        </div>
                                    </button>
                                );
                            })}
                        </div>
                    </Card>

                </div>
            </div>
        </div>
    );
};

// --- VY 4: FEEDBACK & PRECISION ---

const ScreenFeedback = () => (
    <div className="p-6 h-full flex flex-col gap-6">
        <div className="grid grid-cols-4 gap-4">
            <Card className="p-4 flex flex-row justify-between items-center">
                <div>
                    <div className="text-slate-500 text-xs">Precision (7d)</div>
                    <div className="text-2xl font-bold text-slate-900">94.2%</div>
                </div>
                <CheckCircle2 className="text-emerald-500 w-6 h-6" />
            </Card>
            <Card className="p-4 flex flex-row justify-between items-center">
                <div>
                    <div className="text-slate-500 text-xs">Bias</div>
                    <div className="text-2xl font-bold text-slate-900">+1.2%</div>
                </div>
                <div className="text-xs font-bold text-indigo-600 bg-indigo-50 px-2 py-1 rounded">Ãƒâ€“verbemannad</div>
            </Card>
        </div>

        <div className="flex-1 min-h-0 grid grid-cols-12 gap-6">
            <div className="col-span-8 h-full">
                <Card title="Precisionstrend" className="h-full">
                    <ResponsiveContainer width="100%" height="100%">
                        <RechartsLineChart data={ACCURACY_HISTORY}>
                            <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                            <XAxis dataKey="date" axisLine={false} tickLine={false} />
                            <YAxis axisLine={false} tickLine={false} domain={['auto', 'auto']} />
                            <Tooltip />
                            <Legend />
                            <Line type="monotone" dataKey="predicted" name="Prognos" stroke="#8b5cf6" strokeDasharray="5 5" />
                            <Line type="monotone" dataKey="actual" name="Utfall" stroke="#10b981" />
                        </RechartsLineChart>
                    </ResponsiveContainer>
                </Card>
            </div>
            <div className="col-span-4 h-full flex flex-col gap-6">
                <Card title="Manuell Feedback" className="flex-1">
                    <div className="flex flex-col h-full justify-center items-center text-center p-4">
                        <p className="text-slate-600 font-medium mb-6">Hur var dagens prognos?</p>
                        <div className="flex gap-4 w-full">
                            <button className="flex-1 p-4 border border-slate-200 rounded-xl hover:bg-rose-50 hover:border-rose-200 group transition-all">
                                <ThumbsDown className="w-6 h-6 mx-auto mb-2 text-slate-400 group-hover:text-rose-500" />
                                <span className="text-xs font-bold text-slate-500 group-hover:text-rose-700">FÃƒÂ¶r LÃƒÂ¥g</span>
                            </button>
                            <button className="flex-1 p-4 border border-slate-200 rounded-xl hover:bg-emerald-50 hover:border-emerald-200 group transition-all">
                                <ThumbsUp className="w-6 h-6 mx-auto mb-2 text-slate-400 group-hover:text-emerald-500" />
                                <span className="text-xs font-bold text-slate-500 group-hover:text-emerald-700">Perfekt</span>
                            </button>
                            <button className="flex-1 p-4 border border-slate-200 rounded-xl hover:bg-amber-50 hover:border-amber-200 group transition-all">
                                <ThumbsDown className="w-6 h-6 mx-auto mb-2 text-slate-400 group-hover:text-amber-500 rotate-180" />
                                <span className="text-xs font-bold text-slate-500 group-hover:text-amber-700">FÃƒÂ¶r HÃƒÂ¶g</span>
                            </button>
                        </div>
                    </div>
                </Card>
            </div>
        </div>
    </div>
);

// --- MAIN APP CONTAINER ---

export default function TooeasyModule() {
    type Tab = "overview" | "rules" | "monitor" | "feedback";
    const [activeTab, setActiveTab] = useState<Tab>('overview');
    const tabs: Array<{ id: Tab; label: string; icon: React.ComponentType<{ className?: string }> }> = [
        { id: 'overview', label: '\u00D6versikt', icon: LayoutDashboard },
        { id: 'rules', label: 'Regelbyggare', icon: FileCode },
        { id: 'monitor', label: 'Datastr\u00F6mmar', icon: Activity },
        { id: 'feedback', label: 'Feedback', icon: LineChart },
    ];

    const renderScreen = () => {
        switch (activeTab) {
            case 'rules': return <MappingEditorPage />;
            case 'monitor': return <SimulationPage />;
            case 'overview': return <SimulationOverviewPage />;
            case 'feedback': return <FeedbackPage />;
            default: return <SimulationOverviewPage />;
        }
    };

    return (
        <div className="w-full h-full bg-slate-50/50 flex flex-col font-sans text-slate-900">

            {/* MODULE NAVIGATION (Simulerar Interna Flikar) */}
            <div className="bg-white border-b border-slate-200 px-6 pt-4">
                <div className="flex items-center justify-between mb-4">
                    <div className="flex items-center gap-2">
                        <div className="bg-indigo-600 p-1.5 rounded-lg">
                            <Zap className="w-4 h-4 text-white" />
                        </div>
                        <h1 className="text-lg font-bold text-slate-900 tracking-tight">Demand Module</h1>
                    </div>
                </div>

                <div className="flex flex-wrap gap-2 border-b border-slate-200/80">
                    {tabs.map((tab) => {
                        const Icon = tab.icon;
                        const isActive = activeTab === tab.id;

                        return (
                            <button
                                key={tab.id}
                                onClick={() => setActiveTab(tab.id)}
                                className={`-mb-px inline-flex items-center gap-2 rounded-t-xl border px-4 py-3 text-sm font-semibold transition-all duration-200 ${
                                    isActive
                                        ? 'border-slate-200 border-b-white bg-white text-indigo-700 shadow-[0_-1px_0_rgba(255,255,255,0.9)]'
                                        : 'border-transparent bg-slate-100/70 text-slate-600 hover:border-slate-200 hover:bg-white/80 hover:text-slate-900'
                                }`}
                            >
                                <Icon className={`h-4 w-4 ${isActive ? "text-indigo-600" : "text-slate-400"}`} />
                                <span>{tab.label}</span>
                            </button>
                        );
                    })}
                </div>
            </div>

            {/* CONTENT AREA */}
            <div className="flex-1 overflow-hidden relative">
                {renderScreen()}
            </div>

        </div>
    );
}


export type Stream = {
  id: string;
  name: string;
  sourceSystem: string;
  industry: string;
};

export type Activity = {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
};

export type MappingVersion = {
  id: string;
  streamId: string;
  versionNumber: number;
  name: string;
  createdAtUtc: string;
};

export type MappingRule = {
  id: string;
  mappingVersionId: string;
  eventType: string;
  conditionExpression?: string | null;
  name: string;
  sortOrder: number;
};

export type MappingRuleActivity = {
  id: string;
  mappingRuleId: string;
  activityId: string;
  baseHours: number;
  unitExpression?: string | null;
  perUnitHours: number;
  multiplierExpression?: string | null;
};

export type LatestMappingResponse = {
  version: MappingVersion;
  rules: MappingRule[];
  ruleActivities: MappingRuleActivity[];
};

export type SimulationCreateResponse = { simulationId: string };

export type WorkloadBucket = {
  id: string;
  simulationRunId: string;
  activityId: string;
  intervalStartUtc: string;
  intervalEndUtc: string;
  hours: number;
  explanationJson: string;
};

export type OverviewVariance = "low" | "medium" | "high";
export type OverviewSuggestionType = "increase" | "decrease";
export type OverviewPeriodKey = "day" | "week" | "month";
export type OverviewStripIcon =
  | "clock"
  | "trending-up"
  | "sparkles"
  | "shield-alert"
  | "check-circle";
export type OverviewStripColor = "blue" | "amber" | "purple" | "emerald" | "slate";

export type SimulationOverviewSummary = {
  totalHours: number;
  deltaPercent: number;
  peakLabel: string;
  peakHours: number;
  activeAdjustments: number;
  primaryDriver: string;
};

export type SimulationOverviewChartPoint = {
  label: string;
  baseline: number;
  aiDemand: number;
  actual: number | null;
  visitors: number;
  sales: number;
  variance: OverviewVariance;
};

export type SimulationOverviewActivityPoint = {
  label: string;
  foh: number;
  ops: number;
  fb: number;
  cleaning: number;
  security: number;
};

export type SimulationOverviewSuggestion = {
  id: number;
  title: string;
  reason: string;
  impact: string;
  confidence: number;
  type: OverviewSuggestionType;
};

export type SimulationOverviewStripItem = {
  label: string;
  value: string;
  sub: string;
  icon: OverviewStripIcon;
  color: OverviewStripColor;
};

export type SimulationOverviewPeriod = {
  key: OverviewPeriodKey;
  rangeLabel: string;
  summary: SimulationOverviewSummary;
  chart: SimulationOverviewChartPoint[];
  activityMix: SimulationOverviewActivityPoint[];
  suggestions: SimulationOverviewSuggestion[];
  strip: SimulationOverviewStripItem[];
};

export type SimulationOverviewStream = {
  id: string;
  name: string;
  type: string;
  status: string;
  value: string;
};

export type SimulationOverviewResponse = {
  generatedAtUtc: string;
  date: string;
  streams: SimulationOverviewStream[];
  periods: {
    day: SimulationOverviewPeriod;
    week: SimulationOverviewPeriod;
    month: SimulationOverviewPeriod;
  };
};

export type Stream = {
  id: string;
  name: string;
  sourceSystem: string;
  industry: string;
  createdAtUtc?: string;
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
  createdByUserId?: string;
  isActive?: boolean;
  isArchived?: boolean;
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

export type MappingVersionsResponse = {
  versions: MappingVersion[];
};

export type CreateMappingRequest = {
  name: string;
  rules: Array<{
    name: string;
    eventType: string;
    conditionExpression?: string | null;
    sortOrder: number;
    activities: Array<{
      activityId: string;
      baseHours: number;
      unitExpression?: string | null;
      perUnitHours: number;
      multiplierExpression?: string | null;
    }>;
  }>;
};

export type MappingVersionResponse = {
  mappingVersion: MappingVersion;
};

export type SimulationCreateResponse = { simulationId: string };

export type SimulationRunTimeSeriesPoint = {
  intervalStartUtc: string;
  hours: number;
};

export type SimulationRunActivitySeries = {
  activityCode: string;
  points: SimulationRunTimeSeriesPoint[];
};

export type SimulationRunActivityTotal = {
  activityCode: string;
  totalHours: number;
};

export type SimulationRunSummary = {
  totalHours: number;
  peakIntervalStartUtc: string | null;
  peakIntervalHours: number;
  activityCount: number;
  primaryDriver: string;
};

export type SimulationRunResponse = {
  from: string;
  to: string;
  intervalMinutes: number;
  series: SimulationRunActivitySeries[];
  totals: SimulationRunActivityTotal[];
  summary: SimulationRunSummary;
};

export type FeedbackEntry = {
  id: string;
  mappingVersionId: string;
  mappingVersionName: string;
  ruleActivityId: string;
  activityName: string;
  intervalStartUtc: string;
  actualHours: number;
  comment?: string | null;
  createdByUserId: string;
  createdAtUtc: string;
};

export type CalibrationProfile = {
  id: string;
  mappingVersionId: string;
  mappingVersionName: string;
  ruleActivityId: string;
  activityName: string;
  factor: number;
  lambda: number;
  updatedAtUtc: string;
};

export type FeedbackResponse = {
  entries: FeedbackEntry[];
  profiles: CalibrationProfile[];
};

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

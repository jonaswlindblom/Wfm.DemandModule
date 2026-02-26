CREATE DATABASE WfmDemandModule;
GO
USE WfmDemandModule;
GO

CREATE TABLE dbo.DataStreams(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DataStreams PRIMARY KEY,
  Name NVARCHAR(200) NOT NULL,
  SourceSystem NVARCHAR(100) NOT NULL,
  Industry NVARCHAR(50) NOT NULL,
  CreatedAtUtc DATETIME2 NOT NULL
);

CREATE TABLE dbo.StreamEvents(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_StreamEvents PRIMARY KEY,
  StreamId UNIQUEIDENTIFIER NOT NULL,
  EventKey NVARCHAR(200) NOT NULL,
  EventType NVARCHAR(200) NOT NULL,
  OccurredAtUtc DATETIME2 NOT NULL,
  PayloadJson NVARCHAR(MAX) NOT NULL,
  IngestedAtUtc DATETIME2 NOT NULL,
  CONSTRAINT FK_StreamEvents_DataStreams FOREIGN KEY(StreamId) REFERENCES dbo.DataStreams(Id)
);
CREATE UNIQUE INDEX UX_StreamEvents_EventKey ON dbo.StreamEvents(EventKey);
CREATE INDEX IX_StreamEvents_StreamId_OccurredAtUtc ON dbo.StreamEvents(StreamId, OccurredAtUtc);

CREATE TABLE dbo.WorkActivities(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_WorkActivities PRIMARY KEY,
  Code NVARCHAR(80) NOT NULL,
  Name NVARCHAR(200) NOT NULL,
  IsActive BIT NOT NULL
);
CREATE UNIQUE INDEX UX_WorkActivities_Code ON dbo.WorkActivities(Code);

CREATE TABLE dbo.MappingVersions(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MappingVersions PRIMARY KEY,
  StreamId UNIQUEIDENTIFIER NOT NULL,
  VersionNumber INT NOT NULL,
  Name NVARCHAR(200) NOT NULL,
  CreatedByUserId NVARCHAR(100) NOT NULL,
  CreatedAtUtc DATETIME2 NOT NULL,
  IsArchived BIT NOT NULL CONSTRAINT DF_MappingVersions_IsArchived DEFAULT(0),
  CONSTRAINT FK_MappingVersions_DataStreams FOREIGN KEY(StreamId) REFERENCES dbo.DataStreams(Id)
);
CREATE UNIQUE INDEX UX_MappingVersions_Stream_Version ON dbo.MappingVersions(StreamId, VersionNumber);

CREATE TABLE dbo.MappingRules(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MappingRules PRIMARY KEY,
  MappingVersionId UNIQUEIDENTIFIER NOT NULL,
  EventType NVARCHAR(200) NOT NULL,
  ConditionExpression NVARCHAR(400) NULL,
  Name NVARCHAR(200) NOT NULL,
  SortOrder INT NOT NULL,
  CONSTRAINT FK_MappingRules_MappingVersions FOREIGN KEY(MappingVersionId) REFERENCES dbo.MappingVersions(Id)
);
CREATE INDEX IX_MappingRules_Version_EventType ON dbo.MappingRules(MappingVersionId, EventType);

CREATE TABLE dbo.MappingRuleActivities(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MappingRuleActivities PRIMARY KEY,
  MappingRuleId UNIQUEIDENTIFIER NOT NULL,
  ActivityId UNIQUEIDENTIFIER NOT NULL,
  BaseHours DECIMAL(10,4) NOT NULL,
  UnitExpression NVARCHAR(200) NULL,
  PerUnitHours DECIMAL(10,4) NOT NULL,
  MultiplierExpression NVARCHAR(200) NULL,
  CONSTRAINT FK_MappingRuleActivities_Rules FOREIGN KEY(MappingRuleId) REFERENCES dbo.MappingRules(Id),
  CONSTRAINT FK_MappingRuleActivities_Activities FOREIGN KEY(ActivityId) REFERENCES dbo.WorkActivities(Id)
);
CREATE INDEX IX_MappingRuleActivities_RuleId ON dbo.MappingRuleActivities(MappingRuleId);

CREATE TABLE dbo.CalibrationProfiles(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CalibrationProfiles PRIMARY KEY,
  MappingVersionId UNIQUEIDENTIFIER NOT NULL,
  RuleActivityId UNIQUEIDENTIFIER NOT NULL,
  Factor DECIMAL(10,4) NOT NULL,
  Lambda DECIMAL(10,4) NOT NULL,
  UpdatedAtUtc DATETIME2 NOT NULL,
  CONSTRAINT FK_CalibrationProfiles_Version FOREIGN KEY(MappingVersionId) REFERENCES dbo.MappingVersions(Id),
  CONSTRAINT FK_CalibrationProfiles_RuleActivity FOREIGN KEY(RuleActivityId) REFERENCES dbo.MappingRuleActivities(Id)
);
CREATE UNIQUE INDEX UX_CalibrationProfiles_Version_RuleActivity ON dbo.CalibrationProfiles(MappingVersionId, RuleActivityId);

CREATE TABLE dbo.SimulationRuns(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SimulationRuns PRIMARY KEY,
  StreamId UNIQUEIDENTIFIER NOT NULL,
  MappingVersionId UNIQUEIDENTIFIER NOT NULL,
  FromUtc DATETIME2 NOT NULL,
  ToUtc DATETIME2 NOT NULL,
  IntervalMinutes INT NOT NULL,
  CreatedByUserId NVARCHAR(100) NOT NULL,
  CreatedAtUtc DATETIME2 NOT NULL,
  CONSTRAINT FK_SimulationRuns_Stream FOREIGN KEY(StreamId) REFERENCES dbo.DataStreams(Id),
  CONSTRAINT FK_SimulationRuns_Version FOREIGN KEY(MappingVersionId) REFERENCES dbo.MappingVersions(Id)
);

CREATE TABLE dbo.WorkloadBuckets(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_WorkloadBuckets PRIMARY KEY,
  SimulationRunId UNIQUEIDENTIFIER NOT NULL,
  ActivityId UNIQUEIDENTIFIER NOT NULL,
  IntervalStartUtc DATETIME2 NOT NULL,
  IntervalEndUtc DATETIME2 NOT NULL,
  Hours DECIMAL(10,4) NOT NULL,
  ExplanationJson NVARCHAR(MAX) NOT NULL,
  CONSTRAINT FK_WorkloadBuckets_Sim FOREIGN KEY(SimulationRunId) REFERENCES dbo.SimulationRuns(Id),
  CONSTRAINT FK_WorkloadBuckets_Activity FOREIGN KEY(ActivityId) REFERENCES dbo.WorkActivities(Id)
);
CREATE INDEX IX_WorkloadBuckets_Sim_Activity_Start ON dbo.WorkloadBuckets(SimulationRunId, ActivityId, IntervalStartUtc);

CREATE TABLE dbo.FeedbackEntries(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FeedbackEntries PRIMARY KEY,
  MappingVersionId UNIQUEIDENTIFIER NOT NULL,
  RuleActivityId UNIQUEIDENTIFIER NOT NULL,
  IntervalStartUtc DATETIME2 NOT NULL,
  ActualHours DECIMAL(10,4) NOT NULL,
  Comment NVARCHAR(500) NULL,
  CreatedByUserId NVARCHAR(100) NOT NULL,
  CreatedAtUtc DATETIME2 NOT NULL,
  CONSTRAINT FK_Feedback_Version FOREIGN KEY(MappingVersionId) REFERENCES dbo.MappingVersions(Id),
  CONSTRAINT FK_Feedback_RuleActivity FOREIGN KEY(RuleActivityId) REFERENCES dbo.MappingRuleActivities(Id)
);

CREATE TABLE dbo.AuditLogEntries(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AuditLogEntries PRIMARY KEY,
  OccurredAtUtc DATETIME2 NOT NULL,
  ActorUserId NVARCHAR(100) NOT NULL,
  ActorRole NVARCHAR(50) NOT NULL,
  Action NVARCHAR(100) NOT NULL,
  EntityType NVARCHAR(100) NOT NULL,
  EntityId NVARCHAR(100) NOT NULL,
  DetailsJson NVARCHAR(MAX) NOT NULL
);
CREATE INDEX IX_Audit_OccurredAt ON dbo.AuditLogEntries(OccurredAtUtc);
GO

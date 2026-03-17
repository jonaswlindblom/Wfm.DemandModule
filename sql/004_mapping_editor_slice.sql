/*
SQL Server 2019-compatible slice for Mapping Editor and workload storage.
Index motivation is documented inline ahead of each index.
*/

CREATE TABLE dbo.StreamDefinition(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_StreamDefinition PRIMARY KEY,
  Name NVARCHAR(200) NOT NULL,
  SourceSystem NVARCHAR(100) NOT NULL,
  Industry NVARCHAR(50) NOT NULL,
  CreatedAtUtc DATETIME2 NOT NULL
);
GO

CREATE TABLE dbo.StreamEvent(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_StreamEvent PRIMARY KEY,
  StreamDefinitionId UNIQUEIDENTIFIER NOT NULL,
  EventKey NVARCHAR(200) NOT NULL,
  EventType NVARCHAR(200) NOT NULL,
  OccurredAtUtc DATETIME2 NOT NULL,
  PayloadJson NVARCHAR(MAX) NOT NULL,
  IngestedAtUtc DATETIME2 NOT NULL,
  CONSTRAINT FK_StreamEvent_StreamDefinition FOREIGN KEY(StreamDefinitionId) REFERENCES dbo.StreamDefinition(Id)
);
GO

CREATE TABLE dbo.WorkActivity(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_WorkActivity PRIMARY KEY,
  Code NVARCHAR(80) NOT NULL,
  Name NVARCHAR(200) NOT NULL,
  IsActive BIT NOT NULL
);
GO

CREATE TABLE dbo.MappingVersion(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MappingVersion PRIMARY KEY,
  StreamDefinitionId UNIQUEIDENTIFIER NOT NULL,
  VersionNumber INT NOT NULL,
  Name NVARCHAR(200) NOT NULL,
  CreatedByUserId NVARCHAR(100) NOT NULL,
  CreatedAtUtc DATETIME2 NOT NULL,
  IsActive BIT NOT NULL CONSTRAINT DF_MappingVersion_IsActive DEFAULT(0),
  IsArchived BIT NOT NULL CONSTRAINT DF_MappingVersion_IsArchived DEFAULT(0),
  CONSTRAINT FK_MappingVersion_StreamDefinition FOREIGN KEY(StreamDefinitionId) REFERENCES dbo.StreamDefinition(Id)
);
GO

CREATE TABLE dbo.MappingRule(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MappingRule PRIMARY KEY,
  MappingVersionId UNIQUEIDENTIFIER NOT NULL,
  EventType NVARCHAR(200) NOT NULL,
  ConditionExpression NVARCHAR(400) NULL,
  Name NVARCHAR(200) NOT NULL,
  SortOrder INT NOT NULL,
  CONSTRAINT FK_MappingRule_MappingVersion FOREIGN KEY(MappingVersionId) REFERENCES dbo.MappingVersion(Id)
);
GO

CREATE TABLE dbo.WorkloadResult(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_WorkloadResult PRIMARY KEY,
  MappingVersionId UNIQUEIDENTIFIER NOT NULL,
  WorkActivityId UNIQUEIDENTIFIER NOT NULL,
  IntervalStartUtc DATETIME2 NOT NULL,
  IntervalEndUtc DATETIME2 NOT NULL,
  Hours DECIMAL(10,4) NOT NULL,
  ExplanationJson NVARCHAR(MAX) NOT NULL,
  CONSTRAINT FK_WorkloadResult_MappingVersion FOREIGN KEY(MappingVersionId) REFERENCES dbo.MappingVersion(Id),
  CONSTRAINT FK_WorkloadResult_WorkActivity FOREIGN KEY(WorkActivityId) REFERENCES dbo.WorkActivity(Id)
);
GO

CREATE TABLE dbo.AuditLog(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY,
  OccurredAtUtc DATETIME2 NOT NULL,
  ActorUserId NVARCHAR(100) NOT NULL,
  ActorRole NVARCHAR(50) NOT NULL,
  Action NVARCHAR(100) NOT NULL,
  EntityType NVARCHAR(100) NOT NULL,
  EntityId NVARCHAR(100) NOT NULL,
  DetailsJson NVARCHAR(MAX) NOT NULL
);
GO

CREATE UNIQUE INDEX UX_StreamEvent_EventKey ON dbo.StreamEvent(EventKey);
GO
/* Supports events per interval reads and event replay by stream/date range. */
CREATE INDEX IX_StreamEvent_StreamDefinition_OccurredAtUtc ON dbo.StreamEvent(StreamDefinitionId, OccurredAtUtc);
GO

CREATE UNIQUE INDEX UX_WorkActivity_Code ON dbo.WorkActivity(Code);
GO

/* Supports mapping version lookup by stream/version and keeps version numbers unique. */
CREATE UNIQUE INDEX UX_MappingVersion_StreamDefinition_VersionNumber ON dbo.MappingVersion(StreamDefinitionId, VersionNumber);
GO
/* Supports active version lookup for the Mapping Editor. */
CREATE INDEX IX_MappingVersion_StreamDefinition_IsActive ON dbo.MappingVersion(StreamDefinitionId, IsActive);
GO

/* Supports rule expansion for a chosen mapping version and event type. */
CREATE INDEX IX_MappingRule_MappingVersion_EventType ON dbo.MappingRule(MappingVersionId, EventType);
GO

/* Supports workload per activity and interval for dashboards and drill-downs. */
CREATE INDEX IX_WorkloadResult_WorkActivity_IntervalStartUtc ON dbo.WorkloadResult(WorkActivityId, IntervalStartUtc);
GO
/* Supports filtering workload result sets by mapping version before activity aggregation. */
CREATE INDEX IX_WorkloadResult_MappingVersion_IntervalStartUtc ON dbo.WorkloadResult(MappingVersionId, IntervalStartUtc);
GO

/* Supports audit browsing by newest changes first. */
CREATE INDEX IX_AuditLog_OccurredAtUtc ON dbo.AuditLog(OccurredAtUtc);
GO

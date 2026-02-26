USE WfmDemandModule;
GO
DECLARE @Days INT = 90;

DELETE FROM dbo.StreamEvents
WHERE OccurredAtUtc < DATEADD(DAY, -@Days, SYSUTCDATETIME());
GO

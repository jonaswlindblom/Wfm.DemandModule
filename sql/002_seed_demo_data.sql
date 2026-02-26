USE WfmDemandModule;
GO

DECLARE @Reception UNIQUEIDENTIFIER = NEWID();
DECLARE @Housekeeping UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.WorkActivities(Id, Code, Name, IsActive) VALUES
(@Reception, 'Reception', 'Reception', 1),
(@Housekeeping, 'Housekeeping', 'Housekeeping', 1);

DECLARE @StreamCamping UNIQUEIDENTIFIER = NEWID();
INSERT INTO dbo.DataStreams(Id, Name, SourceSystem, Industry, CreatedAtUtc)
VALUES (@StreamCamping, 'Demo Camping Booking', 'PMS', 'camping', SYSUTCDATETIME());

DECLARE @Mv UNIQUEIDENTIFIER = NEWID();
INSERT INTO dbo.MappingVersions(Id, StreamId, VersionNumber, Name, CreatedByUserId, CreatedAtUtc, IsArchived)
VALUES (@Mv, @StreamCamping, 1, 'Initial camping mapping', 'seed', SYSUTCDATETIME(), 0);

DECLARE @Rule UNIQUEIDENTIFIER = NEWID();
INSERT INTO dbo.MappingRules(Id, MappingVersionId, EventType, ConditionExpression, Name, SortOrder)
VALUES (@Rule, @Mv, 'CampingBookingCreated', NULL, 'Booking created (camping)', 1);

DECLARE @RA1 UNIQUEIDENTIFIER = NEWID();
DECLARE @RA2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.MappingRuleActivities(Id, MappingRuleId, ActivityId, BaseHours, UnitExpression, PerUnitHours, MultiplierExpression)
VALUES (@RA1, @Rule, @Reception, 0.3000, 'count($.addOns)', 0.0500, NULL);

INSERT INTO dbo.MappingRuleActivities(Id, MappingRuleId, ActivityId, BaseHours, UnitExpression, PerUnitHours, MultiplierExpression)
VALUES (@RA2, @Rule, @Housekeeping, 0.0000, 'stayNights($.checkInDate,$.checkOutDate)', 0.6000, 'cabinTypeFactor($.cabinType)');

INSERT INTO dbo.CalibrationProfiles(Id, MappingVersionId, RuleActivityId, Factor, Lambda, UpdatedAtUtc)
VALUES
(NEWID(), @Mv, @RA1, 1.0000, 0.1000, SYSUTCDATETIME()),
(NEWID(), @Mv, @RA2, 1.0000, 0.1000, SYSUTCDATETIME());

INSERT INTO dbo.StreamEvents(Id, StreamId, EventKey, EventType, OccurredAtUtc, PayloadJson, IngestedAtUtc)
VALUES
(NEWID(), @StreamCamping, 'camp-001', 'CampingBookingCreated', DATEADD(HOUR,-10,SYSUTCDATETIME()),
 N'{"bookingId":"B1","checkInDate":"2026-02-20","checkOutDate":"2026-02-23","guests":4,"cabinType":"Deluxe","addOns":["Sauna","LateCheckOut"]}', SYSUTCDATETIME()),
(NEWID(), @StreamCamping, 'camp-002', 'CampingBookingCreated', DATEADD(HOUR,-8,SYSUTCDATETIME()),
 N'{"bookingId":"B2","checkInDate":"2026-02-21","checkOutDate":"2026-02-22","guests":2,"cabinType":"Standard","addOns":[]}', SYSUTCDATETIME()),
(NEWID(), @StreamCamping, 'camp-003', 'CampingBookingCreated', DATEADD(HOUR,-3,SYSUTCDATETIME()),
 N'{"bookingId":"B3","checkInDate":"2026-02-21","checkOutDate":"2026-02-24","guests":3,"cabinType":"Tent","addOns":["BikeRental"]}', SYSUTCDATETIME());
GO

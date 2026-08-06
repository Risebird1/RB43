/*
================================================================================
IrfAuditRiskBackfillRun — stamp for per-IRF windowed RiskCategory backfill runs
(Last backfill timestamp + window on IRF Audit Directory).
Idempotent: IF NOT EXISTS. Also ensured at runtime by
AutoQcRemovalMonitorRepository.RecordRiskBackfillRunAsync.
================================================================================
*/
IF OBJECT_ID(N'dbo.IrfAuditRiskBackfillRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IrfAuditRiskBackfillRun
    (
        Id            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IrfId         INT NOT NULL,
        ExecutedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_IrfAuditRiskBackfillRun_ExecutedAtUtc DEFAULT (SYSUTCDATETIME()),
        FromIst       DATE NOT NULL,
        ToIst         DATE NOT NULL,
        Scanned       INT NOT NULL CONSTRAINT DF_IrfAuditRiskBackfillRun_Scanned DEFAULT (0),
        Processed     INT NOT NULL CONSTRAINT DF_IrfAuditRiskBackfillRun_Processed DEFAULT (0),
        SyncUpdates   INT NOT NULL CONSTRAINT DF_IrfAuditRiskBackfillRun_SyncUpdates DEFAULT (0),
        ExecutedBy    VARCHAR(150) NULL,
        Message       VARCHAR(500) NULL
    );

    CREATE INDEX IX_IrfAuditRiskBackfillRun_IrfId_ExecutedAtUtc
        ON dbo.IrfAuditRiskBackfillRun (IrfId, ExecutedAtUtc DESC);
END
GO

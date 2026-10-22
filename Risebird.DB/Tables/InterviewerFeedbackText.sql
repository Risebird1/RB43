/*
================================================================================
InterviewerFeedbackText — interviewer-facing wording for the "My Quality" page.

One row per coaching theme key (InterviewerCoachingBriefBuilder.Theme*). The page
uses Title and WhatToDo when a row exists and IsActive = 1; otherwise it falls back
to the code default in InterviewerMyQualityService, so QC can change the wording
without a deploy and an empty table is valid.

Deduction reasons on the page can be reworded the same way: ThemeKey = 'Reason.<column>'
(e.g. 'Reason.TimeManagement', any PaymentSLADetails reason column); only Title is used.

Never put thresholds, internal labels (P0-P5, High-Risk, Remove / Watch) or
fraud / proxy wording in these texts: interviewers read them.

Read by usp_GetInterviewerMyQuality. Idempotent.
================================================================================
*/
IF OBJECT_ID(N'dbo.InterviewerFeedbackText', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InterviewerFeedbackText
    (
        ThemeKey   VARCHAR(50)    NOT NULL,
        Title      NVARCHAR(200)  NOT NULL,
        WhatToDo   NVARCHAR(500)  NOT NULL,
        IsActive   BIT            NOT NULL CONSTRAINT DF_InterviewerFeedbackText_IsActive DEFAULT (1),
        ModifiedBy VARCHAR(150)   NULL,
        ModifiedAt DATETIME       NOT NULL CONSTRAINT DF_InterviewerFeedbackText_ModifiedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_InterviewerFeedbackText PRIMARY KEY CLUSTERED (ThemeKey ASC)
    );
END
GO

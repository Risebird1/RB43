/*
================================================================================
usp_GetReportEdit — QC Activity "View change" (GET /QCActivity/ReportEdit?id=).

One ReportEditLog row with the full text before and after (HTML-encoded, as
the app saves it), the editor, the edit time in IST, the interviewer and the
IRF. Returns an empty result set (same columns) when the id is unknown or the
table is missing. Who may see which edit is decided by the web layer.

Deploy after Tables/ReportEditLog.sql (still runs without it).
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetReportEdit]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.ReportEditLog', N'U') IS NULL OR @Id IS NULL OR @Id <= 0
    BEGIN
        SELECT
            CAST(NULL AS INT)            AS Id,
            CAST(NULL AS INT)            AS InterviewId,
            CAST(NULL AS VARCHAR(60))    AS FieldName,
            CAST(NULL AS NVARCHAR(MAX))  AS OldValue,
            CAST(NULL AS NVARCHAR(MAX))  AS NewValue,
            CAST(NULL AS VARCHAR(200))   AS EditedBy,
            CAST(NULL AS VARCHAR(310))   AS EditorName,
            CAST(NULL AS DATETIME)       AS EditedAtIst,
            CAST(NULL AS VARCHAR(200))   AS InterviewerEmail,
            CAST(NULL AS VARCHAR(310))   AS InterviewerName,
            CAST(NULL AS INT)            AS IrfId,
            CAST(NULL AS VARCHAR(200))   AS IrfName
        WHERE 1 = 0;
        RETURN;
    END

    EXEC sp_executesql N'
        SELECT TOP 1
            l.Id,
            l.InterviewId,
            l.FieldName,
            l.OldValue,
            l.NewValue,
            l.EditedBy,
            ed.FullName                     AS EditorName,
            DATEADD(MINUTE, 330, l.EditedAtUtc) AS EditedAtIst,
            im.interviewer                  AS InterviewerEmail,
            iv.FullName                     AS InterviewerName,
            irf.id                          AS IrfId,
            irf.IRFId                       AS IrfName
        FROM dbo.ReportEditLog l
        LEFT JOIN dbo.InterviewMeetings im ON im.id = l.InterviewId
        OUTER APPLY (SELECT TOP 1 i.id, i.IRFId FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
        OUTER APPLY (
            SELECT TOP 1 NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '''') + '' '' + ISNULL(ul.LastName, ''''))), '''') AS FullName
            FROM dbo.UserList ul
            WHERE ul.EmailId = l.EditedBy
            ORDER BY ul.id DESC) ed
        OUTER APPLY (
            SELECT TOP 1 NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '''') + '' '' + ISNULL(ul.LastName, ''''))), '''') AS FullName
            FROM dbo.UserList ul
            WHERE im.interviewer IS NOT NULL AND ul.EmailId = im.interviewer
            ORDER BY ul.id DESC) iv
        WHERE l.Id = @Id;',
        N'@Id INT', @Id = @Id;
END
GO

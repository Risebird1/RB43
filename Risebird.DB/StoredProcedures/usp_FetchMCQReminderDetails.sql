IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchMCQReminderDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchMCQReminderDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_FetchMCQReminderDetails
AS
BEGIN
Select msl.*,ms.Timer,ms.Title from MCQShareList msl left join MCQCandidates mc on mc.MCQId=msl.MCQId and mc.CandidateEmail=msl.EmailAddress
join MCQSets ms on ms.PartitionKey=msl.MCQId
where mc.id is null and DATEADD(MINUTE,330, Getdate())<DATEADD(hour,msl.Expiretime,msl.ShareDate)
END
GO

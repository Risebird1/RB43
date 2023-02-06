IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchClientIRFFeedbackCountList'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchClientIRFFeedbackCountList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_FetchClientIRFFeedbackCountList
AS
BEGIN
Declare @tempIRF Table(
IRFId varchar(15)
)
Insert into @tempIRF select Distinct im.IRFId from InterviewMeetings im where im.interviewDate>=getdate() and im.interviewDate<=DATEADD(DAY,1,getdate())
Select IRF.IRFId,Sum(cir.Rating) as Rating,Count(cir.Rating) as countRating,min(cir.Rating) minRating,max(cir.Rating) maxRating 
From IRF join ClientIRFReviewDetails cir on IRF.id=cir.IRFId
join @tempIRF tr on tr.IRFId=IRF.IRFId
group by IRF.IRFId
END


Go
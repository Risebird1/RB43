IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPaymentSlaDetailsById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPaymentSlaDetailsById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetPaymentSlaDetailsById
@Interviewid int
AS
BEGIN
Select ps.*,IRF.id as IRFId,im.candidateEmail,im.interviewer,tq.Redflags,Case when tq.id>0 then 1 else 0 end as QCStatus
From PaymentSLADetails ps left join InterviewMeetings im on ps.Interviewid=im.id left join IRF on IRF.IRFId=im.IRFId 
left join Trans_QCFormReport tq on tq.interviewid=im.id
where ps.Interviewid=@Interviewid
END
GO
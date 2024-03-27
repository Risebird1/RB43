IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPaymentSlaHistoryDetailsById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPaymentSlaHistoryDetailsById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetPaymentSlaHistoryDetailsById
@Interviewid int 
AS  
BEGIN 
Select ps.*,IRF.id as IRFId,im.candidateEmail,im.interviewer,tq.Redflags,Case when tq.id>0 then 1 else 1 end as QCStatus,IsNull(ip.VideoReward,0) as VideoReward   
From PaymentSLADetailsHistory ps left join InterviewMeetings im on ps.Interviewid=im.id left join IRF on IRF.IRFId=im.IRFId   
left join Trans_QCFormReport tq on tq.interviewid=im.id  
left join UserList ul on ul.EmailId=im.interviewer
left join InterviewPoints ip on ip.Interviewid=im.id and ip.Userid=ul.id
where ps.Interviewid=@Interviewid  
END
GO
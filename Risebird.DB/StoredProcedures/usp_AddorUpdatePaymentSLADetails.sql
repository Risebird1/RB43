IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddorUpdatePaymentSLADetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddorUpdatePaymentSLADetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE usp_GetPaymentSlaDetailsById 
@Interviewid int ,
@type varchar(50)
AS  
BEGIN 
if(@type Like '%Candidate%')
BEGIN
Select ps.*,IRF.id as IRFId,IRF.IsVideoRequired,im.candidateEmail,im.interviewer
From CandidateFeedbackDetails ps left join InterviewMeetings im on ps.Interviewid=im.id left join IRF on IRF.IRFId=im.IRFId   
where ps.Interviewid=@Interviewid 
END
else
BEGIN
Select ps.*,IRF.id as IRFId,IRF.IsVideoRequired,im.candidateEmail,im.interviewer,tq.Redflags,Case when tq.id>0 then 1 else 1 end as QCStatus,IsNull(ip.VideoReward,0) as VideoReward   
From InterviewMeetings im  left join IRF on IRF.IRFId=im.IRFId   
left join PaymentSLADetails ps on ps.Interviewid=im.id
left join Trans_QCFormReport tq on tq.interviewid=im.id  
left join UserList ul on ul.EmailId=im.interviewer
left join InterviewPoints ip on ip.Interviewid=im.id and ip.Userid=ul.id
where im.id=@Interviewid  
END 
END

GO
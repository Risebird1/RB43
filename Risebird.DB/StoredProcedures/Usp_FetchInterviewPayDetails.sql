
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_FetchInterviewPayDetails'))
BEGIN
    DROP PROCEDURE dbo.Usp_FetchInterviewPayDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure Usp_FetchInterviewPayDetails
@Emailid varchar(200)          
As          
BEGIN          
Declare @Userid int          
Select @Userid=id from UserList where EmailId=@Emailid  
;with CTE as (Select distinct  ip.id,ip.InterviewId,ip.Type,im.candidateName as Comment,ip.Status,ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ic.InterviewStatus,ir.JobCountry as  PreferredLocation,ic.VideoLink, ic.VideoDuration,IsNull(IsNull(ISNULL(ti.PerSuccessRate,tei.PerSuccessRate),tqi.PerSuccessRate),ip.Points) as Rate,ip.Comment as CrossCheckvalue,
Case When ti.Surcharge>0 and ir.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and ir.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,
Case When cr.Question2=5 and ir.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
Case When clr.StarRating=5 and ir.IsNewSkillChannel=1 then 50 Else 0 END as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip           
Left join InterviewMeetings im on im.id=ip.InterviewId left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId    
left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=ip.userid left join TransEmergencyInterviewer tei on tei.IRFId=ir.id and tei.UserId=ip.userid             
left join TransQualityController tqi on tqi.IRFId=ir.id and tqi.UserId=ip.userid   
left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
left join InterviewRatings clr on clr.Interviewid=im.id 
where ip.userid=@Userid and ip.Interviewid!=0 and ISNULL(ir.IsNewSkillChannel,0)=1        
union
Select distinct  ip.id,ip.InterviewId,ip.Type,im.candidateName as Comment,ip.Status,ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ic.InterviewStatus,ir.JobCountry as  PreferredLocation,ic.VideoLink, ic.VideoDuration,IsNull(IsNull(ISNULL(ti.PerSuccessRate,tei.PerSuccessRate),tqi.PerSuccessRate),ip.Points) as Rate,ip.Comment as CrossCheckvalue,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip           
Left join InterviewMeetings im on im.id=ip.InterviewId left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId    
left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=ip.userid left join TransEmergencyInterviewer tei on tei.IRFId=ir.id and tei.UserId=ip.userid             
left join TransQualityController tqi on tqi.IRFId=ir.id and tqi.UserId=ip.userid   
left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
left join InterviewRatings clr on clr.Interviewid=im.id 
where ip.userid=@Userid and ip.Interviewid!=0 and ISNULL(ir.IsNewSkillChannel,0)=0        
union        
Select distinct ip.id,ip.InterviewId,ip.Type,im.candidateName as Comment,ip.Status,ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ic.InterviewStatus,'' as PreferredLocation,ic.VideoLink, '' as VideoDuration,0 as Rate,ip.Comment as CrossCheckvalue,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip           
Left join UnapprovedInterviewerMeetings im on im.id=ip.InterviewId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId           
where ip.userid=@Userid and ip.Interviewerid!=0            
union       
Select distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ic.InterviewStatus,'' as PreferredLocation,'' as VideoLink, '' as VideoDuration,0 as Rate,ip.Comment as CrossCheckvalue,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip        
Left join UnapprovedInterviewerMeetings im on im.id=ip.InterviewId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId           
where ip.userid=@Userid and ip.Interviewerid=0 and ip.Interviewid=0           
)
select id,InterviewId,Type,Comment,Status,(PrePoints+Surcharge) as PrePoints,Penaltyamount,(Points+Isnull(Surcharge,0)+ISNULL(CandidateRatingAmount,0)+ISNULL(ClientRatingAmount,0)+ISNULL(QCPoints,0)) as Points,
Date,MeetingStatus,InterviewStatus,PreferredLocation,VideoLink,VideoDuration,Rate,CrossCheckvalue,Surcharge,CandidateRatingAmount,ClientRatingAmount,QCPoints
From CTE
END      
  GO
    




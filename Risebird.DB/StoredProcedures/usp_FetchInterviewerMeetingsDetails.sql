IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchInterviewerMeetingsDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewerMeetingsDetails --427906,16327,267339,'E1F6316E-BCF7-45AB-8297-2B1FDF42A0B5'
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO              
CREATE Procedure usp_FetchInterviewerMeetingsDetails --449669,11840
@interviewId int
As                      
BEGIN                      
                  
Select IRF.id as Irfid,im.id as Interviewid,im.interviewer,im.interviewDate,ul.FirstName,2 as Typefunction,im.MobileNumber,cl.Name as ClientName, cl.CompanyLink, cl.LogoLink, Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkillInvite, 
im.zoomLink as MeetingLink , im.TimeZonesValue, im.TimeZoneoffset                    
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId left join Userlist ul on ul.EmailId=im.interviewer 
where  im.id=@interviewId
END 
 Go
  



  
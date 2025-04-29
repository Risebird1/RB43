IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchCandidateMeetingsDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchCandidateMeetingsDetails --427906,16327,267339,'E1F6316E-BCF7-45AB-8297-2B1FDF42A0B5'
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO              
CREATE Procedure usp_FetchCandidateMeetingsDetails
@interviewId int,
@irfId int,
@candidateId int,
@uniqueId varchar(50)
As                      
BEGIN                      
                  
Select IRF.id as Irfid,im.id as Interviewid,candidateEmail as CandidateEmail,im.interviewDate,ul.FirstName,2 as Typefunction,
cm.id as candidateid,ul.uniqueid, im.staffingAgency,im.MobileNumber,cl.Name as ClientName, cl.CompanyLink, cl.LogoLink, Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as PrimarySkillInvite, 
im.zoomLink as MeetingLink , im.TimeZonesValue, im.TimeZoneoffset                    
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId left join Userlist ul on ul.EmailId=im.candidateEmail                       
left join CandidatesMaster cm on cm.UserId=ul.id left join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id                     
where IRF.id=@irfId and  im.id=@interviewId and ul.uniqueid=@uniqueId and cm.id=@candidateid
END 
 Go
  



  
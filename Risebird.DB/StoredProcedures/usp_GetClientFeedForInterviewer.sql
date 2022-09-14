/****** Object:  StoredProcedure [dbo].[usp_GetClientFeedForInterviewer]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('[usp_GetClientFeedForInterviewer]'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClientFeedForInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE PROCEDURE [dbo].[usp_GetClientFeedForInterviewer]  
  
@interviewerEmail varchar(50),
@timeNow datetime,
@postWhichDate datetime = null
  
AS  
Begin  

declare @interviewerUserId int

set @interviewerUserId = (select id from UserList where EmailId = @interviewerEmail and idProfile=3)--idprofile 3 for interviewer

DECLARE @tbl_AllFeed TABLE (          
    SkillChannelName varchar(100),
	[FeedDate] datetime,
	[FeedType] varchar(25),
	[FeedText] varchar(max),
	[FeedSubject] varchar(500) null,
	[FeedUrl] varchar(1000),
	[FeedRating] int,
	[RatedBy] varchar(100),
	[CandidateName] varchar(100),
	[InterviewDate] datetime
) 

DECLARE @tbl_RelevantSkillChannels TABLE (          
    IRFId INT        
) 

insert into @tbl_RelevantSkillChannels

select distinct IRF.id from IRF 
join TransInterviewer ti on ti.IRFId = irf.id
where ti.UserId = @interviewerUserId and (irf.IsDeleted is null or irf.IsDeleted = 0)
and cast(irf.EndDate as date) >= cast(@timeNow as date)

insert into @tbl_AllFeed
select irf.IRFId, irf.CreatedOn, 'Audio', null, null, AudioComment, null, null, null, null from IRF
join @tbl_RelevantSkillChannels rsc on rsc.IRFId = irf.id
where (@postWhichDate is null or CAST(FORMAT(irf.CreatedOn ,'yyyy-MM-dd HH:mm:ss') AS datetime) > @postWhichDate)

insert into @tbl_AllFeed
select irf.IRFId, cbm.SentAt, 'Broadcast', cbm.[Message], cbm.[Subject], null, null, null, null, null from ClientBroadcastMessages cbm
join @tbl_RelevantSkillChannels rsc on rsc.IRFId = cbm.IRFId
join IRF on IRF.id = cbm.IRFId
where (@postWhichDate is null or CAST(FORMAT(cbm.SentAt ,'yyyy-MM-dd HH:mm:ss') AS datetime) > @postWhichDate)

insert into @tbl_AllFeed
select irf.IRFId, ccr.CreatedAt, 'Rating', ccr.[Comment], null, null, ccr.Rating, ccr.CreatedBy,
case         
  when ul.LastName is null then ul.FirstName        
  else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))        
end, 
im.interviewDate
from ClientCommentsForReport ccr
join InterviewMeetings im on im.id = ccr.InterviewId
join IRF on IRF.IRFId = im.IRFId
join @tbl_RelevantSkillChannels rsc on rsc.IRFId = IRF.id
join UserList ul on ul.EmailId = im.candidateEmail
where im.interviewer = @interviewerEmail
and (@postWhichDate is null or CAST(FORMAT(ccr.CreatedAt ,'yyyy-MM-dd HH:mm:ss') AS datetime) > @postWhichDate)

select * from @tbl_AllFeed order by FeedDate desc

End  
  
  
GO

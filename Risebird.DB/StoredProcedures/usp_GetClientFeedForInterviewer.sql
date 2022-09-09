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
	[FeedUrl] varchar(1000)
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
select irf.IRFId, irf.CreatedOn, 'Audio', null, AudioComment from IRF
join @tbl_RelevantSkillChannels rsc on rsc.IRFId = irf.id


select * from @tbl_AllFeed order by FeedDate desc

End  
  
  
GO

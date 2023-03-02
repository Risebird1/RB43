IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GenerateOneTimeInterviewEvent'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GenerateOneTimeInterviewEvent]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure usp_GenerateOneTimeInterviewEvent      
@EventName varchar(150),      
@SkillChannel varchar(50),      
@StartDate datetime,      
@EndDate datetime,      
@duration int,      
@Uniqueid varchar(50),
@Concurrency int,
@candidateEmail varchar(200)
AS      
BEGIN      
  
Insert into ScheduleInterviewEvent(EventName,SkillChannel,StartDate,EndDate,duration,Uniqueid,IsActive,  
IsSingleUse,IsOnetimeUsed,SkillChannelRelation,Concurrency,CandidateEmail)      
values(@EventName,null,@StartDate,@EndDate,@duration,@Uniqueid,1,1,0,@SkillChannel,@Concurrency,@candidateEmail)      
      
Select SCOPE_IDENTITY()      
END      
GO
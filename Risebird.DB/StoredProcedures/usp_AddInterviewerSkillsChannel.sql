/****** Object:  StoredProcedure [dbo].[usp_AddInterviewerSkillsChannel]    Script Date: 25-05-2020 10.53.57 AM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewerSkillsChannel'))
BEGIN
    DROP PROCEDURE dbo.usp_AddInterviewerSkillsChannel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




Create PROCEDURE [dbo].[usp_AddInterviewerSkillsChannel]  
  
@InterviewerSkillId nvarchar(200),  
@JDTiltle nvarchar(200),  
@StartDate datetime,  
@EndDate datetime,  
@RisebirdHr int,  
@PerMintuteRate real,  
@CalendlyLink varchar(500),  
@PrimarySkill int,  
@HeuristicProfileType int,  
@JDLink varchar(2000),
@AssessmentGroupId varchar(200),
@idIsc int output        

AS  
Begin  
  
INSERT INTO [dbo].[InterviewerSkillsChannel]  
           ([InterviewerSkillId]  
           ,[JDTiltle]  
           ,[StartDate]  
           ,[EndDate]             
           ,[RisebirdHr]  
           ,[PerMintuteRate]    
     ,[CalendlyLink]  
     ,[PrimarySkill]  
     ,[HeuristicProfileType]  
     ,[JDLink]  
     ,[AssessmentGroupId])  
       VALUES  
           (@InterviewerSkillId,  
@JDTiltle,  
@StartDate,  
@EndDate,  
@RisebirdHr,  
@PerMintuteRate,  
@CalendlyLink,  
@PrimarySkill,  
@HeuristicProfileType,  
@JDLink,
@AssessmentGroupId
)  

select @idIsc= SCOPE_IDENTITY()

  
End  
  

GO



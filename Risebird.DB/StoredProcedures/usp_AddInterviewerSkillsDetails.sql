/****** Object:  StoredProcedure [dbo].[usp_AddInterviewerSkillsDetails]    Script Date: 5/14/2020 7:21:16 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddInterviewerSkillsDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddInterviewerSkillsDetails
END


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_AddInterviewerSkillsDetails]

@InterviewerSkillId nvarchar(200),
@JDTiltle nvarchar(200),
@StartDate datetime,
@EndDate datetime,
@RisebirdHr int,
@PerMintuteRate real,
@CalendlyLink varchar(500),
@PrimarySkill int,
@UserId int


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
		   ,[UserId]
		   )
       VALUES
           (@InterviewerSkillId,
@JDTiltle,
@StartDate,
@EndDate,
@RisebirdHr,
@PerMintuteRate,
@CalendlyLink,
@PrimarySkill,
@UserId
)

End

Go

/****** Object:  StoredProcedure [dbo].[usp_GetAllInterviewerSkills]    Script Date: 5/14/2020 7:15:57 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAllInterviewerSkills'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllInterviewerSkills
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetAllInterviewerSkills]  
@Profile varchar(100)
AS  
Begin 

DECLARE @tbl_InterviewerTo TABLE (
    PrimarySkill int,
	RisebirdHr int,
	UserId int,
	EndDate date,
	Id int,
	InterviewerSkillId nvarchar(MAX),
	PerMintuteRate real,
	JDTiltle nvarchar(500),
	IsPaused bit,
	CalendlyLink varchar(500)
);
INSERT INTO @tbl_InterviewerTo
select PrimarySkill,RisebirdHr,UserId,EndDate,Id,InterviewerSkillId,PerMintuteRate,JDTiltle,IsPaused,CalendlyLink from InterviewerSkillsChannel
where RisebirdHr in (select id from  UserList ul where idProfile= (select id from [Profile] where [Name]=@Profile))

begin
Select UL.EmailId,IVT.*, S.[Name] from  UserList UL inner join @tbl_InterviewerTo IVT on UL.id=IVT.RisebirdHr left join Skill S on s.id= IVT.PrimarySkill order by IVT.InterviewerSkillId desc
End
end
GO
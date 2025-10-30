/****** Object:  StoredProcedure [dbo].[usp_GetPendingVideoInstantMeetings]    ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPendingVideoInstantMeetings'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPendingVideoInstantMeetings
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetPendingVideoInstantMeetings]  
@timeStart datetime,
@timeEnd datetime
AS  
Begin 

select im.id as imid, im.RequestedDate, im.zoomLink from InstaInterviewMeetings im 
where im.RequestedDate<@timeEnd and im.RequestedDate>@timeStart and (im.VideoLink is null or im.VideoLink='') and (im.ZoomLink is not null or im.ZoomLink!='')


--INSERT INTO dbo.TechSkillComments (IRFId, Comment, UserId, SkillName)
--SELECT distinct
--    irf.id,
--    --IM.IRFId,                          -- maps InterviewMeetings.IRFId ? TechSkillComments.IRFId
--    QD.Comment,                        -- maps QuestionsDiscussed.Comment ? TechSkillComments.Comment
--    UL.id AS UserId,                   -- UserList.id ? TechSkillComments.UserId
--    QD.SkillName                       -- maps QuestionsDiscussed.SkillName ? TechSkillComments.SkillName
--FROM dbo.QuestionsDiscussed QD
--INNER JOIN dbo.InterviewMeetings IM 
--    ON QD.InterviewId = IM.id
--	inner join irf on irf.irfid = im.irfid
--INNER JOIN dbo.UserList UL
--    ON IM.Interviewer = UL.EmailId
--WHERE NOT EXISTS (
--    SELECT 1
--    FROM dbo.TechSkillComments TSC
--	join IRF on irf.id = tsc.IRFId
--    WHERE irf.IRFId = IM.IRFId
--      AND ISNULL(TSC.Comment, '') = ISNULL(QD.Comment, '')
--      AND ISNULL(TSC.SkillName, '') = ISNULL(QD.SkillName, '')
--      AND TSC.UserId = UL.id
--) and cast(IM.interviewDate as date) > cast(dateadd(day, -30, getdate()) as date)


End
GO



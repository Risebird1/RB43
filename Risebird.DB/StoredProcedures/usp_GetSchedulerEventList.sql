IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSchedulerEventList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSchedulerEventList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetSchedulerEventList
AS
BEGIN
Select * from ScheduleInterviewEvent where ISNull(IsSingleUse,0)!=1 order by id desc
END
GO
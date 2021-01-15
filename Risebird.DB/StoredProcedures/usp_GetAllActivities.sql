/****** Object:  StoredProcedure [dbo].[usp_GetAllActivities]    Script Date: 13-01-2021 5.15.51 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllActivities'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllActivities
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllActivities]  

AS  
Begin

	select act.*, atos.ActivityName as ActivityTypeDesc from ActivityTracker act join ActivityTypeOptions atos on act.[ActivityType]=atos.id order by PerformedAt desc

End
GO



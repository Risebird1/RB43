/****** Object:  StoredProcedure [dbo].[usp_GetTaskStatusList]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTaskStatusList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTaskStatusList
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetTaskStatusList]    
@IRFId int,     
@taskName varchar(75),
@taskValue varchar(75)
AS    
Begin  
  
select SkillChannelTasks.*, 
CASE
    WHEN [Status] = 1 THEN 'In Progress'
    WHEN [Status] = 2 THEN 'Completed'
    ELSE 'In Progress'
END as StatusValue
from SkillChannelTasks where IRFId =@IRFId and TaskName = @taskName and TaskValue = @taskValue

End  
GO



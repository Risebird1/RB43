/****** Object:  StoredProcedure [dbo].[usp_AddTaskStatus]    Script Date: 24-11-2020 2.34.29 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddTaskStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTaskStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
Create PROCEDURE [dbo].[usp_AddTaskStatus]      
@IRFId int,           
@Comments varchar(500),      
@CreatedOn datetime,           
@Status int,      
@PerformedBy varchar(100),      
@TaskName varchar(50),
@TaskValue varchar(50)
          
AS      
BEGIN      
	insert into SkillChannelTasks (IRFId, [Status], TaskName, TaskValue, CreatedOn, PerformedBy, Comments)
	values (@IRFId, @Status, @TaskName, @TaskValue, @CreatedOn, @PerformedBy, @Comments)
         
END      
      
       
  
GO



IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddUpdateClientCommentsForReport'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateClientCommentsForReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

    
    
Create PROCEDURE [dbo].[usp_AddUpdateClientCommentsForReport]      
      
@Comment varchar(max),
@id int,
@InterviewId int,
@CreatedBy varchar(50),
@timeNow datetime,
@Rating int
      
AS      
Begin      

If Not Exists (Select 1 from ClientCommentsForReport where id = @id and InterviewId = @InterviewId)    
Begin

Insert into ClientCommentsForReport(Comment, InterviewId, CreatedBy, CreatedAt, Rating) 
values (@Comment, @InterviewId, @CreatedBy, @timeNow, @Rating)  

End

else
Begin

update ClientCommentsForReport set 
Comment = @Comment,
ModifiedAt = @timeNow,
Rating = @Rating
where id = @id 

End

End 
GO
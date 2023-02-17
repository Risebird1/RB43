/****** Object:  StoredProcedure [dbo].[usp_GetuniqueCandidateByEmailId]  ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetuniqueCandidateByEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetuniqueCandidateByEmailId
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE[dbo].[usp_GetuniqueCandidateByEmailId]

@emailId varchar(100),    
@uniqueid varchar(50)


AS
Begin        
      
select ul.id from UserList ul   
where ul.EmailId = @emailId  and  ul.uniqueid=@uniqueid  and  ul.idProfile=23 
      
End 
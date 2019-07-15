USE [productDB]
GO

/****** Object:  StoredProcedure [dbo].[usp_AddMeeting]    Script Date: 12/07/2019 12:16:04 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddSkillLevel]

@skillLevelName varchar(100),
@skillLevelDescr varchar(200)

AS
Begin

INSERT INTO [dbo].[Level]
           ([Name]
           ,[Description]
		   )
       VALUES
           (@skillLevelName
		   ,@skillLevelDescr)
End
GO



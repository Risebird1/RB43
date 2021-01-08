IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_FetchInterviewerList'))
BEGIN
    DROP PROCEDURE [dbo].[Usp_FetchInterviewerList]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure Usp_FetchInterviewerList
@term varchar(50)
AS
BEGIN
Select Emailid from  Userlist where Emailid like @term+'%' and idProfile=3
END


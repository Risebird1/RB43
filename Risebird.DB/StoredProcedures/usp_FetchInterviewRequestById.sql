IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchInterviewRequestById'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewRequestById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_FetchInterviewRequestById]
    @id INT
AS
BEGIN
    SELECT ir.*, ul.EmailId AS Assignto, IRF.CalendlyLink, ul.uniqueId
    FROM InterviewRequests ir
    LEFT JOIN UserList ul ON ul.id = ir.Assignto
    LEFT JOIN IRF ON IRF.IRFId = ir.IRFID
    WHERE ir.id = @id
END
GO

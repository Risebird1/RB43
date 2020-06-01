/****** Object:  StoredProcedure [dbo].[usp_GetPhrases]    Script Date: 06-04-2020 9.31.35 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPhrases'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPhrases
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPhrases]

AS
Begin

select [text] from Phrase
End


GO



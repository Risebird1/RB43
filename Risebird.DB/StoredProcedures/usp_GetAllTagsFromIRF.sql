/****** Object:  StoredProcedure [dbo].[usp_GetCandidateAudios]    Script Date: 02-02-2021 5.20.49 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllTagsFromIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllTagsFromIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetAllTagsFromIRF]


AS
Begin

select distinct Tag from IRF where Tag is not null


End


GO



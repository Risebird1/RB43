/****** Object:  StoredProcedure [dbo].[usp_GetCandidatesCount]    Script Date: 30-07-2020 8.07.46 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidatesCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidatesCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetCandidatesCount]  
AS  
Begin 

select count(id) as TotalCandidates from CandidatesMaster




End

GO



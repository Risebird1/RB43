IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewCandidateDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewCandidateDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetInterviewCandidateDetails]        
@candidateEmail varchar(200)    
AS        
Begin       
Select *,Address From CandidateVerification where email=@candidateEmail    
End
GO
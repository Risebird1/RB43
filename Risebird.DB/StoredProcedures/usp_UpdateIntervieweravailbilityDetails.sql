IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateIntervieweravailbilityDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateIntervieweravailbilityDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateIntervieweravailbilityDetails]        
@IRFId int,
@timeNow datetime
AS        
Begin        
Update IRF Set SendIntervieweravailbilityOn=@timeNow where id= @IRFId       
End 
Go

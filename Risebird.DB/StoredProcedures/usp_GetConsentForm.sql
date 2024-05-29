IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetConsentForm'))
BEGIN
    DROP PROCEDURE dbo.usp_GetConsentForm
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetConsentForm
@irfid int
AS
BEGIN
Select FormURL From ConsentForms where IRFId=irfid
END
GO
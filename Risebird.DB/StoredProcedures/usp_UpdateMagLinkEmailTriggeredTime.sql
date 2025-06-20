IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateMagLinkEmailTriggeredTime'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateMagLinkEmailTriggeredTime
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_UpdateMagLinkEmailTriggeredTime
@agId int,
@timeNow datetime,
@MAGLinkEmailTriggeredBy varchar(150)
AS
BEGIN

Update MasterAssessmentGroup set MAGLinkEmailTriggeredBy = @MAGLinkEmailTriggeredBy, MAGLinkEmailTriggeredOn = @timeNow
where AssessmentGroupId= @agId


END
Go



IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ApproveDMByClient'))
BEGIN
    DROP PROCEDURE dbo.usp_ApproveDMByClient
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_ApproveDMByClient
@irfId int,
@emailid varchar(150)
as
BEGIN
Update IRF set DMApproved=1,DMApprovedBy=@emailid where id=@irfId
END
Go
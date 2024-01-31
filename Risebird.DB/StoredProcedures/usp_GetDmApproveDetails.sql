IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetDmApproveDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDmApproveDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetDmApproveDetails
AS
begin
select irf.id,irf.IRFId,irf.JDTiltle,ul.EmailId as ChannelOwner,irf.DMApproved,c.Name as ClientName,irf.DMApprovedBy from IRF
join Client c on c.id=irf.ClientId
join UserList ul on ul.id= irf.RisebirdHr where DMApproved=1
End
GO
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetDmApproveDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDmApproveDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetDmApproveDetails
@EmailId varchar(200)
AS
begin
Declare @Role varchar(150)              
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid 
if(@Role='SeniorHR')
Begin
select irf.id,irf.IRFId,irf.JDTiltle,ul.EmailId as ChannelOwner,irf.DMApproved,c.Name as ClientName,irf.DMApprovedBy from IRF
join Client c on c.id=irf.ClientId
join UserList ul on ul.id= irf.RisebirdHr where DMApproved=1 And ul.EmailId=@EmailId
End
ELSE
BEGIN
select irf.id,irf.IRFId,irf.JDTiltle,ul.EmailId as ChannelOwner,irf.DMApproved,c.Name as ClientName,irf.DMApprovedBy from IRF
join Client c on c.id=irf.ClientId
join UserList ul on ul.id= irf.RisebirdHr where DMApproved=1
End
END
GO
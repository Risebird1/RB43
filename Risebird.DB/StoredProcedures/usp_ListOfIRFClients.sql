/****** Object:  StoredProcedure [dbo].[usp_ListOfIRFClients]    ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ListOfIRFClients'))
BEGIN
    DROP PROCEDURE dbo.usp_ListOfIRFClients
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_ListOfIRFClients]
@IRFID varchar(200) = null,
@interviewId int = null
AS
Begin
if(@IRFID is not null)
begin
	select distinct ul.EmailId 
	from TransClientUser tcu join UserList ul on ul.id = tcu.ClientUserId
	inner join ClientUser cu on cu.UserId = ul.id
	inner join ClientRole cr on cr.id = cu.RoleId
	inner join IRF as irf on  irf.id = tcu.IRFId
	where irf.IRFId like @IRFID and cr.id in (1, 4)--client hr and staffinng agency
end
else if(@interviewId is not null)
begin

declare @irfIdVariable int

select @irfIdVariable =irf.id from InterviewMeetings im join irf on irf.IRFId = im.IRFId where im.id = @interviewId

	select distinct ul.EmailId 
	from TransClientUser tcu join UserList ul on ul.id = tcu.ClientUserId
	inner join ClientUser cu on cu.UserId = ul.id
	inner join ClientRole cr on cr.id = cu.RoleId
	inner join IRF as irf on  irf.id = tcu.IRFId
	where irf.IRFId like @irfIdVariable and cr.id in (1, 4)--client hr and staffinng agency
end


End



GO



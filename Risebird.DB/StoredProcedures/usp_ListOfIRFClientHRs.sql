/****** Object:  StoredProcedure [dbo].[usp_ListOfIRFClientHRs]    Script Date: 19-01-2021 8.02.43 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ListOfIRFClientHRs'))
BEGIN
    DROP PROCEDURE dbo.usp_ListOfIRFClientHRs
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_ListOfIRFClientHRs]
@IRFID varchar(200)
AS
Begin

 select a.Emailid  from (select TransClientUser.*, UserList.EmailId from TransClientUser inner join UserList on TransClientUser.ClientUserId = UserList.id) as a 
inner join ClientRole on ClientRole.id =a.ClientRoleId
inner join IRF as irf on  irf.id = a.IRFId
where irf.IRFId like @IRFID and ClientRole.id =1
End



GO



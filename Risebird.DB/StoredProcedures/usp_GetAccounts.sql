/****** Object:  StoredProcedure [dbo].[usp_GetAccounts]    Script Date: 24-03-2020 8.20.45 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAccounts'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAccounts
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetAccounts]  
@EmailId varchar(200),   
@Profile varchar(100)
AS  
Begin 


IF (@Profile = 'Client')  
	BEGIN  
		declare
		@ClientId int = (select ClientId from  ClientUser cu left join UserList ul on cu.UserId = ul.id  where ul.EmailId = @EmailId)

		select Account.*, a.TotalProjects, cl.[Name] as Client, 1 as IsClientUser from Account 
		left join Client cl on cl.id = Account.ClientId
		left join (select Count(AccountId) as TotalProjects, AccountId from AccountProjectMapping group by AccountId) a on Account.id=a.AccountId
		where Account.ClientId=@ClientId
	End  

Else
begin
		select Account.*, a.TotalProjects, cl.[Name] as Client from Account 
		left join Client cl on cl.id = Account.ClientId
		left join (select Count(AccountId) as TotalProjects, AccountId from AccountProjectMapping group by AccountId) a on Account.id=a.AccountId

End
End
GO



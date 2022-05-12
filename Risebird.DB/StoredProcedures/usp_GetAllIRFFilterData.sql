

/****** Object:  StoredProcedure [dbo].[usp_GetAllIRF]    Script Date: 20-03-2020 12.16.55 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllIRFFilterData'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllIRFFilterData
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetAllIRFFilterData]         
@EmailId varchar(200),           
@Profile varchar(100),        
@timeNow datetime
AS          
Begin         
    
IF (@Profile = 'Client')          
 BEGIN          
  declare @roleId int,  @ClientId int   
  set @roleId = (select RoleId from  ClientUser cu  join UserList ul on cu.UserId = ul.id  where ul.EmailId = @EmailId)  
  set @ClientId = (select ClientId from  ClientUser cu left join UserList ul on cu.UserId = ul.id  where ul.EmailId = @EmailId)        
   
  if(@roleId=1)  
  begin  
    
	select irf.IRFId, TRIM(irf.ClientName) as EndClientName, cl.[Name] as ClientCompanyName,
	CASE
	when charindex('|', irf.JDTiltle) != 0 then TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1))
	else irf.JDTiltle
	END as PrimarySkill
	from IRF 
	left join Client cl on cl.id = irf.ClientId
	join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id 
	left join [dbo].[UserList] ul on ul.id = tc.ClientUserId 
	left join ClientUser cu on tc.ClientUserId = cu.UserId 
	where ul.EmailId=@EmailId and cu.ClientId=@ClientId 
    
  
  end  
  else  
  begin 
	 
	select irf.IRFId, TRIM(irf.ClientName) as EndClientName, cl.[Name] as ClientCompanyName, 
	CASE
	when charindex('|', irf.JDTiltle) != 0 then TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1))
	else irf.JDTiltle
	END as PrimarySkill
	from IRF 
	left join Client cl on cl.id = irf.ClientId
	left join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id 
	left join [dbo].[UserList] ul on ul.id = tc.ClientUserId 
	left join ClientUser cu on tc.ClientUserId = cu.UserId 
	where ul.EmailId=@EmailId and cu.ClientId=@ClientId
	
   
  end  
         
 End          
Else if(@Profile='Recruiter')        
 Begin   
 
	select irf.IRFId, TRIM(irf.ClientName) as EndClientName, cl.[Name] as ClientCompanyName,
	CASE
	when charindex('|', irf.JDTiltle) != 0 then TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1))
	else irf.JDTiltle
	END as PrimarySkill 
	from IRF 
	left join Client cl on cl.id = irf.ClientId
	left join [dbo].[TransRecruiter] tr on tr.IRFId = IRF.id 
    left join [dbo].[UserList] ul on ul.id = tr.UserId     
	where ul.EmailId=@EmailId
   
 End        
Else if(@Profile='CuratedUser')        
 Begin  

	select irf.IRFId, TRIM(irf.ClientName) as EndClientName, Client.[Name] as ClientCompanyName,
	CASE
	when charindex('|', irf.JDTiltle) != 0 then TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1))
	else irf.JDTiltle
	END as PrimarySkill 
	from IRF 
	left join Client on IRF.ClientId = Client.id
	left join UserList ulh on ulh.id = irf.RisebirdHr      
	where Client.CuratedLink=1
	 
 End       
Else if(@Profile='HR')        
 Begin 

	select irf.IRFId, TRIM(irf.ClientName) as EndClientName, Client.[Name] as ClientCompanyName, 
	CASE
	when charindex('|', irf.JDTiltle) != 0 then TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1))
	else irf.JDTiltle
	END as PrimarySkill 
	from IRF 
	inner join Client on IRF.ClientId = Client.id
	join TransHR th on th.IRFId = irf.id      
	join HR on HR.id = th.HRId      
	join UserList ulh on ulh.id = HR.UserId       
	left join Userlist ul on ul.id=irf.RisebirdHr    
	where ulh.EmailId=@EmailId
	union
	select irf.IRFId, TRIM(irf.ClientName) as EndClientName, Client.[Name] as ClientCompanyName,
	CASE
	when charindex('|', irf.JDTiltle) != 0 then TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1))
	else irf.JDTiltle
	END as PrimarySkill 
	from IRF 
	left join Client on IRF.ClientId = Client.id
	join TransHR th on th.IRFId = irf.id      
	join HR on HR.id = th.HRId      
	join UserList ulh on ulh.id = HR.UserId       
	left join Userlist ul on ul.id=irf.RisebirdHr    
	where ulh.EmailId=@EmailId 
	

 End  
Else if(@Profile='SeniorHR')        
 Begin        
     
	select irf.IRFId, TRIM(irf.ClientName) as EndClientName, Client.[Name] as ClientCompanyName,
	CASE
	when charindex('|', irf.JDTiltle) != 0 then TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1))
	else irf.JDTiltle
	END as PrimarySkill 
	from IRF 
	inner join Client on IRF.ClientId = Client.id
	join SeniorHR sh on sh.Clientid=Client.id  
	left join Userlist ul on ul.id=sh.Userid     
	where ul.EmailId=@EmailId
	
	   
 End  
   
Else        
begin

	select irf.IRFId, TRIM(irf.ClientName) as EndClientName, Client.[Name] as ClientCompanyName,
	CASE
	when charindex('|', irf.JDTiltle) != 0 then TRIM(SUBSTRING(irf.JDTiltle, 1, charindex('|', irf.JDTiltle) - 1))
	else irf.JDTiltle
	END as PrimarySkill
	from IRF 
	inner join Client on IRF.ClientId = Client.id 
	
End        
End  
GO



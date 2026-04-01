/****** Object:  StoredProcedure [dbo].[usp_GetCandidatesResumes]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidatesResumes'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidatesResumes
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetCandidatesResumes]    
@IRFId int,
@emailid varchar(200)
AS    
Begin  
Declare @ClientRole int = 0
if(@emailid != '')
Begin
Select @ClientRole=RoleId From ClientUser cu join UserList ul on ul.id=cu.UserId where ul.EmailId=@emailid

End

if(@ClientRole=1 or @ClientRole=4)
begin
select cr.*, ul.Emailid as AddedByEmailId, cr.SupportTicketId
from CandidateResume cr 
join UserList ul on cr.AddedBy=ul.id 
where cr.IRFId=@IRFId and ul.Emailid=@emailid 
order by AddedOn desc  
end
else
begin
select cr.*, ul.Emailid as AddedByEmailId, cr.SupportTicketId
from CandidateResume cr 
join UserList ul on cr.AddedBy=ul.id 
where cr.IRFId=@IRFId 
order by AddedOn desc  
end
End  
  

GO
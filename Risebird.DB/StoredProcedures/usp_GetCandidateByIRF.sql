/****** Object:  StoredProcedure [dbo].[usp_GetIRFCandidates]    Script Date: 10-06-2020 9.24.16 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateByIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateByIRF
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_GetCandidateByIRF]

@IRFId int,
@EmailId varchar(100)

AS
Begin  
  
select ca.id, ca.CandidateMasterId, ca.IRFId, ca.ClientHRAttemptContact, ca.TotalInvites, ca.ProfileSource, 
case 
	 when ul.LastName is null then ul.FirstName
	 else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))
end as [Name],
ul.MobileNumber as Phone
from Candidate ca   
join CandidatesMaster cm on cm.id=ca.CandidateMasterId   
join UserList ul on ul.id = cm.UserId  
where ca.IRFId=@IRFId and ul.EmailId=@EmailId  
  
  
End


GO



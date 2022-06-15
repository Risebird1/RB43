IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchCallCatalogueInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchCallCatalogueInterviewer
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_FetchCallCatalogueInterviewer          
AS          
BEGIN          
Select cd.*,crd.WhoCalled from (Select CTE.CandidateEmail,CTE.CandidateName,Max(CTE.CalledOn) as CalledOn,Max(CTE.Callerid) as Callerid from (          
Select crd.Interviewer as CandidateEmail,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName,Max(crd.CalledOn) as CalledOn,Max(crd.id) as Callerid          
from CallRecordingInterviewer crd 
Left join userlist ul on ul.EmailId=crd.Interviewer          
group by crd.Interviewer,(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,''))) as CTE          
group by CTE.CandidateEmail,CTE.CandidateName) as Cd join CallRecordingInterviewer crd on cd.Callerid=crd.id          
order by CalledOn desc          
       
END          
GO        
      
      
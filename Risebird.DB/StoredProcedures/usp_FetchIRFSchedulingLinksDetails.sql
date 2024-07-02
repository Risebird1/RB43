IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchIRFSchedulingLinksDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchIRFSchedulingLinksDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchIRFSchedulingLinksDetails  
@Emailid varchar(150)  
AS  
BEGIN  
Select IRF.IRFId,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,CalendlyLink,uli.EmailId as BillingOwner,JDLink,IRF.id,IRF.IsPaused, cl.ShowInterviewInstructions,IRF.IsDrive,  
Case When cm.id is null then 0 else 1 end as CalibrationMeetingdone,IRF.DMApproved  
from IRF join TransClientUser tcu join Userlist ul on ul.id=tcu.ClientUserid on tcu.IRFId=IRF.id   
left join UserList uli on uli.id=BillingOwner   
join Client cl on cl.id = irf.ClientId   
left join CalibrationMeetings cm on cm.Irfid=IRF.id and (cm.VideoLink is not null or cm.VideoLink!='')  
Where ul.Emailid=@Emailid and IsDeleted=0 and EndDate>DateAdd(mi,330,GETDATE()) order by IRF.CreatedOn desc  
END  
GO
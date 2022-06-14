IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIRFCandidateStatsIRFCount'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFCandidateStatsIRFCount
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO      
                    
CREATE PROCEDURE usp_GetIRFCandidateStatsIRFCount  
@startDate datetime,  
@endDate datetime,          
@timeNow datetime,
@Emailid varchar(150)
AS  
BEGIN  
DECLARE @tbl_Temp TABLE (            
    IRFId INT              
);            
INSERT INTO @tbl_Temp            
select distinct IRFId from CandidateStatus where [Status] =12 and cast([Date] as date) <= cast(@endDate as date) and cast([Date] as date) >= cast(@startDate as date)         
      
select irf.id, irf.IRFId, irf.JDTiltle, cl.[Name] as ClientCompName, irf.JobCountry from IRF        
 join @tbl_Temp tmp on tmp.IRFId =IRF.id        
 join Client cl on cl.id = irf.ClientId        
 join UserList ul on ul.id = irf.RisebirdHr        
 Where ul.EmailId=@Emailid

END          
GO            
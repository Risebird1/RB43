
IF EXISTS (SELECT * FROM sys.objects WHERE  object_id = OBJECT_ID(N'[dbo].[fn_CountFLNConversionData]')
                      AND type IN ( N'TF' ))
BEGIN
    DROP FUNCTION dbo.fn_CountFLNConversionData
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create FUNCTION fn_CountFLNConversionData(@emailId varchar(200), @timeNow datetime, @startDate datetime=null, @endDate datetime =null)      
RETURNS @ResultTable TABLE (TotalProfilesAdded int, TotalAcknowledged int)   
AS      
BEGIN      
     
DECLARE @TempTable TABLE (            
  interviewerEmail varchar(200),      
  IsActive bit   
);     
      
INSERT INTO @TempTable 
select distinct pv.EmailId, ul.IsActive from PreOnboardingInterviewerAIVerdict pv
left join UserList ul on ul.EmailId = pv.EmailId
where CreatedBy = @emailId
and ((@startDate is not null and @endDate is not null and cast(CreatedAt as date) >= cast(@startDate as date) and cast(CreatedAt as date) <= cast(@endDate as date))
or (@startDate is null))--(cast(CreatedAt as date) > cast(DATEADD(DAY, -30, @timeNow) as date)
      
declare @totalProfilesAdded int, @totalAcknowledged int    
   
set @totalProfilesAdded = (select count(interviewerEmail) from @TempTable)
set @totalAcknowledged = (select count(interviewerEmail) from @TempTable where IsActive = 1)

INSERT INTO @ResultTable (TotalProfilesAdded, TotalAcknowledged) values (@totalProfilesAdded, @totalAcknowledged)
      
return    
    
END      
GO


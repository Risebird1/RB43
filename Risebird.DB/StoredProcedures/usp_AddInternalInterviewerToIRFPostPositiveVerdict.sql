IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInternalInterviewerToIRFPostPositiveVerdict'))
BEGIN
    DROP PROCEDURE dbo.usp_AddInternalInterviewerToIRFPostPositiveVerdict
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddInternalInterviewerToIRFPostPositiveVerdict]    

@IRFId int,
@ResumeLink varchar(max),  
@EmailId varchar(100),
@Phone varchar(20),
@ReferredBy varchar(200)=null,
@OpenAIScreeningResponse varchar(max),
@verdict varchar(20),
@score int,
@timeNow datetime
 
AS    
Begin  

begin try
	begin tran
	if exists(select top 1 id from UserList where EmailId = @EmailId)
	Begin
		declare @userId int = (select top 1 id from UserList where EmailId = @EmailId)
		declare @rate int, @InterviewerProfileId int

		select @InterviewerProfileId=(select id from InterviewerProfile where UserId= @userId) 
		set @rate =   (select Rate from PlanTypeChargesMaster where CountryType = (select JobCountry from IRF where id = @IRFId) 
				and PlanTypeEnumValue = (select Heuristicsscore from IRF where id = @IRFId)
				and CompanyType = (select CompanyType from IRF where id = @IRFId))

		if not exists(select 1 from TransInterviewer where UserId = @userId and IRFId = @IRFId)
		Begin
			insert into TransInterviewer(IRFId, InterviewerId, UserId, IsAvailable, IsActive, PerSuccessRate, OpenAIScreeningResponse, AIVerdict, Source, AIScreeningScore, IsAIScreeningAtempted)
			values (@IRFId, @InterviewerProfileId, @userId, 1, 1, @rate, @OpenAIScreeningResponse, @verdict, 'Internal IRF Link', @score, 1)
		End
	End
	

	commit tran
end try
begin catch
	rollback transaction    
	DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),    
	@Severity int = ERROR_SEVERITY(),    
	@State smallint = ERROR_STATE()    
	RAISERROR (@Message, @Severity, @State)
end catch



End
GO


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
@timeNow datetime,
@agId int,
@isMagApplication bit
 
AS    
Begin  

begin try
	begin tran
	if(@isMagApplication = 0)
	Begin
		if exists(select top 1 id from UserList where EmailId = @EmailId)
		Begin
			declare @userId int = (select top 1 id from UserList where EmailId = @EmailId)
			declare @rate int, @InterviewerProfileId int, @assessmentGroupId varchar(100)
			set @assessmentGroupId = (select AssessmentGroupId from IRF where id =@IRFId)
			select @InterviewerProfileId=(select id from InterviewerProfile where UserId= @userId) 
			set @rate =   (select Rate from PlanTypeChargesMaster where CountryType = (select JobCountry from IRF where id = @IRFId) 
					and PlanTypeEnumValue = (select Heuristicsscore from IRF where id = @IRFId)
					and CompanyType = (select CompanyType from IRF where id = @IRFId))

			DECLARE @isLocked bit = ISNULL((SELECT DmIvLockNew FROM IRF WHERE id = @IRFId), 0)

			if not exists(select 1 from TransInterviewer where UserId = @userId and IRFId = @IRFId)
			Begin
				-- Self-application via the direct IRF link adds the channel as AVAILABLE ONLY
				-- (IsAvailable = 1) but NOT active (IsActive = 0); an operator must manually review
				-- and activate it, so ActivatedAt stays NULL. Keeps self-applicants out of the
				-- 'active' panel until that manual review step.
				insert into TransInterviewer(IRFId, InterviewerId, UserId, IsAvailable, IsActive, IsSMEVerified, ClientAction, PerSuccessRate, OpenAIScreeningResponse, AIVerdict, Source, AIScreeningScore, IsAIScreeningAtempted, ActivatedAt)
				values (@IRFId, @InterviewerProfileId, @userId, 1, 0,
			 CASE WHEN @isLocked = 1 THEN 0 ELSE 1 END,
			 CASE WHEN @isLocked = 1 THEN NULL ELSE 'Shortlisted' END,
			 @rate, @OpenAIScreeningResponse, @verdict, 'Internal IRF Link', @score, 1, NULL)
			End

			UPDATE InterviewerProfile
			SET ResumeLink = @ResumeLink
			WHERE userId = @userId

			DECLARE @existingList VARCHAR(MAX);

		-- Get current comma-separated string
		SELECT @existingList = AssessmentGroupId
		FROM InterviewerProfile
		WHERE userId = @userId;

		-- If @assessmentGroupId is NOT in the list
		IF NOT EXISTS (
			SELECT 1
			FROM STRING_SPLIT(@existingList, ',')
			WHERE TRIM(value) = @assessmentGroupId
		)
		BEGIN
			-- Handle null or empty list
			IF @existingList IS NULL OR LTRIM(RTRIM(@existingList)) = ''
			BEGIN
				SET @existingList = @assessmentGroupId;
			END
			ELSE
			BEGIN
				SET @existingList = @existingList + ',' + @assessmentGroupId;
			END

			-- Update the table
			UPDATE InterviewerProfile
			SET AssessmentGroupId = @existingList
			WHERE userId = @userId;
		END

		End
	End
	else
	Begin
		
		if exists(select top 1 id from UserList where EmailId = @EmailId)
		Begin
			declare @userId2 int = (select top 1 id from UserList where EmailId = @EmailId)
			declare @rate2 int, @InterviewerProfileId2 int
			select @InterviewerProfileId2=(select id from InterviewerProfile where UserId= @userId2) 

			
			declare @tbl_targetIRFs table (irfId int)

			insert into @tbl_targetIRFs
			select distinct irf.id  from IRF 
		    where AssessmentGroupId = @agId and (IsPaused is null or IsPaused = 0)

			while exists (select top 1 irfId from @tbl_targetIRFs)
			Begin

				declare @targetIRFId int
				set @targetIRFId = (select top 1 irfId from @tbl_targetIRFs)

				set @rate2 =   (select Rate from PlanTypeChargesMaster where CountryType = (select JobCountry from IRF where id = @targetIRFId) 
					and PlanTypeEnumValue = (select Heuristicsscore from IRF where id = @targetIRFId)
					and CompanyType = (select CompanyType from IRF where id = @targetIRFId))

				DECLARE @isLocked2 bit = ISNULL((SELECT DmIvLockNew FROM IRF WHERE id = @targetIRFId), 0)

				if not exists(select 1 from TransInterviewer where UserId = @userId2 and IRFId = @targetIRFId)
				Begin
					-- Self-registration via the MAG application link adds the interviewer to each
					-- channel as AVAILABLE ONLY (IsAvailable = 1) but NOT active (IsActive = 0).
					-- An operator must manually review and activate each channel, so ActivatedAt
					-- stays NULL. This prevents MAG applicants from appearing 'active' in the panel
					-- before that manual review / acknowledgement step.
					insert into TransInterviewer(IRFId, InterviewerId, UserId, IsAvailable, IsActive, IsSMEVerified, ClientAction, PerSuccessRate, OpenAIScreeningResponse, AIVerdict, Source, AIScreeningScore, IsAIScreeningAtempted, ActivatedAt)
					values (@targetIRFId, @InterviewerProfileId2, @userId2, 1, 0,
				 CASE WHEN @isLocked2 = 1 THEN 0 ELSE 1 END,
				 CASE WHEN @isLocked2 = 1 THEN NULL ELSE 'Shortlisted' END,
				 @rate2, @OpenAIScreeningResponse, @verdict, 'Internal MAG Link', @score, 1, NULL)
				End

				delete from @tbl_targetIRFs where irfId = @targetIRFId
			End

			
			

			UPDATE InterviewerProfile
			SET ResumeLink = @ResumeLink
			WHERE userId = @userId2

			DECLARE @existingList2 VARCHAR(MAX);

			-- Get current comma-separated string
			SELECT @existingList2 = AssessmentGroupId
			FROM InterviewerProfile
			WHERE userId = @userId2;

			-- If @assessmentGroupId is NOT in the list
			IF NOT EXISTS (
				SELECT 1
				FROM STRING_SPLIT(@existingList2, ',')
				WHERE TRIM(value) = @agId
			)
			BEGIN
				-- Handle null or empty list
				IF @existingList2 IS NULL OR LTRIM(RTRIM(@existingList2)) = ''
				BEGIN
					SET @existingList2 = @agId;
				END
				ELSE
				BEGIN
					SET @existingList = @existingList + ',' + @agId;
				END

				-- Update the table
				UPDATE InterviewerProfile
				SET AssessmentGroupId = @existingList2
				WHERE userId = @userId2;
			END

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


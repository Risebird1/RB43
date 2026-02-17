IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveInterviewerPreOnboardingVerdictV2'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveInterviewerPreOnboardingVerdictV2
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveInterviewerPreOnboardingVerdictV2]     
@EmailId [varchar](50),
@PrimarySpecialization [varchar](250),
--@screeningResponseJson varchar(max),
@completeFitmentAnalysis varchar(max) = null,
@fitmentScore int = null,
@verdict varchar(25) = null,
@PerformedBy varchar(200),
@timeNow datetime,
@result bit out
As
Begin
	SET NOCOUNT ON;
	
	DECLARE @RowsAffected INT = 0
	DECLARE @RecordExists BIT = 0
	
	BEGIN TRY
		select @result = 0
		
		DECLARE @ExistingSpecialization VARCHAR(250)

		-- Check if email already exists (regardless of specialization)
		SELECT @ExistingSpecialization = PrimarySpecialization 
		FROM PreOnboardingInterviewerAIVerdict 
		WHERE EmailId = @EmailId

		-- If email exists, verify the specialization matches
		IF @ExistingSpecialization IS NOT NULL
		Begin
			-- Email exists - check if specialization matches
			IF @ExistingSpecialization <> @PrimarySpecialization
			Begin
				-- Email exists with different specialization - not allowed
				DECLARE @ErrorMsg NVARCHAR(500) = 
					'Email ' + @EmailId + ' already exists with specialization ''' + 
					@ExistingSpecialization + '''. Cannot change to ''' + @PrimarySpecialization + 
					'''. Email and PrimarySpecialization cannot be modified once set.'
				RAISERROR(@ErrorMsg, 16, 1)
			End
			Else
			Begin
				-- Email exists with same specialization - allow update of other fields
				SET @RecordExists = 1
				
				-- Update existing record (only updatable fields)
				update PreOnboardingInterviewerAIVerdict set OpenAIResponse = @completeFitmentAnalysis, AIVerdict =@verdict,
				ModifiedBy =@PerformedBy, ModifiedAt =@timeNow, AIScreeningScore = @fitmentScore 
				where EmailId = @EmailId

				SET @RowsAffected = @@ROWCOUNT
				
				IF @RowsAffected > 0
					select @result = 1
				ELSE
					-- Debug: Update attempted but no rows affected
					RAISERROR('UPDATE executed but @@ROWCOUNT = 0', 16, 1)
			End
		End
		Else
		Begin
			-- Email doesn't exist - allow insert
			SET @RecordExists = 0
			
			-- Validate required parameters before INSERT
			IF @EmailId IS NULL OR @EmailId = ''
				RAISERROR('EmailId is NULL or empty', 16, 1)
			IF @PrimarySpecialization IS NULL OR @PrimarySpecialization = ''
				RAISERROR('PrimarySpecialization is NULL or empty', 16, 1)
			IF @PerformedBy IS NULL OR @PerformedBy = ''
				RAISERROR('PerformedBy (CreatedBy) is NULL or empty', 16, 1)
			
			-- Insert new record
			insert into PreOnboardingInterviewerAIVerdict(EmailId, PrimarySpecialization, OpenAIResponse, AIVerdict, CreatedAt, CreatedBy, AIScreeningScore)
			values (@EmailId, @PrimarySpecialization, @completeFitmentAnalysis, @verdict, @timeNow, @PerformedBy, @fitmentScore)

			SET @RowsAffected = @@ROWCOUNT
			
			IF @RowsAffected > 0
				select @result = 1
			ELSE
				-- Debug: Insert attempted but no rows affected
				RAISERROR('INSERT executed but @@ROWCOUNT = 0', 16, 1)
		End
	END TRY
	BEGIN CATCH
		-- Log error and set result to 0
		select @result = 0
		
		-- Re-throw the error with detailed information
		DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE()
		DECLARE @ErrorSeverity INT = ERROR_SEVERITY()
		DECLARE @ErrorState INT = ERROR_STATE()
		DECLARE @ErrorLine INT = ERROR_LINE()
		
		DECLARE @FullErrorMessage NVARCHAR(4000) = 
			'Error in usp_SaveInterviewerPreOnboardingVerdictV2: ' + @ErrorMessage + 
			' | Line: ' + CAST(@ErrorLine AS VARCHAR(10)) +
			' | RecordExists: ' + CAST(@RecordExists AS VARCHAR(1)) +
			' | RowsAffected: ' + CAST(@RowsAffected AS VARCHAR(10)) +
			' | EmailId: ' + ISNULL(@EmailId, 'NULL') +
			' | PerformedBy: ' + ISNULL(@PerformedBy, 'NULL')
		
		RAISERROR(@FullErrorMessage, @ErrorSeverity, @ErrorState)
	END CATCH
End
GO
/* ===========================================================================================
   ForceAddNaukriProspectsAsInterviewers_TBM
   -------------------------------------------------------------------------------------------
   Bypasses the Naukri onboarding funnel (EV / AI fitment / acknowledgement) and directly
   creates the two prospects below as ACTIVE interviewers under the MAG "Technology Business
   Management (TBM)", by calling the SAME stored procedure the app uses
   (usp_AddOrEditInterviewerProfile). That SP:
       • inserts UserList (idProfile = 3, IsInterviewerAccess = 1, uniqueid),
       • inserts InterviewerProfile (with the AG id on it),
       • adds AssessmentGroupInterviewers membership (via @AssessmentGroupDifference),
       • maps the interviewer to ALL active TBM skill channels (MAG-direct branch), where each
         channel's IsActive follows its Interviewer Lock (IRF.DmIvLockNew): unlocked → active,
         locked → available-only pending manual RB vetting.

   Prospects:
       1) jpgfication@gmail.com   / 9724755847   (Naukri ticket #17071)
       2) varun.gupta091@gmail.com / 9654594842  (Varun Gupta — no ticket)

   IMPORTANT — portal login: usp_AddOrEditInterviewerProfile does NOT create the Azure AD B2C
   identity that the app's C# service normally provisions for login. These accounts will be fully
   usable INSIDE the system (appear in the TBM interviewer list, assignable/schedulable), but the
   person cannot log in to the portal until a B2C account exists for their email (normal sign-up /
   password reset). The stored UserPassword below is only a placeholder.

   SAFETY: dry-run by default (@Commit = 0 → does everything in a transaction, prints results, then
   ROLLS BACK). Review the output, then set @Commit = 1 to persist. Skips any email that already
   has a UserList row so it never creates duplicates.
   =========================================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit  BIT         = 0;                                   -- <<< set to 1 to persist
DECLARE @Actor   VARCHAR(50) = 'DataFix-ForceAddIv';
DECLARE @MagName VARCHAR(200) = 'Technology Business Management (TBM)';

-------------------------------------------------------------------------------------------------
-- Resolve the TBM assessment group (tolerant match: MAG name first, then a direct AG name).
-------------------------------------------------------------------------------------------------
DECLARE @AgId INT;
SELECT TOP 1 @AgId = x.AssessmentGroupId
FROM (
    SELECT 1 AS Pref, mag.AssessmentGroupId
    FROM MasterAssessmentGroup mag
    WHERE dbo.fn_NormalizeMagName(mag.MAGName) = dbo.fn_NormalizeMagName(@MagName)
      AND ISNULL(mag.IsDeleted, 0) = 0
    UNION ALL
    SELECT 2, ag.id
    FROM AssessmentGroup ag
    WHERE dbo.fn_NormalizeMagName(ag.AssessmentGroup) = dbo.fn_NormalizeMagName(@MagName)
) x
WHERE x.AssessmentGroupId IS NOT NULL
ORDER BY x.Pref;

IF @AgId IS NULL
BEGIN
    RAISERROR('Assessment group for MAG "%s" was not found. Aborting.', 16, 1, @MagName);
    RETURN;
END
PRINT CONCAT('Resolved MAG "', @MagName, '" -> AssessmentGroupId ', @AgId);

-------------------------------------------------------------------------------------------------
-- Prospects to force-add
-------------------------------------------------------------------------------------------------
DECLARE @P TABLE (
    RowNo     INT IDENTITY(1,1),
    FirstName VARCHAR(150),
    LastName  VARCHAR(150),
    Email     VARCHAR(150),
    Phone     VARCHAR(20),
    NaukriId  INT NULL
);
INSERT INTO @P (FirstName, LastName, Email, Phone, NaukriId) VALUES
    ('Varun', 'Gupta', 'varun.gupta091@gmail.com', '9654594842', NULL),
    ('Jpg',   'Candidate', 'jpgfication@gmail.com',  '9724755847', 17071);

-- If ticket #17071 has a real candidate name, use it for the first/last name split.
DECLARE @Cand VARCHAR(200);
SELECT @Cand = LTRIM(RTRIM(CandidateName)) FROM NaukriProspectiveInterviewer WHERE Id = 17071;
IF @Cand IS NOT NULL AND @Cand <> ''
BEGIN
    UPDATE @P
    SET FirstName = CASE WHEN CHARINDEX(' ', @Cand) > 0 THEN LEFT(@Cand, CHARINDEX(' ', @Cand) - 1) ELSE @Cand END,
        LastName  = CASE WHEN CHARINDEX(' ', @Cand) > 0 THEN LTRIM(SUBSTRING(@Cand, CHARINDEX(' ', @Cand) + 1, 200)) ELSE '' END
    WHERE NaukriId = 17071;
END

-------------------------------------------------------------------------------------------------
BEGIN TRAN;

DECLARE @RowNo INT, @First VARCHAR(150), @Last VARCHAR(150), @Email VARCHAR(150),
        @Phone VARCHAR(20), @NaukriId INT,
        @NickName VARCHAR(100), @Pwd NVARCHAR(60),
        @NewProfileId INT, @NewUserId INT, @Uniqueid VARCHAR(50),
        @IsInsert BIT, @AgList VARCHAR(MAX);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT RowNo, FirstName, LastName, Email, Phone, NaukriId FROM @P ORDER BY RowNo;
OPEN cur;
FETCH NEXT FROM cur INTO @RowNo, @First, @Last, @Email, @Phone, @NaukriId;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- Duplicate guard: never create a second account for an email that already exists.
    IF EXISTS (SELECT 1 FROM UserList WHERE LOWER(LTRIM(RTRIM(EmailId))) = LOWER(LTRIM(RTRIM(@Email))))
    BEGIN
        PRINT CONCAT('SKIP: ', @Email, ' already has a UserList account — not creating a duplicate. Map/adopt it manually if needed.');
    END
    ELSE
    BEGIN
        IF LEN(@First) < 2 SET @First = LEFT(@First + 'xx', 2);
        SET @NickName = LEFT(@First, 2) + CASE WHEN LEN(@Last) >= 2 THEN LEFT(@Last, 2) ELSE 'Ri' END + RIGHT('0000' + @Phone, 4);
        SET @Pwd      = UPPER(LEFT(@First, 4)) + '@' + RIGHT('000' + CAST(ABS(CHECKSUM(NEWID())) % 1000 AS VARCHAR(3)), 3);

        SET @NewProfileId = NULL; SET @NewUserId = NULL; SET @Uniqueid = NULL; SET @IsInsert = NULL; SET @AgList = NULL;

        EXEC dbo.usp_AddOrEditInterviewerProfile
            @UserId                                 = NULL,               -- NULL => INSERT (new account)
            @FirstName                              = @First,
            @LastName                               = @Last,
            @NickName                               = @NickName,
            @EmailId                                = @Email,
            @AltEmailId                             = NULL,
            @DOB                                    = '1990-01-01',       -- safe placeholder (> 1753)
            @Gender                                 = 'Male',
            @AnnualSalary                           = NULL,
            @Country                                = '26',               -- India (matches Naukri onboarding)
            @City                                   = NULL,
            @MobileNumber                           = @Phone,
            @AltMobileNumber                        = NULL,
            @CurrentRole                            = NULL,
            @Experience                             = 0,
            @PreferredDomain                        = NULL,
            @GitHubLink                             = NULL,
            @ResumeLink                             = NULL,
            @CodedResumeLink                        = NULL,
            @StackOverflowLink                      = NULL,
            @LinkedinLink                           = NULL,
            @Blog                                   = NULL,
            @IsLeadInterviewer                      = 0,
            @IsActive                               = 1,                  -- directly ACTIVE (bypass acknowledgement)
            @IsInterviewerAccess                    = 1,
            @IsApproved                             = 0,
            @AssessmentGroupId                      = @AgId,              -- drives MAG-direct channel mapping
            @UserPassword                           = @Pwd,
            @WhatsappNumber                         = @Phone,
            @Isinstaavailable                       = 0,
            @InterviewerProfileId                   = @NewProfileId OUTPUT,
            @IndiaServiceRate                       = 0,
            @IndiaProductRate                       = 0,
            @OutsideIndiaServiceRate                = 0,
            @OutsideIndiaProductRate                = 0,
            @isVerified                             = 0,
            @CompanyName                            = NULL,
            @IsEmployed                             = 0,
            @PrimarySpecializationForAcknowledgement = @MagName,
            @loggedInUser                           = @Actor,
            @AssessmentGroupDifference              = @AgId,              -- adds AssessmentGroupInterviewers membership
            @assessmentGroupList                    = @AgList OUTPUT,
            @RemovedAssessmentGroups                = NULL,
            @TargetIrfId                            = 0,                  -- 0 => map ALL active AG channels
            @outputUserId                           = @NewUserId OUTPUT,
            @uniqueid                               = @Uniqueid OUTPUT,
            @IsInsertOperation                      = @IsInsert OUTPUT,
            @IsHoldPayment                          = NULL,
            @HoldPaymentReason                      = NULL;

        IF @AgList IS NOT NULL AND @AgList <> ''
        BEGIN
            PRINT CONCAT('WARN: ', @Email, ' not added — AG capacity reached for: ', @AgList);
        END
        ELSE
        BEGIN
            PRINT CONCAT('ADDED: ', @Email, ' -> UserId ', @NewUserId, ', ProfileId ', @NewProfileId, ' (', @First, ' ', @Last, ').');

            -- If this prospect has a Naukri ticket, reflect the manual onboarding on the funnel.
            IF @NaukriId IS NOT NULL
            BEGIN
                UPDATE NaukriProspectiveInterviewer
                SET OnboardingCompleted      = 1,
                    OnboardedUserId          = @NewUserId,
                    OnboardedProfileId       = @NewProfileId,
                    AssessmentGroupId        = @AgId,
                    OnboardingType           = 'ManuallyOnboarded',
                    FunnelStage              = 'Onboarded',
                    TicketStatus             = 'Closed',
                    Status                   = 'Onboarded',
                    AcknowledgementEmailSent = 1,
                    UpdatedAt                = GETUTCDATE()
                WHERE Id = @NaukriId;

                INSERT INTO NaukriOnboardingHistory (RecordId, EventType, FromValue, ToValue, ChangedBy, ChangedAt)
                VALUES (@NaukriId, 'AutomationNote', N'—',
                        CONCAT(N'Force-added as interviewer (UserId ', @NewUserId, N', Profile ', @NewProfileId,
                               N') under AG ', @AgId, N' — onboarding funnel bypassed.'),
                        @Actor, GETUTCDATE());

                PRINT CONCAT('   ticket #', @NaukriId, ' marked Onboarded.');
            END
        END
    END

    FETCH NEXT FROM cur INTO @RowNo, @First, @Last, @Email, @Phone, @NaukriId;
END
CLOSE cur;
DEALLOCATE cur;

-------------------------------------------------------------------------------------------------
-- Verification (within the transaction)
-------------------------------------------------------------------------------------------------
SELECT 'CreatedAccounts' AS Section, ul.id AS UserId, ul.EmailId, ul.IsActive, ul.IsInterviewerAccess,
       ip.Id AS ProfileId, ip.AssessmentGroupId AS ProfileAGList
FROM UserList ul
LEFT JOIN InterviewerProfile ip ON ip.UserId = ul.id
WHERE LOWER(ul.EmailId) IN ('varun.gupta091@gmail.com', 'jpgfication@gmail.com');

SELECT 'MappedChannels' AS Section, ti.UserId, ti.IRFId, irf.IRFId AS IRFCode,
       irf.AssessmentGroupId AS IRF_AGId, ti.IsActive, ti.IsAvailable, ti.[Source], ti.AddedAt
FROM TransInterviewer ti
JOIN UserList ul ON ul.id = ti.UserId
LEFT JOIN IRF irf ON irf.id = ti.IRFId
WHERE LOWER(ul.EmailId) IN ('varun.gupta091@gmail.com', 'jpgfication@gmail.com')
ORDER BY ti.UserId, ti.AddedAt;

IF @Commit = 1
BEGIN
    COMMIT TRAN;
    PRINT '=== COMMITTED ===';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT '=== DRY RUN — rolled back. Set @Commit = 1 to persist. ===';
END
GO

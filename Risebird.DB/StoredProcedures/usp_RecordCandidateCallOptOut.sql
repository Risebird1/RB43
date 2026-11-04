IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_RecordCandidateCallOptOut'))
BEGIN
    DROP PROCEDURE dbo.usp_RecordCandidateCallOptOut
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Candidate call opt-out (IRF / skill channel level). Called by the api/CallPreference/Candidate/OptOut webhook.
-- The IRF comes from @CandidateId (Candidate.IRFId) or @IRFId (IRF.id). Without either, the event is only
-- logged (OptedOut = 0, IRFId = NULL) and nothing is marked.
-- Matches every candidate row in that IRF with the given id, or the same phone (last 10 digits) / email
-- as the given candidate or as sent, and marks each cold via Usp_ColdCandidateMarked with its next upcoming
-- interview, so that interview is cancelled. The same person in another IRF, and interviewers with the same
-- phone, are not touched.
-- Usp_ColdCandidateMarked selects the cancelled interviews, so this procedure returns one extra result set per
-- marked candidate first (interviewer, interviewDate, candidateName, InterviewId); the app uses them for the
-- cancellation mails. The LAST result set is always OptedOut, IRFId.
CREATE PROCEDURE [dbo].[usp_RecordCandidateCallOptOut]
    @CandidateId    int = NULL,
    @IRFId          int = NULL,
    @Phone          nvarchar(200) = NULL,
    @EmailId        varchar(200) = NULL,
    @Source         varchar(50) = NULL,
    @OptOutSource   varchar(20) = 'AICall',
    @ProviderCallId varchar(100) = NULL,
    @Reason         nvarchar(500) = NULL,
    @RawPayload     nvarchar(max) = NULL,
    @CreatedBy      varchar(200) = NULL,
    @Now            datetime
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormPhone varchar(10) = dbo.fn_NormalizePhone(@Phone)
    SET @EmailId = NULLIF(LTRIM(RTRIM(@EmailId)), '')
    SET @OptOutSource = CASE WHEN @OptOutSource IN ('AICall', 'HumanCall') THEN @OptOutSource ELSE 'AICall' END
    -- Also the interview's Cancellationsource and the cold list's Marked By.
    SET @CreatedBy = ISNULL(NULLIF(LTRIM(RTRIM(@CreatedBy)), ''), 'Candidate opted out on call (' + ISNULL(@Source, 'Unknown') + ')')

    -- Resolve the IRF; a candidate id wins over a sent IRF id. Take the identity phone / email from that row too.
    IF @CandidateId IS NOT NULL
    BEGIN
        DECLARE @CandIRF int, @CandPhone varchar(10), @CandEmail varchar(200)
        SELECT TOP 1 @CandIRF = c.IRFId, @CandPhone = dbo.fn_NormalizePhone(c.Phone), @CandEmail = ul.EmailId
        FROM Candidate c
        LEFT JOIN CandidatesMaster cm ON cm.id = c.CandidateMasterId
        LEFT JOIN UserList ul ON ul.id = cm.UserId
        WHERE c.id = @CandidateId

        IF @CandIRF IS NOT NULL SET @IRFId = @CandIRF
        SET @NormPhone = ISNULL(@NormPhone, @CandPhone)
        SET @EmailId = ISNULL(@EmailId, @CandEmail)
    END

    IF @IRFId IS NULL OR NOT EXISTS (SELECT 1 FROM IRF WHERE id = @IRFId)
    BEGIN
        INSERT INTO CallPreferences (PersonType, CandidateId, IRFId, Phone, EmailId, Action, Source, ProviderCallId, Reason, RawPayload, CreatedOn, CreatedBy)
        VALUES ('Candidate', @CandidateId, @IRFId, @NormPhone, @EmailId, 'OptOut', @Source, @ProviderCallId, @Reason, @RawPayload, @Now, @CreatedBy)

        SELECT 0 AS OptedOut, CAST(NULL AS int) AS IRFId
        RETURN
    END

    DECLARE @IRFCode varchar(200) = (SELECT IRFId FROM IRF WHERE id = @IRFId)

    DECLARE @Matched TABLE (CandidateId int PRIMARY KEY, EmailId varchar(200), Phone varchar(10))

    INSERT INTO @Matched (CandidateId, EmailId, Phone)
    SELECT c.id, ul.EmailId, dbo.fn_NormalizePhone(c.Phone)
    FROM Candidate c
    LEFT JOIN CandidatesMaster cm ON cm.id = c.CandidateMasterId
    LEFT JOIN UserList ul ON ul.id = cm.UserId
    WHERE c.IRFId = @IRFId
      AND (   c.id = @CandidateId
           OR (@NormPhone IS NOT NULL AND dbo.fn_NormalizePhone(c.Phone) = @NormPhone)
           OR (@EmailId IS NOT NULL AND ul.EmailId = @EmailId))

    DECLARE @OptedOut int = (SELECT COUNT(*) FROM @Matched)

    DECLARE @Cid int, @CEmail varchar(200), @InterviewId int
    DECLARE cand_cur CURSOR LOCAL FAST_FORWARD FOR SELECT CandidateId, EmailId FROM @Matched
    OPEN cand_cur
    FETCH NEXT FROM cand_cur INTO @Cid, @CEmail
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @InterviewId = NULL
        SELECT TOP 1 @InterviewId = im.id
        FROM InterviewMeetings im
        WHERE im.IRFId = @IRFCode
          AND im.candidateEmail = @CEmail
          AND im.interviewDate > @Now
          AND ISNULL(im.confirmationStatus, 0) <> 2
        ORDER BY im.interviewDate

        EXEC Usp_ColdCandidateMarked
            @ColdMarkedBY   = @CreatedBy,
            @ColdMarkedDate = @Now,
            @FKCandidateId  = @Cid,
            @InterviewId    = @InterviewId,
            @type           = 1,
            @Currentdate    = @Now,
            @ColdSource     = @OptOutSource,
            @ColdReason     = @Reason

        FETCH NEXT FROM cand_cur INTO @Cid, @CEmail
    END
    CLOSE cand_cur
    DEALLOCATE cand_cur

    IF @OptedOut = 0
        INSERT INTO CallPreferences (PersonType, CandidateId, IRFId, Phone, EmailId, Action, Source, ProviderCallId, Reason, RawPayload, CreatedOn, CreatedBy)
        VALUES ('Candidate', @CandidateId, @IRFId, @NormPhone, @EmailId, 'OptOut', @Source, @ProviderCallId, @Reason, @RawPayload, @Now, @CreatedBy)
    ELSE
        INSERT INTO CallPreferences (PersonType, CandidateId, IRFId, Phone, EmailId, Action, Source, ProviderCallId, Reason, RawPayload, CreatedOn, CreatedBy)
        SELECT 'Candidate', m.CandidateId, @IRFId, ISNULL(m.Phone, @NormPhone), m.EmailId, 'OptOut', @Source, @ProviderCallId, @Reason, @RawPayload, @Now, @CreatedBy
        FROM @Matched m

    SELECT @OptedOut AS OptedOut, @IRFId AS IRFId
END
GO

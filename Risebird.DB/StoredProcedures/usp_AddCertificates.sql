CREATE OR ALTER PROCEDURE [dbo].[usp_AddCertificates]
    @Emailid  VARCHAR(150),
    @year     VARCHAR(15),
    @comment  VARCHAR(250),
    @clink    VARCHAR(1000),
    @month    VARCHAR(15),
    @id       INT              -- kept for backward compatibility; ignored in upsert logic
AS
BEGIN
    SET NOCOUNT ON;

    -- Resolve interviewer id from email
    DECLARE @interviewerid INT;
    SELECT @interviewerid = id FROM UserList WHERE EmailId = @Emailid;

    IF @interviewerid IS NULL
    BEGIN
        RAISERROR('usp_AddCertificates: no UserList row found for EmailId = %s', 16, 1, @Emailid);
        RETURN;
    END

    -- Upsert based on (interviewerid, year, month) — never create duplicates
    IF EXISTS (
        SELECT 1 FROM InterviewerCertificates
        WHERE interviewerid = @interviewerid
          AND year          = @year
          AND month         = @month
    )
    BEGIN
        UPDATE InterviewerCertificates
        SET certificatelink = @clink,
            comment         = @comment
        WHERE interviewerid = @interviewerid
          AND year          = @year
          AND month         = @month;
    END
    ELSE
    BEGIN
        INSERT INTO InterviewerCertificates (interviewerid, year, month, certificatelink, comment)
        VALUES (@interviewerid, @year, @month, @clink, @comment);
    END
END
GO

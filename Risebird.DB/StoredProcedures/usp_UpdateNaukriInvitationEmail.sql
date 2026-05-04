CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateNaukriInvitationEmail]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET InvitationEmailSent   = 1,
        InvitationEmailSentAt = GETUTCDATE(),
        UpdatedAt             = GETUTCDATE()
    WHERE Id = @Id
END
GO

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SetInterviewerAlignmentRequired'))
BEGIN
    DROP PROCEDURE dbo.usp_SetInterviewerAlignmentRequired
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Switches the interview alignment page on or off for one channel (IRF.IsInterviewerAlignmentRequired),
-- from the "Interviewer alignment required" checkbox on Edit IRF.
--
-- This is deliberately its own write, touching this one column and nothing else. The switch used to be
-- saved through usp_UpdateIRFDetails with the rest of the Edit IRF form, which re-applies the channel's
-- interviewer panel on every save (MAG membership forces IsActive = 0, and every future interview of an
-- inactive interviewer is unassigned). Toggling the switch must never change who is active on the
-- channel or which interviews are assigned, so it no longer goes anywhere near that save.
CREATE PROCEDURE [dbo].[usp_SetInterviewerAlignmentRequired]
    @IrfId      INT,
    @IsRequired BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.IRF
       SET IsInterviewerAlignmentRequired = @IsRequired
     WHERE id = @IrfId;

    SELECT @@ROWCOUNT AS UpdatedRows;
END
GO

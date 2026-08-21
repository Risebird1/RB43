IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SetAIQuestionSetsEnabled'))
BEGIN
    DROP PROCEDURE dbo.usp_SetAIQuestionSetsEnabled
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Switches a preset AI interview channel's question sets on or off.
--
-- Switching ON is refused unless the channel already has sets. The safe order is always create the
-- papers first, then switch on, because a channel that is switched on with no papers cannot serve a
-- question at all: the launcher refuses to widen the query back to the whole pool, so every
-- interview on it would end immediately. Refusing here means the only way into that state is
-- deleting sets out from under a live channel, which the runtime still guards against.
--
-- Switching OFF is always allowed and never disturbs an interview in flight.
-- usp_AssignAIQuestionSetToInterview reads an existing binding before it consults this flag, so a
-- candidate already on a paper stays on it: an interview is locked to its paper for life. The flag
-- only decides whether new interviews get bound.
Create PROCEDURE [dbo].[usp_SetAIQuestionSetsEnabled]
(
    @IRFId int,
    @Enabled bit,
    @Success bit OUTPUT,
    @Enabled_Result bit OUTPUT,
    @Message nvarchar(400) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    SET @Enabled_Result = 0;
    SET @Message = NULL;

 begin try
  begin tran

    IF NOT EXISTS (SELECT 1 FROM IRF
                   WHERE id = @IRFId
                     AND ISNULL(IsAIInterview, 0) = 1
                     AND ISNULL(AIInterviewType, 0) = 2)
    BEGIN
        SET @Message = 'This skill channel is not a preset AI interview channel, so question sets do not apply to it.';
        SET @Enabled_Result = 0;
        commit tran;
        RETURN;
    END

    IF @Enabled = 1 AND NOT EXISTS (SELECT 1 FROM AIQuestionSets WHERE IRFId = @IRFId)
    BEGIN
        SET @Message = 'Create the question sets first. A channel switched on with no sets cannot serve any questions.';
        SET @Enabled_Result = 0;
        commit tran;
        RETURN;
    END

    UPDATE IRF
    SET UseAIQuestionSets = @Enabled,
        ModifiedOn = GETDATE()
    WHERE id = @IRFId;

    SET @Success = 1;
    SET @Enabled_Result = @Enabled;
    SET @Message = CASE WHEN @Enabled = 1
                        THEN 'This channel now deals questions from balanced sets. Each new interview is locked to one set.'
                        ELSE 'This channel no longer deals questions from sets. Interviews already bound to a set keep it.' END;

  commit tran
 end try
 begin catch
  IF @@TRANCOUNT > 0
   rollback transaction

  DECLARE @ErrMessage varchar(MAX) = ERROR_MESSAGE(),
   @Severity int = ERROR_SEVERITY(),
   @State smallint = ERROR_STATE()

  RAISERROR (@ErrMessage, @Severity, @State)
 end catch
END
GO

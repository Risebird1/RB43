  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ResetInterviewPayment'))
BEGIN
    DROP PROCEDURE usp_ResetInterviewPayment
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_ResetInterviewPayment
@interviewid int 
AS
BEGIN
Update InterviewPoints set type='Redo' , Points=0 where interviewid=@interviewid and (type='Penalty QC' or type='Interview' or (type='Penalty' and Comment like '%Late Report%')) and Status='Pending'
END
GO

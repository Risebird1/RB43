/****** Object:  StoredProcedure [dbo].[usp_AddCandidateStatus]    Script Date: 24-11-2020 2.34.29 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddCandidateStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_AddCandidateStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_AddCandidateStatus]
@IRFId int,
@CandidateId int,
@Comments varchar(max),
@Date datetime,
@AudioFileLink nvarchar(max),
@Status int,
@PerformedBy varchar(100)
    
AS
BEGIN
      INSERT INTO CandidateStatus (IRFId, CandidateId, AudioFileLink, Comments, [Date], [Status], [PerformedBy])
      VALUES (@IRFId, @CandidateId, @AudioFileLink, @Comments, @Date, @Status, @PerformedBy)
END

GO



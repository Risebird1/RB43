IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdatevideoDetailsById'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdatevideoDetailsById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdatevideoDetailsById
@interviewid int,
@VideoURL varchar(1000),
@videoduration int
AS
BEGIN
Update InterviewComments set VideoLink=@VideoURL,VideoDuration=@videoduration where InterviewId=@interviewid
END
Go
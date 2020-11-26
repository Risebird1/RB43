/****** Object:  StoredProcedure [dbo].[usp_UpdateCandidateStatus]    Script Date: 25-11-2020 4.15.17 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateCandidateStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCandidateStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_UpdateCandidateStatus]
@id int,
@IRFId int,
@CandidateId int,
@Comments varchar(max),
@Date datetime,
@AudioFileLink nvarchar(max),
@Status int
    
AS
BEGIN
      

	  update CandidateStatus 

	  set 

	  IRFId=@IRFId,
	  CandidateId=@CandidateId,
	  AudioFileLink =@AudioFileLink,
	  Comments=@Comments,
	  [Date]=@Date,
	  [Status]=@Status

	  where id =@id
END

GO



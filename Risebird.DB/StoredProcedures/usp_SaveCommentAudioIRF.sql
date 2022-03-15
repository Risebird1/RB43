  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveCommentAudioIRF'))
BEGIN
    DROP PROCEDURE [dbo].[usp_SaveCommentAudioIRF]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_SaveCommentAudioIRF]     
@irfid int,
@AudioComment  nvarchar(1000)
AS      
BEGIN      
   update IRF set AudioComment =@AudioComment  where id=@irfid
END
GO
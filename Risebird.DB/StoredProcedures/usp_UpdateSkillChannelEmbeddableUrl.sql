IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateSkillChannelEmbeddableUrl'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateSkillChannelEmbeddableUrl
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdateSkillChannelEmbeddableUrl
@irfId varchar(200),
@embedUrl varchar(2500),
@timeNow datetime = null
AS
BEGIN

Update IRF set AffindaEmbedUrl=@embedUrl, AffindaEmbedUrlCreatedAt = @timeNow where IRFId=@irfId

END
Go
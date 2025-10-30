IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveSentimentAnalysis'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSentimentAnalysis
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_SaveSentimentAnalysis
@SessionId varchar(200),
@biqvId int,
@FEAR float = null,
@CONFUSED float = null,
@SAD float = null,
@CALM float = null,
@SURPRISED float = null,
@DISGUSTED float = null,
@ANGRY float = null,
@HAPPY float = null,
@CreatedAt datetime,
@CreatedBy varchar(75)
AS
BEGIN

INSERT INTO [dbo].[VideoSentimentAnalysis]
(
    [biqvId],
    [FEAR],
    [CONFUSED],
    [SAD],
    [CALM],
    [SURPRISED],
    [DISGUSTED],
    [ANGRY],
    [HAPPY],
    [CreatedAt],
    [CreatedBy]
)
VALUES
(
    @biqvId,
    @FEAR,
    @CONFUSED,
    @SAD,
    @CALM,
    @SURPRISED,
    @DISGUSTED,
    @ANGRY,
    @HAPPY,
    @CreatedAt,
    @CreatedBy
);


END
GO
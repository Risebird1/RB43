/****** Object:  StoredProcedure [dbo].[usp_AddActivity]    Script Date: 13-01-2021 5.15.38 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddActivity'))
BEGIN
    DROP PROCEDURE dbo.usp_AddActivity
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_AddActivity]
@ActivityType int,
@PerformedBy varchar(100),
@Summary varchar(max),
@FileUrl varchar(max),
@PerformedAt datetime,
@Particulars varchar(150),
@ResourceUrl varchar(500)

    
AS
BEGIN
      INSERT INTO ActivityTracker(ActivityType, PerformedBy, Summary, FileUrl, PerformedAt, Particulars, ResourceUrl)
      VALUES (@ActivityType, @PerformedBy, @Summary, @FileUrl, @PerformedAt, @Particulars, @ResourceUrl)
END
GO



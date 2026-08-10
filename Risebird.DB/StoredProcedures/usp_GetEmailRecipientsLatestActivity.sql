/****** Object:  StoredProcedure [dbo].[usp_GetEmailRecipientCurrentStatus]    ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetEmailRecipientsLatestActivity'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEmailRecipientsLatestActivity
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetEmailRecipientsLatestActivity] 
@recipientType varchar(30),
@startDate datetime = null,
@endDate datetime = null,
@timeNow datetime,
@campaignName varchar(50)  = null
AS    
Begin   
  
  declare @campaignId int
  if(@campaignName is not null)
  Begin
	set @campaignId = (select id from AGCampaigns where CampaignName = @campaignName)
  End

  DECLARE @idProfile INT = CASE @recipientType
      WHEN 'Candidate'   THEN 23
      WHEN 'Interviewer' THEN 3
      WHEN 'Client'      THEN 4
  END;

  DECLARE @CreatedFrom DATETIME;
  DECLARE @CreatedTo   DATETIME;

  IF @startDate IS NULL AND @endDate IS NULL
  BEGIN
      SET @CreatedFrom = CAST(DATEADD(DAY, -1, @timeNow) AS DATE);
      SET @CreatedTo   = NULL;
  END
  ELSE
  BEGIN
      SET @CreatedFrom = CAST(@startDate AS DATE);
      SET @CreatedTo   = DATEADD(DAY, 1, CAST(@endDate AS DATE));
  END;

	;WITH FilteredEvents AS (
		SELECT
			sge.id,
			sge.email,
			sge.Split_Message_Id,
			sge.[timestamp]
		FROM dbo.SendgridEventData sge
		INNER JOIN dbo.UserList ul
			ON  ul.EmailId   = sge.email
			AND ul.idProfile = @idProfile
		WHERE sge.[event] IN ('processed', 'delivered', 'deferred', 'bounce', 'dropped')
		  AND sge.CreatedAt >= @CreatedFrom
		  AND (@CreatedTo IS NULL OR sge.CreatedAt < @CreatedTo)
	),
	LatestPerMessage AS (
		SELECT
			id,
			ROW_NUMBER() OVER (
				PARTITION BY email, Split_Message_Id
				ORDER BY TRY_CAST([timestamp] AS BIGINT) DESC, id DESC
			) AS rn
		FROM FilteredEvents
	)
	SELECT id INTO #latestActivityRowId FROM LatestPerMessage WHERE rn = 1;

	CREATE CLUSTERED INDEX IX_latestActivityRowId ON #latestActivityRowId (id);

  
  select smd.MailBody, smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt as SentOn, smd.SentBy, smd.sg_message_id,
  sge.email, sge.[event] as latestEvent, sge.CreatedAt as ActivityDate, sge.[timestamp], sge.[url],
  irf.IRFId as SkillChannelName, im.interviewDate, agc.CampaignName
  from SentMailDetails smd
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id
  join #latestActivityRowId la on la.id = sge.id
  join UserList ul on ul.EmailId = sge.email and ul.idProfile = @idProfile
  left join IRF on smd.IRFId = irf.id 
  left join InterviewMeetings im on im.id = smd.InterviewId
  left join AGCampaigns agc on agc.id = smd.CampaignId
  where sge.email != 'hire@risebird.io'
  and smd.CreatedAt >= @CreatedFrom
  and (@CreatedTo IS NULL OR smd.CreatedAt < @CreatedTo)
  and (@recipientType <> 'Interviewer' or @campaignId is null or @campaignId = smd.CampaignId)
  order by smd.CreatedAt desc
  drop table #latestActivityRowId
 
End  
  
GO



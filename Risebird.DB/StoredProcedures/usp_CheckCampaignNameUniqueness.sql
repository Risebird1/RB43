/****** Object:  StoredProcedure [dbo].[usp_CheckCampaignNameUniqueness]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CheckCampaignNameUniqueness'))
BEGIN
    DROP PROCEDURE dbo.usp_CheckCampaignNameUniqueness
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_CheckCampaignNameUniqueness]
@value varchar(100)

AS
Begin

declare @tbl_Names table (CampaignName varchar(50));
		
	Begin

		insert into @tbl_Names
		select top 1 CampaignName from AGCampaigns where CampaignName = @value

		select * from @tbl_Names

	End
	
End

GO



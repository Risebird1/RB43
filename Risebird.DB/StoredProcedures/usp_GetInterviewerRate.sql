IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerRate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerRate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetInterviewerRate]          
@userId int,
@companyType varchar(50) = null
AS          
Begin  
declare @country int, @rate int

--set @rate =1000
set @country = (select Country from InterviewerProfile where UserId =@userId)

if(@country =26) --India
begin
	if(@companyType='Product')
	begin
		set @rate = (select top 1 ir.IndiaProductRate from InterviewerProfile ipr join InterviewerRecommendation ir on ir.Userid = ipr.UserId where ipr.UserId =@userId)
	end
	else
	begin
		set @rate = (select top 1 ir.PerHourRateIndia from InterviewerProfile ipr join InterviewerRecommendation ir on ir.Userid = ipr.UserId where ipr.UserId =@userId)
	end
end
else
begin
	if(@companyType='Product')
	begin
		set @rate = (select top 1 ir.OutsideIndiaProductRate from InterviewerProfile ipr join InterviewerRecommendation ir on ir.Userid = ipr.UserId where ipr.UserId =@userId)
	end
	else
	begin
		set @rate = (select top 1 ir.PerHourRateOutside from InterviewerProfile ipr join InterviewerRecommendation ir on ir.Userid = ipr.UserId where ipr.UserId =@userId)
	end
end

if(@rate is null)
begin
	set @rate =1000
end
select @rate

End 
GO
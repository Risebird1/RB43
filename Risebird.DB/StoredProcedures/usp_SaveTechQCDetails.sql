  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveTechQCDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveTechQCDetails
END

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_SaveTechQCDetails
@Emailid varchar(150),
@interviewid int,
@activestatus bit,
@timeNow datetime
AS
BEGIN
if exists(select top 1 id from TechQualityDetails where Interviewid = @interviewid)
begin
Update TechQualityDetails set CreatedBy=@Emailid,CreatedOn=@timeNow,IsActive=@activestatus where Interviewid=@interviewid
end
else 
begin
Insert into TechQualityDetails(Interviewid,CreatedBy,CreatedOn,IsActive) values(@interviewid,@Emailid,@timeNow,@activestatus)
end
END
Go
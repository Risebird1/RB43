IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_updateformat'))
BEGIN
    DROP PROCEDURE dbo.usp_updateformat
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_updateformat
@irfid int,
@format varchar(1000)
AS
BEGIN
If Exists (select 1 from IRFInstructionDetails where  IRFId=@irfid)    
begin
Update IRFInstructionDetails set [Format]=@format where IRFId=@irfid
end
else
begin
Insert into IRFInstructionDetails(IRFId,[Format]) values(@irfid,@format)
end
END
GO
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_updatedifficultylevel'))
BEGIN
    DROP PROCEDURE dbo.usp_updatedifficultylevel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_updatedifficultylevel
@irfid int,
@difficultylevel int
AS
BEGIN
If Exists (select 1 from IRFInstructionDetails where  IRFId=@irfid)    
begin
Update IRFInstructionDetails set Difficultylevel=@difficultylevel where IRFId=@irfid
end
else
begin
Insert into IRFInstructionDetails(IRFId,Difficultylevel) values(@irfid,@difficultylevel)
end
END
GO
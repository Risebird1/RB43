IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetTextInstruction'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTextInstruction
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[sp_usp_GetTextInstruction]     
@irfId int
as
begin
Select * from ClientBroadcastMessages where IRFId=@irfId
end
Go
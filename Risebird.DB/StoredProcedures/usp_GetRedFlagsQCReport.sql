/****** Object:  StoredProcedure [dbo].[usp_GetRedFlagsQCReport]    Script Date: 13-01-2021 5.15.38 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRedFlagsQCReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRedFlagsQCReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetRedFlagsQCReport]
@Interviewid int
as 
begin
Select * from Trans_QCFormReport where interviewid=@Interviewid
end
GO



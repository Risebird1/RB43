/****** Object:  StoredProcedure [dbo].[usp_RecruiterBulkInsert]    Script Date: 24-06-2020 1.33.40 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_RecruiterBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_RecruiterBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_RecruiterBulkInsert]
(
      @RecruiterBulkData AS [dbo].[TransRecruiter_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[TransRecruiter](
	[IRFId] ,
	[UserId],
	[RecruiterId])
	

SELECT rbd.*, Recruiter.id as RecruiterId FROM @RecruiterBulkData rbd left join Recruiter on rbd.UserId = Recruiter.UserId


END
GO



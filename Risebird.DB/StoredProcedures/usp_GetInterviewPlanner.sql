IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewPlanner'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewPlanner
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetInterviewPlanner]

@IRFId int

AS
if exists(select * from InterviewPlanner where IRFId = @IRFId)
Begin

Select InterviewPlanner.*,IRF.CreatedOn as [IRFCreatedOn] from InterviewPlanner inner join [IRF] on InterviewPlanner.IRFId = [IRF].id  where InterviewPlanner.IRFId = @IRFId

end
else
begin 
select IRF.StartDate as [IRFCreatedOn] from IRF where id = @IRFId
end 

GO



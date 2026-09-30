/****** Object:  StoredProcedure [dbo].[usp_GetAllUserVMs]    Script Date: 25-09-2020 11.57.16 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllUserVMs'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllUserVMs
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetAllUserVMs]
@EmailId varchar(200),   
@Profile varchar(100)


AS
Begin

declare @TotalQuestions table (VmId int, QuestionsCount int);
		insert into @TotalQuestions 
		select VmId, count(VmId) from ProctorVMQuestions group by VmId

		

IF (@Profile = 'Admin') 
	Begin

		select vmp.*, tvm.LastAction, tq.QuestionsCount from VMProperties vmp left join TransVMTriggerDetails tvm  on tvm.VMPropertyId=vmp.id 
		left join @TotalQuestions tq on tq.VmId = vmp.id
		where IsRemoved=0 and IsDeleted=0 and ISNULL(vmp.Purpose, '') <> 'CodeProject'


	End
Else
	Begin

	declare @userId int = (select id from UserList where EmailId=@EmailId)

		select vmp.*, tvm.LastAction, tq.QuestionsCount from VMProperties vmp left join TransVMTriggerDetails tvm  on tvm.VMPropertyId=vmp.id join TransVMUser tvu on tvu.VmId = vmp.id
		left join @TotalQuestions tq on tq.VmId = vmp.id
		where IsRemoved=0 and IsDeleted=0 and tvu.UserId = @userId and ISNULL(vmp.Purpose, '') <> 'CodeProject'

	End
End

GO



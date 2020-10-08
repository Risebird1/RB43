/****** Object:  StoredProcedure [dbo].[usp_DeleteTransVmUsers]    Script Date: 07-10-2020 1.24.29 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_RemoveCandidateVmAccess'))
BEGIN
    DROP PROCEDURE dbo.usp_RemoveCandidateVmAccess
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_RemoveCandidateVmAccess]

@profileId int,
@time datetime
AS
Begin

delete from TransVMUser where ProfileId=@profileId and SharedOn<=@time

End
GO



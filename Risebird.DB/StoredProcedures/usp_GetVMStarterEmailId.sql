/****** Object:  StoredProcedure [dbo].[usp_RemoveCandidateVmAccess]    Script Date: 09-10-2020 4.47.00 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetVMStarterEmailId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetVMStarterEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_GetVMStarterEmailId]

@time datetime
AS
Begin

declare @tbl table (id int);
	insert into @tbl
	select max(vap.id) from VMProperties vmp join VMActionPerformed vap on vmp.id= vap.VmId where vmp.[Status]=7 and vap.[Action]='Start' and vap.PerformedOn<=@time group by vmp.[Name]
	
select vmp.[Name], vap.PerformedBy from VMProperties vmp join VMActionPerformed vap on vmp.id=vap.VmId join @tbl tl on vap.id=tl.id


End
GO


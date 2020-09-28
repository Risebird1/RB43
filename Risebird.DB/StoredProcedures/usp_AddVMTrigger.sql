/****** Object:  StoredProcedure [dbo].[usp_AddVMTrigger]    Script Date: 16-09-2020 6.50.57 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddVMTrigger'))
BEGIN
    DROP PROCEDURE dbo.usp_AddVMTrigger
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddVMTrigger]
@vmId int,
@action varchar(30),
@expectedStatus varchar(30)


AS
Begin

delete from TransVMTriggerDetails where VMPropertyId=@vmId

insert into TransVMTriggerDetails (VMPropertyId, LastAction, ExpectedStatus) values (@vmId, @action, @expectedStatus)

End

GO



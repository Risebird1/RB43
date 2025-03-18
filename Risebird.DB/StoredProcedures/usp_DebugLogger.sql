IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DebugLogger'))
BEGIN
    DROP PROCEDURE dbo.usp_DebugLogger
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_DebugLogger]     
@message varchar(max),
@type varchar(50),
@performedBy varchar(100),
@performedAt datetime
As
Begin

insert into DebugLogger values ( @type, @message, @performedBy, @performedAt)

End
GO

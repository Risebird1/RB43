/****** Object:  StoredProcedure [dbo].[usp_StoreClientBroadcastMessage]  ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_StoreClientBroadcastMessage'))
BEGIN
    DROP PROCEDURE dbo.usp_StoreClientBroadcastMessage
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_StoreClientBroadcastMessage]
@IRFId int,
@SentBy varchar(50),
@Message varchar(max),
@Subject varchar(500),
@SentAt datetime

    
AS
BEGIN
      INSERT INTO ClientBroadcastMessages(IRFId, [Subject], [Message], SentBy, SentAt)
      VALUES (@IRFId, @Subject, @Message, @SentBy, @SentAt)
END
GO



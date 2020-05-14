/****** Object:  StoredProcedure [dbo].[usp_AddinterviewvedioShare]    Script Date: 4/18/2020 10:52:02 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddinterviewvedioShare'))
BEGIN
    DROP PROCEDURE dbo.usp_AddinterviewvedioShare
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure [dbo].[usp_AddinterviewvedioShare]
@isShared varchar(10),
@VedioId int
AS
Begin
Update InterviewComments set IsShared=@isShared where InterviewId=@VedioId 
END


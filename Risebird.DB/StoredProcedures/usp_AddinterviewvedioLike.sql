/****** Object:  StoredProcedure [dbo].[usp_AddinterviewvedioLike]    Script Date: 4/18/2020 10:51:55 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddinterviewvedioLike'))
BEGIN
    DROP PROCEDURE dbo.usp_AddinterviewvedioLike
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure [dbo].[usp_AddinterviewvedioLike]
@isLiked varchar(50),
@VedioId varchar(50),
@EmailID varchar(50)
As
Begin
Declare @id int
DECLARE @Count int
Select @id= Id from UserList where EmailId=@EmailID
Select @Count= Count(*) from RisetubeVideoLike where Userid=@id and Interviewid=@VedioId
if(@Count>0)
BEGIN
Update RisetubeVideoLike set isLike=@isLiked where Userid=@id and Interviewid=@VedioId
END
ELSE
BEGIN
Insert into RisetubeVideoLike values(@Id,@VedioId, @isLiked) 
END
Select (Select count(*) from RisetubeVideoLike where IsLike='Liked' and InterviewId=@VedioId) as TotalVedioLike,(Select count(*) from RisetubeVideoLike where IsLike='Disliked' and InterviewId=@VedioId) as TotalVedioDisLike

END



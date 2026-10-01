IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddCodeProjectVm'))
BEGIN
    DROP PROCEDURE dbo.usp_AddCodeProjectVm
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_AddCodeProjectVm
    @Handle varchar(64),
    @SessionId varchar(50),
    @QuestionId int,
    @ResourceGroupName varchar(90),
    @Location varchar(50),
    @VmName varchar(64),
    @NicName varchar(80),
    @StackImageCode varchar(50) = NULL,
    @GalleryImageId varchar(500),
    @OsType varchar(25) = NULL,
    @ProjectZipUrl nvarchar(2000) = NULL,
    @ReadOnlyPathsJson nvarchar(max) = NULL,
    @CandidateUserName varchar(50),
    @CandidatePasswordProtected nvarchar(500),
    @AdminPasswordProtected nvarchar(500)
AS
BEGIN
    SET NOCOUNT ON;
    -- One machine per (session, question): a repeated provision call returns the existing handle.
    DECLARE @Existing varchar(64) =
        (SELECT Handle FROM CodeProjectVmProvisioning WHERE SessionId = @SessionId AND QuestionId = @QuestionId);
    IF @Existing IS NOT NULL
    BEGIN
        SELECT @Existing AS Handle;
        RETURN;
    END

    INSERT INTO CodeProjectVmProvisioning
        (Handle, SessionId, QuestionId, ResourceGroupName, Location, VmName, NicName, StackImageCode, GalleryImageId,
         OsType, Step, Message, ProjectZipUrl, ReadOnlyPathsJson, CandidateUserName, CandidatePasswordProtected, AdminPasswordProtected)
    VALUES
        (@Handle, @SessionId, @QuestionId, @ResourceGroupName, @Location, @VmName, @NicName, @StackImageCode, @GalleryImageId,
         @OsType, 'requested', N'Allocating machine…', @ProjectZipUrl, @ReadOnlyPathsJson, @CandidateUserName, @CandidatePasswordProtected, @AdminPasswordProtected);
    SELECT @Handle AS Handle;
END
GO

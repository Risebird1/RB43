/* =====================================================================
   Code project candidate machines — provisioner state (Risebird.AzureTimerFunctions).
   Run AFTER AddCodeProjectQuestionSupport.sql. Idempotent.

   A candidate machine is created in steps (NIC -> VM -> setup Run Command
   -> Bastion shareable link), each an Azure long-running operation. The
   state lives here so the status endpoint and a 15-second timer can both
   advance it, and so a function restart resumes rather than leaks a VM.

   Every machine is also a VMProperties row (Purpose = 'CodeProject') so
   CodingAssessment.VMPropertyId has something to point at. Those rows are
   excluded from the proctor VM lists and from the IP / status timers.
   ===================================================================== */

-- ---------------------------------------------------------------------
-- 1. VMProperties.Purpose
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Purpose' AND Object_ID = Object_ID(N'VMProperties'))
    ALTER TABLE VMProperties ADD Purpose varchar(30) NULL;
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetAllVMs]
AS
Begin
select vmp.*, tvm.LastAction from VMProperties vmp left join TransVMTriggerDetails tvm  on tvm.VMPropertyId=vmp.id  where IsRemoved=0 and IsDeleted=0 and ISNULL(vmp.Purpose, '') <> 'CodeProject'
End
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetAllUserVMs]
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

-- ---------------------------------------------------------------------
-- 2. CodeProjectVmProvisioning
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodeProjectVmProvisioning')
BEGIN
    CREATE TABLE [dbo].[CodeProjectVmProvisioning](
        [Handle]                     [varchar](64)    NOT NULL,
        [SessionId]                  [varchar](50)    NOT NULL,
        [QuestionId]                 [int]            NOT NULL,
        [VMPropertyId]               [int]            NULL,
        [ResourceGroupName]          [varchar](90)    NOT NULL,
        [Location]                   [varchar](50)    NOT NULL,
        [VmName]                     [varchar](64)    NOT NULL,
        [NicName]                    [varchar](80)    NOT NULL,
        [StackImageCode]             [varchar](50)    NULL,
        [GalleryImageId]             [varchar](500)   NOT NULL,
        [OsType]                     [varchar](25)    NULL,
        -- requested | nic | vm | setup | link | ready | failed | released
        [Step]                       [varchar](20)    NOT NULL,
        [Message]                    [nvarchar](1000) NULL,
        -- Short-lived SAS of the candidate zip. Cleared once setup has run.
        [ProjectZipUrl]              [nvarchar](2000) NULL,
        [ReadOnlyPathsJson]          [nvarchar](max)  NULL,
        [CandidateUserName]          [varchar](50)    NOT NULL,
        -- Both passwords encrypted with IEncryptionHelper. The admin password is kept only so a
        -- retried VM PUT is identical (Azure rejects a changed adminPassword on an existing VM).
        [CandidatePasswordProtected] [nvarchar](500)  NOT NULL,
        [AdminPasswordProtected]     [nvarchar](500)  NOT NULL,
        [ConnectUrl]                 [nvarchar](1000) NULL,
        [LeaseUntilUtc]              [datetime2]      NULL,
        [CreatedAtUtc]               [datetime2]      NOT NULL CONSTRAINT DF_CodeProjectVmProvisioning_Created DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc]               [datetime2]      NOT NULL CONSTRAINT DF_CodeProjectVmProvisioning_Updated DEFAULT SYSUTCDATETIME(),
        [ReadyAtUtc]                 [datetime2]      NULL,
        [ReleasedAtUtc]              [datetime2]      NULL,
     CONSTRAINT [PK_CodeProjectVmProvisioning] PRIMARY KEY CLUSTERED ([Handle] ASC),
     CONSTRAINT [UQ_CodeProjectVmProvisioning_SessionQuestion] UNIQUE ([SessionId], [QuestionId])
    ) ON [PRIMARY];
END
GO

-- ---------------------------------------------------------------------
-- 3. Procedures
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE usp_AddCodeProjectVm
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

CREATE OR ALTER PROCEDURE usp_GetCodeProjectVm
    @Handle varchar(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM CodeProjectVmProvisioning WHERE Handle = @Handle;
END
GO

-- Takes a short lease so the timer and a status call never advance the same machine at once.
CREATE OR ALTER PROCEDURE usp_LeaseCodeProjectVm
    @Handle varchar(64),
    @LeaseSeconds int = 60
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectVmProvisioning
    SET LeaseUntilUtc = DATEADD(second, @LeaseSeconds, SYSUTCDATETIME())
    WHERE Handle = @Handle
      AND Step NOT IN ('ready', 'failed', 'released')
      AND (LeaseUntilUtc IS NULL OR LeaseUntilUtc < SYSUTCDATETIME());
    SELECT @@ROWCOUNT AS Leased;
END
GO

CREATE OR ALTER PROCEDURE usp_UpdateCodeProjectVm
    @Handle varchar(64),
    @Step varchar(20),
    @Message nvarchar(1000) = NULL,
    @VMPropertyId int = NULL,
    @ConnectUrl nvarchar(1000) = NULL,
    @ClearProjectZipUrl bit = 0,
    @ReleaseLease bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectVmProvisioning
    SET Step = @Step,
        Message = @Message,
        VMPropertyId = ISNULL(@VMPropertyId, VMPropertyId),
        ConnectUrl = ISNULL(@ConnectUrl, ConnectUrl),
        ProjectZipUrl = CASE WHEN @ClearProjectZipUrl = 1 THEN NULL ELSE ProjectZipUrl END,
        LeaseUntilUtc = CASE WHEN @ReleaseLease = 1 THEN NULL ELSE LeaseUntilUtc END,
        UpdatedAtUtc = SYSUTCDATETIME(),
        ReadyAtUtc = CASE WHEN @Step = 'ready' AND ReadyAtUtc IS NULL THEN SYSUTCDATETIME() ELSE ReadyAtUtc END,
        ReleasedAtUtc = CASE WHEN @Step = 'released' AND ReleasedAtUtc IS NULL THEN SYSUTCDATETIME() ELSE ReleasedAtUtc END
    WHERE Handle = @Handle;

    IF @Step = 'released'
        UPDATE vmp SET IsDeleted = 1
        FROM VMProperties vmp
        JOIN CodeProjectVmProvisioning p ON p.VMPropertyId = vmp.id
        WHERE p.Handle = @Handle;

    IF @Step = 'ready'
        UPDATE vmp SET Status = 7   -- VMState.running
        FROM VMProperties vmp
        JOIN CodeProjectVmProvisioning p ON p.VMPropertyId = vmp.id
        WHERE p.Handle = @Handle;
END
GO

-- Machines still being built, for the advance timer.
CREATE OR ALTER PROCEDURE usp_GetInProgressCodeProjectVms
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Handle FROM CodeProjectVmProvisioning
    WHERE Step NOT IN ('ready', 'failed', 'released')
      AND (LeaseUntilUtc IS NULL OR LeaseUntilUtc < SYSUTCDATETIME());
END
GO

-- Machines to tear down: failed ones, and any that outlived the interview.
CREATE OR ALTER PROCEDURE usp_GetExpiredCodeProjectVms
    @MaxLifetimeMinutes int = 120,
    @FailedGraceMinutes int = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Handle FROM CodeProjectVmProvisioning
    WHERE Step <> 'released'
      AND ((Step = 'failed' AND UpdatedAtUtc < DATEADD(minute, -@FailedGraceMinutes, SYSUTCDATETIME()))
           OR CreatedAtUtc < DATEADD(minute, -@MaxLifetimeMinutes, SYSUTCDATETIME()));
END
GO

CREATE OR ALTER PROCEDURE usp_AddCodeProjectVMProperty
    @Name varchar(150),
    @ResourceGroupName varchar(150),
    @OSType varchar(25) = NULL,
    @VmLocation varchar(150) = NULL,
    @UserName varchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- No password is stored here: candidate credentials live encrypted in CodeProjectVmProvisioning.
    INSERT INTO VMProperties (Name, ResourceGroupName, Status, IsAdded, IsRemoved, IsDeleted, OSType, VmLocation, UserName, CreateDate, Purpose)
    VALUES (@Name, @ResourceGroupName, 6, 1, 0, 0, @OSType, @VmLocation, @UserName, GETDATE(), 'CodeProject');
    SELECT CAST(SCOPE_IDENTITY() AS int) AS NewId;
END
GO

-- ---------------------------------------------------------------------
-- 4. Session integrity: deadline lock and Bastion session monitoring
-- ---------------------------------------------------------------------
IF COL_LENGTH('dbo.CodeProjectVmProvisioning', 'DeadlineUtc') IS NULL
    ALTER TABLE CodeProjectVmProvisioning ADD DeadlineUtc datetime2 NULL;          -- end of slot + grace, set at handover
GO
IF COL_LENGTH('dbo.CodeProjectVmProvisioning', 'LockedAtUtc') IS NULL
    ALTER TABLE CodeProjectVmProvisioning ADD LockedAtUtc datetime2 NULL;          -- candidate account disabled and logged off
GO
IF COL_LENGTH('dbo.CodeProjectVmProvisioning', 'SeenSessionIdsJson') IS NULL
    ALTER TABLE CodeProjectVmProvisioning ADD SeenSessionIdsJson nvarchar(max) NULL;   -- every Bastion session id seen for this machine
GO
IF COL_LENGTH('dbo.CodeProjectVmProvisioning', 'IntegrityFlagsJson') IS NULL
    ALTER TABLE CodeProjectVmProvisioning ADD IntegrityFlagsJson nvarchar(max) NULL;   -- [{atUtc, code, detail}] shown to reviewers
GO

CREATE OR ALTER PROCEDURE usp_SetCodeProjectVmDeadline
    @Handle varchar(64),
    @DeadlineUtc datetime2
AS
BEGIN
    SET NOCOUNT ON;
    -- Set once: a second handover call (reconnect) must not extend the slot.
    UPDATE CodeProjectVmProvisioning SET DeadlineUtc = @DeadlineUtc, UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Handle = @Handle AND DeadlineUtc IS NULL;
END
GO

-- Machines a candidate may be connected to right now, for the session monitor.
CREATE OR ALTER PROCEDURE usp_GetReadyCodeProjectVms
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Handle, VmName, ResourceGroupName, CandidateUserName, DeadlineUtc, LockedAtUtc, SeenSessionIdsJson, IntegrityFlagsJson
    FROM CodeProjectVmProvisioning
    WHERE Step = 'ready';
END
GO

CREATE OR ALTER PROCEDURE usp_UpdateCodeProjectVmMonitor
    @Handle varchar(64),
    @SeenSessionIdsJson nvarchar(max) = NULL,
    @IntegrityFlagsJson nvarchar(max) = NULL,
    @MarkLocked bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectVmProvisioning
    SET SeenSessionIdsJson = ISNULL(@SeenSessionIdsJson, SeenSessionIdsJson),
        IntegrityFlagsJson = ISNULL(@IntegrityFlagsJson, IntegrityFlagsJson),
        LockedAtUtc = CASE WHEN @MarkLocked = 1 AND LockedAtUtc IS NULL THEN SYSUTCDATETIME() ELSE LockedAtUtc END,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Handle = @Handle;
END
GO

-- Handles still tracked (not released), for the nightly orphan sweep.
CREATE OR ALTER PROCEDURE usp_GetLiveCodeProjectVmHandles
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Handle FROM CodeProjectVmProvisioning WHERE Step <> 'released';
END
GO

/* Verification */
SELECT 'VMProperties.Purpose' AS [Object],
       CASE WHEN COL_LENGTH('dbo.VMProperties', 'Purpose') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS Status
UNION ALL SELECT 'CodeProjectVmProvisioning',
       CASE WHEN OBJECT_ID(N'CodeProjectVmProvisioning') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'usp_GetAllVMs excludes candidate machines',
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID(N'usp_GetAllVMs')) LIKE '%CodeProject%' THEN 'OK' ELSE 'STALE - candidate VMs visible to proctors' END;
GO

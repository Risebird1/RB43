/* =====================================================================
   Code project one-click infrastructure setup (Risebird.AzureTimerFunctions,
   CodeProjects/Setup). Idempotent. The setup function applies this script
   itself before anything else, then the two earlier code project migrations.

   CodeProjectSettings holds what the setup discovers or creates (resource
   group, subnet, Bastion, runner jobs, functions URL/key). Both apps read
   configuration first and fall back to this table, so no appsettings edit
   is needed after setup.
   ===================================================================== */

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodeProjectSettings')
BEGIN
    CREATE TABLE [dbo].[CodeProjectSettings](
        [Name]         [varchar](150)  NOT NULL,   -- same path as the configuration key, e.g. CodeProjects:Azure:SubnetId
        [Value]        [nvarchar](max) NULL,       -- encrypted with IEncryptionHelper when IsSecret = 1
        [IsSecret]     [bit]           NOT NULL CONSTRAINT DF_CodeProjectSettings_IsSecret DEFAULT 0,
        [UpdatedAtUtc] [datetime2]     NOT NULL CONSTRAINT DF_CodeProjectSettings_Updated DEFAULT SYSUTCDATETIME(),
     CONSTRAINT [PK_CodeProjectSettings] PRIMARY KEY CLUSTERED ([Name] ASC)
    ) ON [PRIMARY];
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodeProjectInfraSetupRuns')
BEGIN
    CREATE TABLE [dbo].[CodeProjectInfraSetupRuns](
        [Id]             [int] IDENTITY(1,1) NOT NULL,
        [Status]         [varchar](20)   NOT NULL,   -- running | completed | failed | blocked
        [StateJson]      [nvarchar](max) NULL,
        [StartedBy]      [varchar](150)  NULL,
        [StartedAtUtc]   [datetime2]     NOT NULL CONSTRAINT DF_CodeProjectInfraSetupRuns_Started DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc]   [datetime2]     NOT NULL CONSTRAINT DF_CodeProjectInfraSetupRuns_Updated DEFAULT SYSUTCDATETIME(),
        [CompletedAtUtc] [datetime2]     NULL,
        [LeaseUntilUtc]  [datetime2]     NULL,
     CONSTRAINT [PK_CodeProjectInfraSetupRuns] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY];
END
GO

CREATE OR ALTER PROCEDURE usp_GetCodeProjectSettings
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Name, Value, IsSecret FROM CodeProjectSettings;
END
GO

CREATE OR ALTER PROCEDURE usp_SetCodeProjectSetting
    @Name varchar(150),
    @Value nvarchar(max),
    @IsSecret bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    MERGE CodeProjectSettings AS t
    USING (SELECT @Name AS Name) AS s ON t.Name = s.Name
    WHEN MATCHED THEN UPDATE SET Value = @Value, IsSecret = @IsSecret, UpdatedAtUtc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT (Name, Value, IsSecret) VALUES (@Name, @Value, @IsSecret);
END
GO

-- Resumes the latest unfinished run, or starts a new one. Returns the run id.
CREATE OR ALTER PROCEDURE usp_StartCodeProjectInfraSetup
    @StartedBy varchar(150) = NULL,
    @InitialStateJson nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id int = (SELECT TOP 1 Id FROM CodeProjectInfraSetupRuns WITH (UPDLOCK, HOLDLOCK)
                       WHERE Status IN ('running', 'failed', 'blocked') ORDER BY Id DESC);
    IF @Id IS NOT NULL
    BEGIN
        UPDATE CodeProjectInfraSetupRuns SET Status = 'running', UpdatedAtUtc = SYSUTCDATETIME(), LeaseUntilUtc = NULL WHERE Id = @Id;
        SELECT @Id AS Id;
        RETURN;
    END
    INSERT INTO CodeProjectInfraSetupRuns (Status, StateJson, StartedBy) VALUES ('running', @InitialStateJson, @StartedBy);
    SELECT CAST(SCOPE_IDENTITY() AS int) AS Id;
END
GO

CREATE OR ALTER PROCEDURE usp_GetLatestCodeProjectInfraSetup
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 Id, Status, StateJson, StartedBy, StartedAtUtc, UpdatedAtUtc, CompletedAtUtc
    FROM CodeProjectInfraSetupRuns ORDER BY Id DESC;
END
GO

CREATE OR ALTER PROCEDURE usp_LeaseCodeProjectInfraSetup
    @Id int,
    @LeaseSeconds int = 180
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectInfraSetupRuns
    SET LeaseUntilUtc = DATEADD(second, @LeaseSeconds, SYSUTCDATETIME())
    WHERE Id = @Id AND Status = 'running'
      AND (LeaseUntilUtc IS NULL OR LeaseUntilUtc < SYSUTCDATETIME());
    SELECT @@ROWCOUNT AS Leased;
END
GO

CREATE OR ALTER PROCEDURE usp_SaveCodeProjectInfraSetup
    @Id int,
    @Status varchar(20),
    @StateJson nvarchar(max),
    @ReleaseLease bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE CodeProjectInfraSetupRuns
    SET Status = @Status,
        StateJson = @StateJson,
        UpdatedAtUtc = SYSUTCDATETIME(),
        CompletedAtUtc = CASE WHEN @Status = 'completed' THEN SYSUTCDATETIME() ELSE CompletedAtUtc END,
        LeaseUntilUtc = CASE WHEN @ReleaseLease = 1 THEN NULL ELSE LeaseUntilUtc END
    WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE usp_UpsertStackImage
    @Code varchar(50),
    @DisplayName varchar(200),
    @Toolchain varchar(500),
    @GalleryImageId varchar(500),
    @OsType varchar(25)
AS
BEGIN
    SET NOCOUNT ON;
    MERGE StackImages AS t
    USING (SELECT @Code AS Code) AS s ON t.Code = s.Code
    WHEN MATCHED THEN UPDATE SET DisplayName = @DisplayName, Toolchain = @Toolchain, GalleryImageId = @GalleryImageId, OsType = @OsType, IsActive = 1
    WHEN NOT MATCHED THEN INSERT (Code, DisplayName, Toolchain, GalleryImageId, OsType, IsActive)
                          VALUES (@Code, @DisplayName, @Toolchain, @GalleryImageId, @OsType, 1);
END
GO

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SetCodeProjectSetting'))
BEGIN
    DROP PROCEDURE dbo.usp_SetCodeProjectSetting
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_SetCodeProjectSetting
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

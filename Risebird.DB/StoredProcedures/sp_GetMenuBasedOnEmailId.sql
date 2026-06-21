IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_GetMenuBasedOnEmailId'))
BEGIN
    DROP PROCEDURE dbo.sp_GetMenuBasedOnEmailId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[sp_GetMenuBasedOnEmailId]   
  @EmailId nvarchar(300),
  @SessionId varchar(300)
AS  
BEGIN  
    SET NOCOUNT ON;

    DECLARE @lastLogin DATETIME;
    DECLARE @profile   VARCHAR(50);
    DECLARE @roleId    INT = 0;
    DECLARE @clientId  INT = 0;
    DECLARE @userId    INT;
    DECLARE @Now       DATETIME = GETDATE();

    SELECT
        @userId    = ul.id,
        @lastLogin = ul.LastLogin,
        @profile   = pr.[Name],
        @roleId    = ISNULL(cu.RoleId, 0),
        @clientId  = ISNULL(cu.ClientId, 0)
    FROM dbo.UserList ul
    INNER JOIN dbo.[Profile] pr ON pr.id = ul.idProfile
    LEFT JOIN dbo.ClientUser cu ON cu.UserId = ul.id
    WHERE ul.EmailId = @EmailId
      AND ul.IsActive = 1;

    IF @userId IS NOT NULL
       AND NOT EXISTS (
           SELECT 1
           FROM dbo.UserSessions usn
           INNER JOIN dbo.UserList us ON us.id = usn.UserId
           WHERE usn.SessionId = @SessionId
             AND usn.IsActive = 1
             AND us.IsActive = 1
       )
    BEGIN
        INSERT INTO dbo.UserSessions (UserId, SessionId, IsActive)
        VALUES (@userId, @SessionId, 1);
    END

    IF @userId IS NOT NULL
    BEGIN
        UPDATE dbo.UserList
        SET LastLogin = @Now,
            Isonline  = 1
        WHERE id = @userId;
    END
	
    IF (@EmailId != 'ExternalUser')
    BEGIN
        IF (@profile = 'Interviewer')
        BEGIN
            SELECT fn.functionname, fn.description, fn.idparentfunction, fn.sicon, fn.surl, fn.idisplayorder, pl.Name profileName,
                pf.dAccess, @lastLogin AS NavigationShow, c.LogoLink
            FROM dbo.[Function] fn
            JOIN dbo.ProfileFunction pf ON fn.id = pf.idfunction
            JOIN dbo.[Profile] pl ON pl.id = pf.idprofile
            JOIN dbo.UserList ul ON pl.id = ul.idprofile
            LEFT JOIN dbo.ClientUser cu ON cu.UserId = ul.id
            LEFT JOIN dbo.Client c ON c.id = cu.ClientId
            WHERE ul.EmailId = @EmailId AND ul.IsActive = 1
            ORDER BY idisplayorder;
        END
        ELSE IF (@profile = 'Client' AND @roleId = 4) -- staffing agency
        BEGIN
            SELECT fn.functionname, fn.description, fn.idparentfunction, fn.sicon, fn.surl, fn.idisplayorder, pl.Name profileName,
                pf.dAccess, @lastLogin AS NavigationShow, c.LogoLink
            FROM dbo.[Function] fn
            JOIN dbo.ProfileFunction pf ON fn.id = pf.idfunction
            JOIN dbo.[Profile] pl ON pl.id = pf.idprofile
            JOIN dbo.UserList ul ON pl.id = ul.idprofile
            LEFT JOIN dbo.ClientUser cu ON cu.UserId = ul.id
            LEFT JOIN dbo.Client c ON c.id = cu.ClientId
            WHERE ul.EmailId = @EmailId AND ul.IsActive = 1 AND fn.IsStaffingAgencyAccess = 1
            ORDER BY idisplayorder;
        END
        ELSE IF (@profile = 'Client')
        BEGIN
            SELECT fn.functionname, fn.description, fn.idparentfunction, fn.sicon, fn.surl, fn.idisplayorder, pl.Name profileName,
                pf.dAccess, @lastLogin AS NavigationShow, c.LogoLink
            FROM dbo.[Function] fn
            JOIN dbo.ProfileFunction pf ON fn.id = pf.idfunction
            JOIN dbo.[Profile] pl ON pl.id = pf.idprofile
            JOIN dbo.UserList ul ON pl.id = ul.idprofile
            LEFT JOIN dbo.ClientUser cu ON cu.UserId = ul.id
            LEFT JOIN dbo.Client c ON c.id = cu.ClientId
            WHERE ul.EmailId = @EmailId AND ul.IsActive = 1
            ORDER BY idisplayorder;
        END
        ELSE
        BEGIN
            SELECT fn.functionname, fn.description, fn.idparentfunction, fn.sicon, fn.surl, fn.idisplayorder, pl.Name profileName,
                pf.dAccess, @lastLogin AS NavigationShow, c.LogoLink
            FROM dbo.[Function] fn
            JOIN dbo.ProfileFunction pf ON fn.id = pf.idfunction
            JOIN dbo.[Profile] pl ON pl.id = pf.idprofile
            JOIN dbo.UserList ul ON pl.id = ul.idprofile
            LEFT JOIN dbo.ClientUser cu ON cu.UserId = ul.id
            LEFT JOIN dbo.Client c ON c.id = cu.ClientId
            WHERE ul.EmailId = @EmailId AND ul.IsActive = 1
            ORDER BY idisplayorder;
        END
    END
    ELSE
    BEGIN
        SELECT DISTINCT fn.functionname, fn.description, fn.idparentfunction, fn.sicon, fn.surl, fn.idisplayorder, pl.Name profileName,
            pf.dAccess, @lastLogin AS NavigationShow
        FROM dbo.[Function] fn
        JOIN dbo.ProfileFunction pf ON fn.id = pf.idfunction
        JOIN dbo.[Profile] pl ON pl.id = pf.idprofile
        JOIN dbo.UserList ul ON pl.id = ul.idprofile
        WHERE pl.Name = 'ExternalUser';
    END
END
GO



-- Seed Admin -> WhatsApp campaigns menu (/WhatsAppCampaign) for the profiles WhatsAppCampaignController
-- allows: Admin, SupportAdmin, SeniorHR, HR. The side menu lists every Function whose parent is the Admin
-- menu (idParentFunction = 3) under Admin.
-- Safe to re-run: skips the insert when the URL already exists and grants each profile once.

SET NOCOUNT ON;

DECLARE @adminMenuId INT = 3;
DECLARE @campaignsId INT;

IF NOT EXISTS (SELECT 1 FROM dbo.[Function] WHERE id = @adminMenuId)
BEGIN
    PRINT 'Admin menu (Function id 3) not found - nothing seeded.';
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.[Function] WHERE sURL = '/WhatsAppCampaign')
BEGIN
    INSERT INTO dbo.[Function] (FunctionName, Description, idParentFunction, sIcon, sURL, iDisplayOrder, IsCommon)
    VALUES ('WhatsApp campaigns', 'Send schedule / reschedule / confirmation WhatsApp templates to candidate lists', @adminMenuId, 'fab fa-whatsapp', '/WhatsAppCampaign', 95, 1);
END

SELECT @campaignsId = id FROM dbo.[Function] WHERE sURL = '/WhatsAppCampaign';

INSERT INTO dbo.ProfileFunction (idprofile, idfunction, dAccess)
SELECT p.id, @campaignsId, 1
FROM dbo.[Profile] p
WHERE p.[Name] IN ('Admin', 'SupportAdmin', 'SeniorHR', 'HR')
  AND NOT EXISTS (
        SELECT 1 FROM dbo.ProfileFunction pf
        WHERE pf.idprofile = p.id AND pf.idfunction = @campaignsId
  );

-- The item shows under the Admin menu, so a profile without the Admin menu itself does not see it in the
-- side menu; it still reaches the page from the links on the PlivoCX campaigns, Not Scheduled, Reschedule
-- and Not Confirmed pages.

PRINT 'Admin -> WhatsApp campaigns menu seed complete. Function id: ' + ISNULL(CAST(@campaignsId AS VARCHAR(20)), 'N/A');
GO

-- Seeds the running costs known when the Interview Profitability grid was built.
-- Safe to re-run: skips a factor whose Name already exists.
--
-- EffectiveFrom is the start of FY 2026-27 so the grid charges these costs to every interview this
-- financial year. If the figures were different earlier, end-date these rows and add the older
-- figures as separate rows rather than editing these.

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.OperationalCostFactor WHERE [Name] = 'Azure cloud hosting')
BEGIN
    INSERT INTO dbo.OperationalCostFactor (Name, Category, Amount, Currency, Basis, EffectiveFrom, Notes, CreatedBy)
    VALUES ('Azure cloud hosting', 'Cloud', 20000.00, 'INR', 'PerDay', '2026-04-01',
            'Average daily Azure bill (app services, SQL, storage, VMs, functions).', 'seed');
END

IF NOT EXISTS (SELECT 1 FROM dbo.OperationalCostFactor WHERE [Name] = 'SendGrid email')
BEGIN
    INSERT INTO dbo.OperationalCostFactor (Name, Category, Amount, Currency, Basis, EffectiveFrom, Notes, CreatedBy)
    VALUES ('SendGrid email', 'Email', 3000.00, 'INR', 'PerDay', '2026-04-01',
            'Average daily SendGrid cost for interview invites, reminders and reports.', 'seed');
END

PRINT 'Operational cost factor seed complete.';
GO

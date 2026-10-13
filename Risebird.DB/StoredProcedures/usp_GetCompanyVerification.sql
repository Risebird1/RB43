IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCompanyVerification'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCompanyVerification
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: the cached registry lookup (and blocklist entry) for one employer.
Create PROCEDURE [dbo].[usp_GetCompanyVerification]
@NormalizedName nvarchar(300)
AS
Begin
	SET NOCOUNT ON;

	select NormalizedName, DisplayName, Found, CIN, [Status], IncorporationDate, StruckOffDate, EmployeeBand,
	Website, DomainAgeMonths, [Source], CheckedOn, IsBlocklisted, BlocklistReason, BlocklistedBy, BlocklistedOn
	from CompanyVerification
	where NormalizedName = @NormalizedName
End
GO

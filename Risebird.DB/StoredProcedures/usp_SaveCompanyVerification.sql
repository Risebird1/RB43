IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveCompanyVerification'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCompanyVerification
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Advanced Resume Screening: upserts a registry lookup for one employer. The blocklist columns
-- change only when @SetBlocklist = 1, so a routine re-lookup can never un-blocklist a company.
Create PROCEDURE [dbo].[usp_SaveCompanyVerification]
@NormalizedName nvarchar(300),
@DisplayName nvarchar(300) = null,
@Found bit = null,
@CIN varchar(30) = null,
@Status varchar(50) = null,
@IncorporationDate date = null,
@StruckOffDate date = null,
@EmployeeBand varchar(30) = null,
@Website varchar(300) = null,
@DomainAgeMonths int = null,
@Source varchar(100) = null,
@SetBlocklist bit = 0,
@IsBlocklisted bit = 0,
@BlocklistReason nvarchar(1000) = null,
@BlocklistedBy varchar(200) = null
AS
Begin
	SET NOCOUNT ON;

	if not exists (select 1 from CompanyVerification where NormalizedName = @NormalizedName)
	Begin
		insert into CompanyVerification (NormalizedName, DisplayName, IsBlocklisted)
		values (@NormalizedName, @DisplayName, 0)
	End

	if (@SetBlocklist = 1)
	Begin
		update CompanyVerification set
			IsBlocklisted = ISNULL(@IsBlocklisted, 0),
			BlocklistReason = @BlocklistReason,
			BlocklistedBy = @BlocklistedBy,
			BlocklistedOn = GETUTCDATE(),
			DisplayName = ISNULL(DisplayName, @DisplayName)
		where NormalizedName = @NormalizedName
	End
	else
	Begin
		update CompanyVerification set
			DisplayName = ISNULL(@DisplayName, DisplayName),
			Found = @Found, CIN = @CIN, [Status] = @Status, IncorporationDate = @IncorporationDate,
			StruckOffDate = @StruckOffDate, EmployeeBand = @EmployeeBand, Website = @Website,
			DomainAgeMonths = @DomainAgeMonths, [Source] = @Source, CheckedOn = GETUTCDATE()
		where NormalizedName = @NormalizedName
	End
End
GO

/****** Object:  StoredProcedure [dbo].[usp_UpdateSupportHR]    Script Date: 04-03-2020 3.35.33 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSupportBasedOnProfile'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSupportBasedOnProfile
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetSupportBasedOnProfile] 
@emailId varchar(100) 

AS
BEGIN

if(@emailId is not null)
Begin
declare @profileId int
set @profileId = (select idProfile from UserList where EmailId = @emailId)

if(@profileId = 3 or @profileId = 4 or @profileId = 23)
Begin

Select st.id,st.Name,st.EmailId,st.MobileNumber,
st.WhatsappNumber, st.OnVoiceCall, st.OnWhatsapp, st.OnEmail, st.AvailabilityHours, 1 as IsOnline, st.applicableToProfileId 
from support st
where st.applicableToProfileId is null or st.applicableToProfileId = @profileId

End
Else
Begin

Select st.id,st.Name,st.EmailId,st.MobileNumber,
st.WhatsappNumber, st.OnVoiceCall, st.OnWhatsapp, st.OnEmail, st.AvailabilityHours, 1 as IsOnline, st.applicableToProfileId 
from support st


End
END

Else
Begin
Select st.id,st.Name,st.EmailId,st.MobileNumber,
st.WhatsappNumber, st.OnVoiceCall, st.OnWhatsapp, st.OnEmail, st.AvailabilityHours, 1 as IsOnline, st.applicableToProfileId 
from support st
where st.applicableToProfileId is null
END


 
END
GO



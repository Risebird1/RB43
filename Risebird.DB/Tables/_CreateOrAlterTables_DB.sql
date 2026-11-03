PRINT 'Start: Updating tables...'

PRINT 'Table Function...'
:r $(path)\Tables\Function.sql

PRINT 'Table Profile...'
:r $(path)\Tables\Profile.sql
PRINT 'Table InterviewMeetings...'
:r $(path)\Tables\InterviewMeetings.sql
PRINT 'Table UserList...'
:r $(path)\Tables\UserList.sql
PRINT 'Table SentWhatsAppMessageDetails...'
:r $(path)\Tables\SentWhatsAppMessageDetails.sql
PRINT 'Table WhatsAppOptOut...'
:r $(path)\Tables\WhatsAppOptOut.sql
PRINT 'Table WhatsAppCampaign...'
:r $(path)\Tables\WhatsAppCampaign.sql
PRINT 'Table WhatsAppCampaignCandidate...'
:r $(path)\Tables\WhatsAppCampaignCandidate.sql

PRINT 'End: Updating tables...'
GO

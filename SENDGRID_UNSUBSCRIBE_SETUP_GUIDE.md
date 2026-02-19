# SendGrid Unsubscribe Feature Implementation Guide

## Overview

This document explains how to complete the setup of SendGrid's unsubscribe functionality for Risebird email notifications. The code has been updated to support unsubscribe groups, but you need to configure the unsubscribe group in SendGrid first.

## What Has Been Implemented

✅ **Code Changes:**
1. Added `UnsubscribeGroupId` configuration setting to `appsettings.json`
2. Updated `NotificationHelper.cs` to read the unsubscribe group ID
3. Modified the `SendMail` method to automatically attach unsubscribe functionality to all outgoing emails

## What You Need to Do

### Step 1: Create an Unsubscribe Group in SendGrid

1. **Log in to SendGrid:**
   - Go to https://app.sendgrid.com/
   - Log in with your SendGrid account credentials

2. **Navigate to Suppression Management:**
   - Click on **Settings** in the left sidebar
   - Select **Sender Authentication** OR navigate directly to **Email API** > **Suppressions** > **Unsubscribe Groups**
   - Or directly visit: https://app.sendgrid.com/suppressions/advanced_suppression_manager

3. **Create a New Unsubscribe Group:**
   - Click **"Create New Group"** or **"+ Add Unsubscribe Group"**
   - Fill in the following details:
     - **Group Name:** `Risebird Notifications` (or your preferred name)
     - **Group Description:** `All email notifications from Risebird including interview alerts, candidate invites, and system notifications`
     - **Display Name on Email:** Check this box to make it visible to recipients
   - Click **"Save"**

4. **Note the Group ID:**
   - After creating the group, you'll see it in the list
   - Note the **Group ID** (it will be a number like `12345`)

### Step 2: Update Configuration Files

Update the `UnsubscribeGroupId` in your configuration files with the Group ID from Step 1:

**File: `Risebird.Web/appsettings.json`**
```json
"SendGridKeySettings": {
  "UserName": "azure_9119f7b456399652d5a99faa61a6fbe7@azure.com",
  "Password": "rise_123",
  "MailFrom": "notification@risebird.io",
  "ApiKey": "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",
  "UnsubscribeGroupId": 12345  // Replace with your actual Group ID
}
```

**Do the same for:**
- `Risebird.Services/appsettings.json`
- Any other `appsettings.json` files in your solution

### Step 3: Add Unsubscribe Link to Email Templates (Optional but Recommended)

SendGrid will automatically add an unsubscribe link at the bottom of emails, but you can also add a custom link in your HTML templates.

#### Option A: Use SendGrid's ASM Tags (Recommended)

Add this to the footer of your email templates stored in Azure Blob Storage:

```html
<div style="text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666;">
  <p>
    <a href="<%asm_group_unsubscribe_raw_url%>" style="color: #666; text-decoration: underline;">Unsubscribe</a> | 
    <a href="<%asm_preferences_raw_url%>" style="color: #666; text-decoration: underline;">Email Preferences</a>
  </p>
</div>
```

**Email Templates to Update:**
- `EmergencyInterviewNotificationV2.html`
- `InviteCandidateTemplateV2.html`
- `InterviewAssingmentMailTemplate.html`
- `CancelInviteCandidateTemplate.html`
- `ResumeEvaluationReminderTemplate.html`
- Any other email templates you use

#### Option B: Let SendGrid Handle It Automatically

If you set `UnsubscribeGroupId` in the code (which we did), SendGrid will automatically add an unsubscribe footer to all emails. However, adding a custom link (Option A) gives you more control over the appearance.

### Step 4: Test the Implementation

1. **Deploy your changes** to a test/staging environment first
2. **Send a test email** using any of your email functions
3. **Check the received email:**
   - You should see an unsubscribe link at the bottom
   - Click on it to verify it takes you to SendGrid's unsubscribe page
4. **Verify in SendGrid:**
   - Go to **Suppressions** > **Unsubscribe Groups**
   - Click on your group to see if unsubscribes are being tracked

### Step 5: Monitor and Manage Unsubscribes

**View Unsubscribes:**
- In SendGrid, go to **Suppressions** > **Unsubscribe Groups**
- Click on your group to see who has unsubscribed
- You can manually add/remove emails from the suppression list

**Re-enable Someone Who Unsubscribed:**
- Go to the unsubscribe group
- Find their email and click the "X" or "Remove" button

## Benefits of This Implementation

✅ **Compliance:** Meets CAN-SPAM Act and GDPR requirements  
✅ **Deliverability:** Improves sender reputation and email deliverability  
✅ **Credibility:** Shows professionalism and respect for recipients  
✅ **User Control:** Allows recipients to manage their preferences  
✅ **Automatic Management:** SendGrid handles the suppression list automatically  

## How It Works

1. When an email is sent through `SendMail()`, the code automatically attaches the unsubscribe group ID
2. SendGrid adds an unsubscribe link to the email
3. When a recipient clicks "Unsubscribe", they're added to the suppression list
4. Future emails to that address will be automatically suppressed by SendGrid
5. The suppression is logged in SendGrid's dashboard for your review

## Advanced Options (Optional)

### Multiple Unsubscribe Groups

If you want different types of emails to have separate unsubscribe options:

1. Create multiple groups in SendGrid (e.g., "Interview Notifications", "Marketing", "System Alerts")
2. Modify the `SendMail` method to accept an optional `unsubscribeGroupId` parameter
3. Pass different group IDs based on email type

### Custom Unsubscribe Page

You can create a custom unsubscribe landing page:
1. In SendGrid, go to your unsubscribe group settings
2. Enable "Custom Unsubscribe Page"
3. Enter your custom page URL

## Troubleshooting

**Unsubscribe link not appearing:**
- Verify `UnsubscribeGroupId` is set to a valid number (not 0)
- Check that the group ID exists in your SendGrid account
- Ensure you're using the correct SendGrid API key

**Unsubscribes not being tracked:**
- Wait 1 minute after creating a group before sending emails
- Check the group ID is correct
- Verify the SendGrid API key has proper permissions

**Testing the unsubscribe:**
- Use a real email address (not a test one)
- Check your spam folder if the test email doesn't arrive
- Allow a few seconds for SendGrid to process the unsubscribe

## Support Resources

- **SendGrid Suppression Groups Docs:** https://www.twilio.com/docs/sendgrid/for-developers/sending-email/suppressions
- **SendGrid Support:** https://support.sendgrid.com/
- **CAN-SPAM Compliance:** https://www.ftc.gov/tips-advice/business-center/guidance/can-spam-act-compliance-guide-business

## Questions?

If you encounter any issues or have questions about this implementation, please refer to the SendGrid documentation or contact SendGrid support for assistance with setting up unsubscribe groups.

---

**Last Updated:** February 15, 2026  
**Implemented By:** AI Assistant  
**Status:** Code Implementation Complete - Configuration Required

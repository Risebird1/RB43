# Testing Checklist - Interviewer Email Fix

## Pre-Deployment Checks
- [ ] Code compiles without errors
- [ ] No linter errors
- [ ] All unit tests pass (if applicable)
- [ ] Database connection strings are configured
- [ ] Email service (SendGrid) is configured and accessible

## Test Scenarios

### Scenario 1: Normal Flow - All Steps Succeed
**Objective**: Verify email is sent and no duplicates occur

**Steps**:
1. Navigate to Add Interviewer page
2. Fill in all required fields:
   - First Name: Test
   - Last Name: User
   - Email: testuser@example.com
   - Mobile Number: Valid number
   - Primary Specialization: Any valid value
3. Select Assessment Groups
4. Submit the form
5. Select a target skill channel from dropdown
6. Complete onboarding

**Expected Results**:
- ✓ UserList entry created
- ✓ InterviewerProfile entry created
- ✓ ONE acknowledgement email sent to testuser@example.com
- ✓ Skill channel mapping succeeds
- ✓ Success message: "Interviewer onboarded successfully!"
- ✓ No duplicate emails

### Scenario 2: Step 2 Failure - Skill Channel Mapping Fails
**Objective**: Verify email is sent even when skill channel mapping fails

**Steps**:
1. Add a new interviewer (complete Step 1)
2. Note the success message and redirection
3. Attempt to add invalid/null skill channel (or simulate DB error)

**Expected Results**:
- ✓ UserList entry exists
- ✓ InterviewerProfile entry exists
- ✓ Acknowledgement email WAS sent (check email)
- ✓ Error message: "Failed to update the Interviewer"
- ✓ Error logged with details about skill channel failure

**Where to Check Logs**:
```
Look for: "Failed to add target skill channel for interviewer UserId: X, IrfId: Y"
```

### Scenario 3: Email Service Failure
**Objective**: Verify onboarding succeeds even if email fails

**Setup**: Temporarily misconfigure email service or simulate failure

**Steps**:
1. Add a new interviewer
2. Complete both steps

**Expected Results**:
- ✓ UserList entry created
- ✓ InterviewerProfile entry created
- ✓ Skill channel mapping succeeds
- ✓ Success message shown to admin
- ✓ Error logged: "Failed to send acknowledgement email to [email]"
- ✓ Onboarding process NOT blocked by email failure

**Where to Check Logs**:
```
Look for: "Failed to send acknowledgement email to testuser@example.com"
Or: "Acknowledgement email may have failed for interviewer testuser@example.com"
```

### Scenario 4: Update Existing Interviewer - No Email Should Be Sent
**Objective**: Verify email is NOT sent when editing existing interviewer

**Steps**:
1. Navigate to Edit Interviewer page
2. Select an existing interviewer
3. Modify some fields (e.g., mobile number, experience, etc.)
4. Save the changes

**Expected Results**:
- ✓ InterviewerProfile entry updated successfully
- ✓ NO email sent (this is UPDATE, not INSERT)
- ✓ Success message shown
- ✓ Activity log shows "Update Interviewer"

**Where to Check**:
- Check recipient's email inbox - should NOT receive new acknowledgement email
- Check application logs - no email sending attempts should be logged

### Scenario 5: Complete Failure - Invalid Data in Step 1
**Objective**: Verify nothing is created when validation fails

**Steps**:
1. Attempt to add interviewer with:
   - Duplicate email (already exists)
   - Invalid data
   - Missing required fields

**Expected Results**:
- ✓ NO UserList entry created
- ✓ NO InterviewerProfile entry created
- ✓ NO email sent
- ✓ Appropriate error message shown
- ✓ Error logged

## Log Monitoring

### Success Indicators
Look for these in application logs:
```
"Successfully added target skill channel for interviewer UserId: X, IrfId: Y"
```

### Warning Indicators
```
"Acknowledgement email may have failed for interviewer [email]. Result: [result]"
"Failed to add target skill channel for interviewer UserId: X, IrfId: Y. Repository returned false."
```

### Error Indicators
```
"Failed to send acknowledgement email to [email]"
"SQL Exception in AddTargetSkillChannelToInterviewerPreOnboarding for UserId: X, TargetIrfId: Y"
"Exception in AddTargetSkillChannelToInterviewerPreOnboarding for UserId: X, TargetIrfId: Y"
```

## Database Verification Queries

### Check if interviewer was added:
```sql
SELECT TOP 10 * 
FROM UserList 
WHERE EmailId = 'testuser@example.com'
ORDER BY id DESC
```

### Check interviewer profile:
```sql
SELECT ip.*, ul.EmailId, ul.FirstName, ul.LastName
FROM InterviewerProfile ip
INNER JOIN UserList ul ON ip.UserId = ul.id
WHERE ul.EmailId = 'testuser@example.com'
ORDER BY ip.Id DESC
```

### Check if there are interviewers without skill channel mapping:
```sql
-- This query can help identify interviewers who may have been affected by the old bug
-- (Added in Step 1 but Step 2 failed)
SELECT ul.id, ul.EmailId, ul.FirstName, ul.LastName, ip.CreatedOn
FROM UserList ul
INNER JOIN InterviewerProfile ip ON ul.id = ip.UserId
WHERE NOT EXISTS (
    SELECT 1 FROM InterviewerIRFMappings iim 
    WHERE iim.InterviewerId = ul.id
)
AND ip.CreatedOn > '2024-01-01'  -- Adjust date as needed
ORDER BY ip.CreatedOn DESC
```

## Email Verification

### Check Sent Emails:
1. Check recipient inbox for acknowledgement email
2. Verify email contains:
   - Interviewer's name
   - Acknowledgement button/link
   - Correct userId in the link

### Email Link Format:
```
https://portal.risebird.io/Interviewer/InterviewerAcknowledgementResponse?status=1&user=[userId]
```

## Performance Checks
- [ ] Email sending doesn't significantly delay the onboarding process
- [ ] Database operations complete within acceptable time
- [ ] No memory leaks or resource issues

## Rollback Verification
If rollback is needed:
- [ ] Revert files listed in INTERVIEWER_EMAIL_FIX_SUMMARY.md
- [ ] Verify application compiles
- [ ] Test basic add interviewer flow
- [ ] Confirm previous behavior is restored

## Post-Deployment Monitoring (First 48 Hours)

Monitor these metrics:
- [ ] Number of new interviewers added
- [ ] Email delivery success rate
- [ ] Error rate in logs
- [ ] Step 2 (skill channel mapping) failure rate
- [ ] User complaints about missing emails

## Sign-Off

| Role | Name | Date | Status |
|------|------|------|--------|
| Developer | | | |
| QA Tester | | | |
| Product Owner | | | |
| DevOps | | | |

## Notes
_Add any additional notes or observations here_

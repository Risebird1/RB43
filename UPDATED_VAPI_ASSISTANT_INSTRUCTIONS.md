# Updated Vapi Assistant Instructions (Dynamic Client Name + Simplified Dates)

## 🎯 What Changed

### 1. Dynamic Client Name ✅
- Removed hardcoded "SONATA"
- Now uses actual client name from database (fetched via IRF → Client relationship)
- Each call automatically uses the correct client name

### 2. Simplified Date References ✅
- Removed specific date strings (yyyy-mm-dd format)
- Assistant now ONLY says "tomorrow" or "day after tomorrow"
- Eliminates confusion from AI reading out date numbers digit-by-digit

### 3. Natural Date Handling ✅
- Assistant speaks dates naturally ("tomorrow", "day after tomorrow")
- Backend automatically converts to proper date format
- No more "two zero two six dash zero two dash two five" nonsense

---

## 📋 Updated Assistant Instructions for Vapi Dashboard

Copy and paste the following into your Vapi Assistant's **System Instructions** field:

```
You are an outbound interview scheduling assistant from Risebird Interviews calling on behalf of {CLIENT_NAME} for urgent hiring.

IDENTITY & PURPOSE:
- You represent Risebird Interviews on behalf of {CLIENT_NAME}
- This is URGENT and time-sensitive hiring - interviews MUST be scheduled within the next TWO days ONLY
- Your goal: Politely convey urgency and secure a confirmed interview slot within the next two days

CURRENT DATE INFORMATION (CRITICAL):
- Today is: {CURRENT_DAY_OF_WEEK}
- You can ONLY offer: TOMORROW or DAY AFTER TOMORROW
- When speaking to candidate, use the words "tomorrow" or "day after tomorrow" ONLY
- NEVER read out specific dates like numbers (e.g., do NOT say "2026-02-25" or "February 25th")
- IRF ID for this interview: {IRF_ID}
- Candidate email for confirmation: {CANDIDATE_EMAIL}

CRITICAL RULES:
- Interviews MUST be within the next TWO days from today (tomorrow or day after tomorrow ONLY)
- Do NOT allow scheduling beyond 2 days under any circumstances
- When speaking dates: Always say "tomorrow" or "day after tomorrow" - NEVER read out specific date numbers
- If candidate requests later dates: "I apologize, but due to the urgent nature of this hiring process, we can only accommodate slots within the next two days. Would either tomorrow or the day after work for you?"
- If candidate insists on dates beyond 2 days: Politely explain this is not possible and end call gracefully
- ALL times must be confirmed in IST (Indian Standard Time)

CALL FLOW:
1. Greet professionally with warmth
   - "Hello, this is Risebird Interviews calling. Is this a good time to speak briefly?"
   - Wait for confirmation

2. State purpose and ask for name
   - "I'm calling from Risebird on behalf of {CLIENT_NAME} regarding an interview opportunity for the {JOB_TITLE} position."
   - "May I have your full name please?"
   - Wait for candidate to provide their name
   - IMPORTANT: Use the EXACT name the candidate provides, NOT any pre-filled information

3. Explain urgency POLITELY but FIRMLY
   - "I appreciate your time. This is a time-sensitive opportunity and we're looking to schedule interviews within the next two days. We'd really value the chance to speak with you soon."
   - "Would you be available tomorrow or the day after tomorrow for the interview?"
   - Wait for response

4. Get specific time
   - "What time would work best for you?" 
   - If they say just "morning" or "afternoon", ask: "What specific time would be most convenient? Please specify with AM or PM."
   - Wait for specific time (e.g., "10:00 AM", "02:30 PM")

5. Confirm details explicitly with appreciation
   - Repeat EXACT name, date (as "tomorrow" or "day after tomorrow") and time back to candidate
   - "Thank you! Let me just confirm - [CANDIDATE_NAME], your interview with {CLIENT_NAME} is scheduled for [tomorrow/day after tomorrow] at [TIME] IST. Is that correct?"
   - Wait for explicit confirmation ("yes", "correct", "that's right")
   - IMPORTANT: When confirming, say "tomorrow" or "day after tomorrow", do NOT read out date numbers

6. Submit booking (ONLY after explicit confirmation)
   - Call submitInterviewSlot function with:
     * candidateName: The EXACT full name the candidate provided in step 2 (NOT the email or pre-filled value)
     * candidateEmail: {CANDIDATE_EMAIL}
     * interviewDate: Use "tomorrow" if they chose tomorrow, "day after tomorrow" if they chose day after
     * interviewTime: Time in HH:MM AM/PM format (e.g., "10:00 AM", "02:30 PM")
     * timezone: "IST"
     * candidateConfirmed: true (ONLY if candidate said yes/confirmed/correct)
     * irfId: {IRF_ID}

7. End professionally with gratitude
   - "Perfect! Your interview is confirmed. You'll receive a confirmation email shortly. We appreciate your time and look forward to speaking with you. Good luck!"

OBJECTION HANDLING (BE POLITE BUT PERSISTENT):
- If busy now: "I completely understand. This will only take 2 minutes to find a suitable time. May we proceed?"
- If asks to call back: "I appreciate that, and since I have you on the line and this is time-sensitive, could we quickly lock in a time that works for you? It will just take a moment."
- If uncertain about job: "I understand. You had applied for this position, and the company is moving quickly with qualified candidates. Would you like to secure an interview slot?"
- If wants different date beyond 2 days: "I appreciate your preference, but unfortunately, due to the urgent hiring timeline, we can only offer slots within the next two days - either tomorrow or the day after. Would either of those work for you?"

VALIDATION RULES:
- Candidate name MUST be their actual full name (first and last name), NOT an email address or phone number
- Date MUST be either "tomorrow" or "day after tomorrow" - NO EXCEPTIONS
- When SPEAKING to candidate: Use words "tomorrow" or "day after tomorrow" ONLY
- When SUBMITTING to tool: Use "tomorrow" or "day after tomorrow" as the interviewDate value
- Time MUST include AM or PM (e.g., "10:00 AM", not just "10:00")
- Candidate MUST explicitly confirm with "yes", "correct", "confirmed", "that's right", or similar
- If candidate says "maybe", "I think so", "probably" - ask for explicit confirmation: "I want to make sure we have this right. Can you confirm this time works for you?"
- Do NOT submit booking without explicit confirmation
- ALWAYS include the IRF ID ({IRF_ID}) and candidate email ({CANDIDATE_EMAIL}) when submitting the slot

NEVER:
- Use email addresses or phone numbers as the candidate name
- Accept dates beyond 2 days from today (be firm but polite about this)
- Submit booking without explicit confirmation
- Use 24-hour time format (always use AM/PM)
- Forget to specify timezone as IST
- Read out specific date numbers when speaking (e.g., NEVER say "2026-02-25" or "February 25th, 2026")
- Use pre-filled or template values for candidateName - always use what the candidate tells you
- Forget to include irfId ({IRF_ID}) and candidate email ({CANDIDATE_EMAIL}) in the tool call
```

---

## 🔄 How Dynamic Values Work

The system now automatically injects these values into the assistant's context:

| Placeholder | Source | Example Value |
|-------------|--------|---------------|
| `{CLIENT_NAME}` | `Client.Name` (via IRF.ClientId) | "Microsoft", "Google", "Amazon", etc. |
| `{CURRENT_DAY_OF_WEEK}` | System-generated | "Tuesday, February 24, 2026" |
| `{JOB_TITLE}` | `IRF.IRFId` | "Senior .NET Developer" |
| `{IRF_ID}` | `Candidate.IRFId` | "123" |
| `{CANDIDATE_EMAIL}` | `UserList.EmailId` | "john@example.com" |

**Key Point**: The assistant receives these values already filled in. It doesn't see `{CLIENT_NAME}` - it sees the actual client name like "Microsoft".

---

## 📝 What to Update in Vapi Dashboard

### Option 1: Update Existing Assistant (Recommended)

1. Log into your Vapi dashboard
2. Navigate to your existing assistant (ID: from your config)
3. Update the **System Instructions** field with the text above
4. Save the assistant

### Option 2: Create New Assistant

1. Create a new assistant in Vapi dashboard
2. Set name: "Interview Scheduler - Dynamic"
3. Paste the instructions above into **System Instructions**
4. Configure the tool: `submitInterviewSlot`
5. Copy the new assistant ID
6. Update config: `Vapi:InterviewBookingAssistantId`

---

## ✅ Key Improvements

### Before (Problems)

❌ **Hardcoded Client**: "calling on behalf of SONATA"
   - Problem: All calls said "SONATA" even for different clients
   
❌ **Date Numbers**: "Tomorrow is: 2026-02-25"
   - Problem: AI reads "two zero two six dash zero two dash two five"
   - Confusing and robotic

❌ **Tool Date Format**: "interviewDate: Date in YYYY-MM-DD format"
   - Problem: Forced specific date strings in tool calls

### After (Solutions)

✅ **Dynamic Client**: "calling on behalf of {ClientName}"
   - Solution: Each call uses correct client name from database
   
✅ **Natural Dates**: "You can ONLY offer: TOMORROW or DAY AFTER TOMORROW"
   - Solution: AI says "tomorrow" or "day after tomorrow" naturally
   - No more number-reading confusion

✅ **Flexible Tool Format**: "Use 'tomorrow' or 'day after tomorrow'"
   - Solution: Backend handles date conversion
   - AI just passes simple strings

---

## 🎯 Expected Behavior

### When Assistant Speaks:

**Old Way** ❌:
> "Your interview is scheduled for two zero two six dash zero two dash two five at 10 AM"

**New Way** ✅:
> "Your interview is scheduled for tomorrow at 10 AM"

### Multiple Clients:

**Candidate 1** (Applied to Microsoft):
> "I'm calling from Risebird on behalf of Microsoft..."

**Candidate 2** (Applied to Google):
> "I'm calling from Risebird on behalf of Google..."

**Candidate 3** (Applied to Amazon):
> "I'm calling from Risebird on behalf of Amazon..."

All automatic! ✨

---

## 🧪 Testing the Changes

### Test URL:
```
https://localhost:5001/Dashboard/Index?testVapiCalls=true
```

### What to Listen For:

1. **Client Name**: 
   - Listen for "calling on behalf of [CLIENT_NAME]"
   - Should match the actual client in your database

2. **Date Mentions**:
   - Should hear "tomorrow" or "day after tomorrow"
   - Should NOT hear "2026" or "02" or "25" or date numbers

3. **Confirmation**:
   - "Your interview with [CLIENT_NAME] is scheduled for tomorrow at [TIME]"
   - Clear, natural, no confusion

---

## 📊 Database Changes

### Stored Procedure Updated

Added Client join and ClientName field:

```sql
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id
INNER JOIN Client cl ON irf.ClientId = cl.id  -- NEW JOIN
...
SELECT ...
    cl.Name AS ClientName,  -- NEW FIELD
    ...
```

### View Model Updated

Added ClientName property:

```csharp
public class VapiEligibleCandidateVM
{
    ...
    public string ClientName { get; set; }  // NEW
    ...
}
```

---

## 🔍 Verification Steps

### 1. Check Stored Procedure Returns Client Name

```sql
EXEC usp_GetEligibleCandidatesForVapiCalls @Count = 5;

-- Should now include ClientName column
-- Verify client names are correct
```

### 2. Check Test Displays Client Name

Run test URL and verify output shows:
```
📞 Processing: John Doe
   Client: Microsoft  ← Should show actual client name
   Email: john@example.com
   ...
```

### 3. Check Vapi Call Logs

After making test calls:
- Log into Vapi dashboard
- Check call transcript
- Verify assistant says client name, not "SONATA"
- Verify assistant says "tomorrow"/"day after tomorrow", not date numbers

---

## 🎊 Summary of Changes

| Change | Before | After | Impact |
|--------|--------|-------|--------|
| **Client Name** | Hardcoded "SONATA" | Dynamic from DB | ✅ Correct client per IRF |
| **Date Speaking** | Reads digits | Says "tomorrow" | ✅ Natural, clear |
| **Date Format** | yyyy-mm-dd | "tomorrow"/"day after" | ✅ Simple |
| **Database Query** | No client join | Includes client join | ✅ Client name available |
| **View Model** | No ClientName | Has ClientName | ✅ Passed to prompt |

---

## 📁 Files Modified (7 total)

1. ✅ `usp_GetEligibleCandidatesForVapiCalls.sql` - Added Client join and ClientName field
2. ✅ `VapiEligibleCandidateVM.cs` - Added ClientName property
3. ✅ `VapiInterviewSchedulingJob.cs` - Updated prompt generation (dynamic client, no dates)
4. ✅ `DashboardController.cs` - Updated prompt generation (dynamic client, no dates)
5. ✅ `VapiInterviewCallController.cs` - Updated prompt generation (dynamic client, no dates)
6. ✅ `VapiTestResults.cshtml` - Already supports client name display
7. ✅ `UPDATED_VAPI_ASSISTANT_INSTRUCTIONS.md` - This document!

---

## 🚀 Action Items

### For You:

1. **Run Database Migration** (if needed):
   ```sql
   -- Check if CandidateMasterId exists in Candidate table
   -- If not, run: Add_CandidateMasterId_To_Candidate.sql
   
   -- Update stored procedure
   -- Run: usp_GetEligibleCandidatesForVapiCalls.sql
   ```

2. **Test the Code**:
   ```
   https://localhost:5001/Dashboard/Index?testVapiCalls=true
   ```

3. **Update Vapi Dashboard**:
   - Copy the instructions above (between the ``` blocks)
   - Paste into your Vapi Assistant's System Instructions
   - Replace placeholders with actual format: `{{clientName}}` `{{irfId}}` etc.
   - Save

4. **Verify**:
   - Make a test call
   - Listen for correct client name
   - Listen for "tomorrow"/"day after tomorrow" (not date numbers)

---

## 🎯 Final Instructions for Vapi Dashboard

**IMPORTANT**: When pasting into Vapi dashboard, the system automatically injects values from `VariableValues`. The placeholders in the instructions should match your variable names.

### If Using VariableValues:

Our code sends:
```csharp
VariableValues = new Dictionary<string, object>
{
    { "irfId", candidate.IRFId.ToString() },
    { "candidateEmail", candidate.CandidateEmail }
}
```

But we don't send clientName as a variable - it's injected into the system prompt directly by our code before sending.

### So the instructions are already personalized per call!

Each call gets a unique system prompt with:
- Real client name (e.g., "Microsoft", not "{clientName}")
- Current day of week
- Real IRF ID
- Real candidate email

The assistant just needs to follow the instructions!

---

## 📞 Example Call Flow (After Changes)

**Assistant**: "Hello, this is Risebird Interviews calling. Is this a good time to speak briefly?"

**Candidate**: "Yes"

**Assistant**: "I'm calling from Risebird on behalf of **Microsoft** regarding an interview opportunity for the Senior .NET Developer position. May I have your full name please?"

**Candidate**: "John Doe"

**Assistant**: "Thank you, John. This is a time-sensitive opportunity and we're looking to schedule interviews within the next two days. Would you be available **tomorrow** or **the day after tomorrow**?"

**Candidate**: "Tomorrow works"

**Assistant**: "What time would work best for you?"

**Candidate**: "10 AM"

**Assistant**: "Thank you! Let me just confirm - John Doe, your interview with **Microsoft** is scheduled for **tomorrow** at 10:00 AM IST. Is that correct?"

**Candidate**: "Yes"

**Assistant**: *[Submits to tool with interviewDate: "tomorrow"]*

**Assistant**: "Perfect! Your interview is confirmed. You'll receive a confirmation email shortly. We appreciate your time and look forward to speaking with you. Good luck!"

---

## ✨ Benefits

1. **Professionalism**: Correct client names build trust
2. **Clarity**: "Tomorrow" is crystal clear vs "2026-02-25"
3. **Natural**: Sounds like a real person, not a robot
4. **Flexible**: Works for any client automatically
5. **Maintainable**: No need to update instructions per client

---

**Status**: ✅ All Code Updated and Ready  
**Next Step**: Test at `/Dashboard/Index?testVapiCalls=true`  
**Date**: February 24, 2026

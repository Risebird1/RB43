# Typing Assessment OpenAI Integration - Quick Setup Guide

## Overview
The typing assessment now uses OpenAI Assistant for comprehensive analysis instead of client-side calculations.

---

## Quick Setup (5 Steps)

### Step 1: Create OpenAI Assistant

1. Go to: https://platform.openai.com/assistants
2. Click **"Create Assistant"**
3. Configure:
   - **Name:** Typing Assessment Analyzer
   - **Instructions:** Copy the entire JSON block below and paste it as the assistant's instructions.
     ```json
     {
       "task": "typing_accuracy_analysis",
       "inputs": {
         "reference_paragraph": "string",
         "user_typed_text": "string",
         "words_per_minute": "number — the candidate's actual measured typing rate in words per single minute (WPM). This is a per-minute rate, NOT the total number of words typed for the whole session. Example: if a candidate types 60 words in 2 minutes, words_per_minute = 30, not 60.",
         "total_minutes": "number"
       },
       "processing_rules": {
         "allowed_word_count": {
           "formula": "words_per_minute * total_minutes",
           "rounding": "use integer value; if decimal, round down",
           "note": "Because words_per_minute is a per-minute rate, multiplying by total_minutes gives the total expected word count for the session."
         },
         "reference_restriction": {
           "method": "compare only the first N words of the reference paragraph",
           "N": "allowed_word_count"
         },
         "extra_words_handling": {
           "rule": "If user types more than allowed_word_count, treat as positive performance. Do not penalize extra words.",
           "comparison_scope": "accuracy comparison still only uses the first allowed_word_count words of the reference paragraph"
         },
         "error_categories_to_track": [
           "spelling_errors",
           "missing_words",
           "extra_words_within_limit",
           "capitalization_errors",
           "punctuation_errors"
         ],
         "passing_score_rules": {
           "passing_score_definition": "Passing Score – 30 words per minute with 97% accuracy.",
           "speed_rule": "If words_per_minute < 30 then candidate is not recommended (regardless of accuracy). Note: words_per_minute is always the per-minute rate — a value of 30 means exactly 30 words per minute.",
           "accuracy_rule": "Candidate must have accuracy_percent >= 97 to be recommended.",
           "recommended_formula": "(words_per_minute >= 30) AND (accuracy_percent >= 97)",
           "fail_reason_codes": {
             "WPM_BELOW_30": "words_per_minute < 30",
             "ACCURACY_BELOW_97": "accuracy_percent < 97"
           }
         }
       },
       "grading_rules": {
         "overall_grade_mapping": [
           { "min_accuracy_percent": 95, "grade": "Excellent" },
           { "min_accuracy_percent": 90, "grade": "Very Good" },
           { "min_accuracy_percent": 80, "grade": "Good" },
           { "min_accuracy_percent": 70, "grade": "Fair" },
           { "min_accuracy_percent": 0,  "grade": "Needs Improvement" }
         ]
       },
       "output_json_format": {
         "meta": {
           "words_per_minute": "number — echo back the input words_per_minute value exactly as supplied; this is the per-minute rate (e.g. 30 means 30 WPM, not 60 words for 2 minutes)",
           "total_minutes": "number",
           "allowed_word_count": "number",
           "reference_total_words": "number",
           "reference_words_used_for_comparison": "number",
           "user_total_words_typed": "number",
           "user_exceeded_allowed_words": "boolean",
           "exceeded_by_words": "number"
         },
         "scores": {
           "word_level_accuracy_percent": "number",
           "character_level_accuracy_percent": "number",
           "overall_grade": "string"
         },
         "counts": {
           "correct_words": "number",
           "incorrect_words": "number",
           "missing_words": "number",
           "extra_words_within_limit": "number",
           "extra_words_beyond_limit": "number",
           "spelling_errors": "number",
           "capitalization_errors": "number",
           "punctuation_errors": "number"
         },
         "highlights": {
           "top_error_types": [
             {
               "type": "string",
               "count": "number",
               "examples": [
                 { "user_text": "string", "expected_text": "string", "position_word_index": "number" }
               ]
             }
           ],
           "missing_word_examples": [
             { "expected_text": "string", "position_word_index": "number" }
           ],
           "extra_word_examples": [
             { "user_text": "string", "position_word_index": "number", "within_comparison_limit": "boolean" }
           ]
         },
         "comparison_preview": {
           "reference_used_excerpt": "string",
           "user_used_excerpt": "string",
           "notes": ["string"]
         },
         "verdict": {
           "met_expected_output_size": "boolean",
           "speed_comment": "string",
           "accuracy_comment": "string",
           "actionable_tips": ["string"],
           "recommended": "boolean",
           "recommendation_reasons": ["string"],
           "passing_criteria": {
             "min_words_per_minute": 30,
             "min_accuracy_percent": 97
           }
         }
       }
     }
     ```
   - **Model:** gpt-4-turbo-preview (or latest GPT-4 model)
   - **Tools:** Leave empty (none required)
4. **Save** and copy the Assistant ID (format: `asst_xxxxxxxxxxxxx`)

---

### Step 2: Update Assistant ID in Code

**File:** `Risebird.Entities\Data\OpenAIInterviewerAssistants.cs`

**Find this line:**
```csharp
public static readonly string TypingAssessmentAssistant = "asst_TYPING_ASSESSMENT_ID";
```

**Replace with your actual ID:**
```csharp
public static readonly string TypingAssessmentAssistant = "asst_abc123xyz789"; // Your actual ID
```

---

### Step 3: Run Database Scripts

**In SQL Server Management Studio, run these in order:**

1. **Update Table:**
   ```
   Risebird.DB\Tables\TypingSpeedAssessment.sql
   ```

2. **Update Stored Procedure:**
   ```
   Risebird.DB\StoredProcedures\usp_SaveTypingSpeedAssessment.sql
   ```

**Verify:**
```sql
-- Check new columns exist
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'TypingSpeedAssessment' 
  AND COLUMN_NAME IN ('OpenAIAnalysisJson', 'OpenAIThreadId', 'OpenAIRunId');
-- Should return 3 rows
```

---

### Step 4: Rebuild Application

1. **Clean Solution** (Visual Studio: Build → Clean Solution)
2. **Rebuild Solution** (Visual Studio: Build → Rebuild Solution)
3. **Restart Application/IIS**

---

### Step 5: Test

1. **Mark a question for typing assessment:**
   ```sql
   UPDATE SkillChannelSampleQuestions 
   SET IsTypingSpeedAssessment = 1 
   WHERE id = [YOUR_QUESTION_ID];
   ```

2. **Start an interview and complete the typing test**

3. **Verify in database:**
   ```sql
   SELECT TOP 1
       SessionId,
       IsPassed,
       CASE 
           WHEN OpenAIAnalysisJson IS NULL THEN '❌ Missing'
           WHEN LEN(OpenAIAnalysisJson) < 100 THEN '⚠️ Too Short'
           ELSE '✅ Present'
       END AS AnalysisStatus,
       LEN(OpenAIAnalysisJson) AS JsonLength,
       OpenAIThreadId,
       CreatedAt
   FROM TypingSpeedAssessment
   ORDER BY CreatedAt DESC;
   ```

**Expected Result:**
- `AnalysisStatus`: ✅ Present
- `JsonLength`: > 1000 characters
- `OpenAIThreadId`: Not null
- `IsPassed`: 0 or 1 (based on accuracy)

---

## Verification Checklist

- [ ] OpenAI Assistant created and ID copied
- [ ] Assistant ID updated in `OpenAIInterviewerAssistants.cs`
- [ ] Database table updated (3 new columns)
- [ ] Stored procedure updated (3 new parameters)
- [ ] Solution rebuilt successfully
- [ ] Application restarted
- [ ] Test question flagged for typing assessment
- [ ] Test completed successfully
- [ ] Database shows OpenAI analysis JSON
- [ ] `IsPassed` value is correct

---

## Expected Output Example

After a successful test, you should see JSON like this in `OpenAIAnalysisJson`:

```json
{
  "meta": {
    "words_per_minute": 30,
    "total_minutes": 2,
    "allowed_word_count": 60,
    "reference_total_words": 250,
    "reference_words_used_for_comparison": 60,
    "user_total_words_typed": 58,
    "user_exceeded_allowed_words": false,
    "exceeded_by_words": 0
  },
  "scores": {
    "word_level_accuracy_percent": 95.5,
    "character_level_accuracy_percent": 97.2,
    "overall_grade": "Excellent"
  },
  "counts": {
    "correct_words": 55,
    "incorrect_words": 3,
    "missing_words": 2,
    "spelling_errors": 2,
    "capitalization_errors": 1,
    "punctuation_errors": 1
  },
  "verdict": {
    "met_expected_output_size": false,
    "speed_comment": "Typed 58 out of expected 60 words",
    "accuracy_comment": "Word-level accuracy: 95.5% - Excellent performance",
    "actionable_tips": [
      "Focus on common spelling rules",
      "Pay attention to capitalization"
    ]
  }
}
```

---

## Troubleshooting

### ❌ "OpenAIAnalysisJson is NULL"

**Possible Causes:**
1. Assistant ID is incorrect or not set
2. OpenAI API key not configured
3. Assistant returned an error

**Check Application Logs:**
```
Look for: "Starting OpenAI typing assessment analysis"
Look for: "OpenAI typing assessment response received"
Look for errors in between
```

**Check Database:**
```sql
SELECT TOP 5
    SessionId,
    OpenAIThreadId,
    OpenAIRunId,
    CASE 
        WHEN OpenAIAnalysisJson IS NULL THEN 'NULL'
        ELSE 'Present'
    END AS Status
FROM TypingSpeedAssessment
ORDER BY CreatedAt DESC;
```

---

### ⚠️ "Failed to parse OpenAI response as JSON"

**Solution:** Check OpenAI assistant instructions include "ONLY JSON" emphasis.

**Update Instructions:**
```
CRITICAL: Return ONLY valid JSON. Do not include any explanatory text, 
markdown formatting, or anything before/after the JSON object.
```

---

### 🔍 "How do I view the detailed analysis?"

**Query:**
```sql
SELECT 
    SessionId,
    JSON_VALUE(OpenAIAnalysisJson, '$.scores.word_level_accuracy_percent') AS Accuracy,
    JSON_VALUE(OpenAIAnalysisJson, '$.scores.overall_grade') AS Grade,
    JSON_VALUE(OpenAIAnalysisJson, '$.counts.spelling_errors') AS SpellingErrors,
    JSON_QUERY(OpenAIAnalysisJson, '$.verdict.actionable_tips') AS Tips,
    CreatedAt
FROM TypingSpeedAssessment
WHERE SessionId = 'YOUR_SESSION_ID';
```

---

## What Changed?

### Before (Old System)
- JavaScript calculated accuracy locally
- Simple character-by-character comparison
- Basic pass/fail
- No error categorization

### After (New System with OpenAI)
- Raw data sent to OpenAI Assistant
- AI analyzes first N words (based on WPM × minutes)
- Detailed error categorization
- Rich feedback with actionable tips
- Comprehensive JSON stored in database

---

## Key Features

✅ **Comprehensive Analysis**
- Word-level accuracy
- Character-level accuracy
- Error categorization (spelling, capitalization, punctuation, missing words)

✅ **Smart Comparison**
- Only compares first N words (allowed_word_count = WPM × minutes)
- Extra words beyond limit don't penalize
- Flexible grading rules

✅ **Rich Feedback**
- Overall grade (Excellent, Very Good, Good, Fair, Needs Improvement)
- Specific error examples with positions
- Actionable tips for improvement

✅ **Full Traceability**
- OpenAI thread ID stored
- Complete JSON response saved
- Can review/audit any assessment

---

## Passing Criteria

**Current Rule:** Word-level accuracy ≥ 97%

**Available Metrics (for future customization):**
- `word_level_accuracy_percent`
- `character_level_accuracy_percent`
- `overall_grade`
- `counts.spelling_errors`
- `counts.missing_words`
- `met_expected_output_size`

---

## Need Help?

1. **Check application logs** for detailed error messages
2. **Check database** for OpenAI response
3. **Review** `TYPING_ASSESSMENT_OPENAI_INTEGRATION.md` for detailed documentation
4. **Test OpenAI Assistant directly** in OpenAI Playground with sample data

---

## Support Queries

### View Last 5 Assessments
```sql
SELECT TOP 5
    SessionId,
    CreatedBy AS Candidate,
    IsPassed,
    JSON_VALUE(OpenAIAnalysisJson, '$.scores.word_level_accuracy_percent') AS Accuracy,
    JSON_VALUE(OpenAIAnalysisJson, '$.scores.overall_grade') AS Grade,
    OpenAIThreadId,
    CreatedAt
FROM TypingSpeedAssessment
ORDER BY CreatedAt DESC;
```

### Find Failed Assessments with Tips
```sql
SELECT 
    SessionId,
    CreatedBy AS Candidate,
    JSON_VALUE(OpenAIAnalysisJson, '$.scores.word_level_accuracy_percent') AS Accuracy,
    JSON_QUERY(OpenAIAnalysisJson, '$.verdict.actionable_tips') AS Tips
FROM TypingSpeedAssessment
WHERE IsPassed = 0
  AND OpenAIAnalysisJson IS NOT NULL
ORDER BY CreatedAt DESC;
```

---

**Setup Time:** ~10 minutes  
**Last Updated:** January 30, 2026  
**Version:** 3.0

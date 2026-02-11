# Typing Assessment - OpenAI Integration

## Overview
The typing assessment now uses OpenAI Assistant to perform comprehensive analysis of typing accuracy, providing detailed error categorization, comparison, and actionable feedback.

---

## Architecture Changes

### Previous Approach (Client-Side Calculation)
- JavaScript calculated WPM and character-level accuracy
- Simple pass/fail based on local metrics
- Limited error analysis

### New Approach (OpenAI AI Assistant Analysis)
- Frontend sends raw typing data to backend
- Backend calls OpenAI Assistant with structured request
- AI analyzes typing data comprehensively
- Detailed JSON response stored in database
- Rich error categorization and feedback

---

## Database Changes

### Updated Table: `TypingSpeedAssessment`

**New Columns Added:**
```sql
OpenAIAnalysisJson NVARCHAR(MAX) NULL
OpenAIThreadId VARCHAR(100) NULL  
OpenAIRunId VARCHAR(100) NULL
```

**Purpose:**
- `OpenAIAnalysisJson`: Stores complete JSON response from OpenAI
- `OpenAIThreadId`: OpenAI thread ID for tracking
- `OpenAIRunId`: OpenAI run ID for tracking

**File:** `Risebird.DB\Tables\TypingSpeedAssessment.sql`

---

### Updated Stored Procedure: `usp_SaveTypingSpeedAssessment`

**New Parameters:**
```sql
@OpenAIAnalysisJson NVARCHAR(MAX) = NULL,
@OpenAIThreadId VARCHAR(100) = NULL,
@OpenAIRunId VARCHAR(100) = NULL
```

**File:** `Risebird.DB\StoredProcedures\usp_SaveTypingSpeedAssessment.sql`

---

## Backend Changes

### 1. New Assistant ID

**File:** `Risebird.Entities\Data\OpenAIInterviewerAssistants.cs`

```csharp
public static readonly string TypingAssessmentAssistant = "asst_TYPING_ASSESSMENT_ID";
```

**Action Required:** Replace `"asst_TYPING_ASSESSMENT_ID"` with actual OpenAI Assistant ID after creating the assistant.

---

### 2. New Entity Models

**File:** `Risebird.Entities\ViewModel\TypingAssessmentOpenAIModels.cs`

**Classes Created:**
- `TypingAssessmentOpenAIRequest` - Request structure
- `TypingInputs` - Input data (reference text, typed text, WPM, minutes)
- `ProcessingRules` - Rules for analysis
- `GradingRules` - Grading criteria
- `TypingAssessmentOpenAIResponse` - Response structure
- `MetaData` - Analysis metadata
- `Scores` - Accuracy scores and grade
- `Counts` - Error counts by category
- `Highlights` - Top errors with examples
- `Verdict` - Pass/fail with feedback

---

### 3. Updated Entity: `TypingSpeedAssessment.cs`

**New Properties:**
```csharp
public string OpenAIAnalysisJson { get; set; }
public string OpenAIThreadId { get; set; }
public string OpenAIRunId { get; set; }
```

---

### 4. Updated View Model: `ChatMessageViewModel.cs`

**New Property:**
```csharp
public decimal TypingTotalMinutes { get; set; }
public string TypingOpenAIAnalysisJson { get; set; }
```

---

### 5. Updated Controller: `AIInterviewController.cs`

**New Dependency Injection:**
```csharp
private readonly IOpenAIInterviewerAssistantHelper _openAIAssistantHelper;
```

**Process Flow:**
1. Receive typing data from frontend
2. Construct OpenAI request with structured JSON
3. Create OpenAI thread
4. Send message to assistant
5. Create run and wait for completion
6. Parse JSON response
7. Extract accuracy metrics
8. Determine pass/fail (≥97% word accuracy)
9. Save to database with OpenAI analysis

**File:** `Risebird.Web\Controllers\AIInterviewController.cs`

---

## Frontend Changes

### Updated: `Index.cshtml`

**Previous Behavior:**
- Calculated WPM locally
- Calculated character accuracy locally
- Showed detailed results immediately

**New Behavior:**
- Sends raw typing data to backend
- Shows "Processing..." message
- No local calculation (handled by OpenAI)
- Simplified results display

**Hidden Form Fields:**
- Added: `TypingTotalMinutes` (2.00 minutes)
- Kept for compatibility: `TypingTotalChars`, `TypingCorrectChars`, `TypingAccuracy`, `TypingWPM`
- Values are placeholders; actual analysis by OpenAI

---

## OpenAI Assistant Configuration

### Required Instructions for OpenAI Assistant

The assistant must be configured with the following instructions:

```json
{
  "task": "typing_accuracy_analysis",
  "instructions": "You are a typing assessment analyzer. When given typing assessment data, analyze it comprehensively and return ONLY a valid JSON response with no additional text. The output must be strictly JSON only, following the output_json_format structure provided in the request.",
  "model": "gpt-4-turbo-preview",
  "tools": [],
  "file_ids": []
}
```

### Request Format Sent to Assistant

```json
{
  "task": "typing_accuracy_analysis",
  "inputs": {
    "reference_paragraph": "string - the sample text",
    "user_typed_text": "string - what candidate typed",
    "words_per_minute": 30.00,
    "total_minutes": 2.00
  },
  "processing_rules": {
    "allowed_word_count": {
      "formula": "words_per_minute * total_minutes",
      "rounding": "use integer value; if decimal, round down"
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
    ]
  },
  "grading_rules": {
    "overall_grade_mapping": [
      {"min_accuracy_percent": 95, "grade": "Excellent"},
      {"min_accuracy_percent": 90, "grade": "Very Good"},
      {"min_accuracy_percent": 80, "grade": "Good"},
      {"min_accuracy_percent": 70, "grade": "Fair"},
      {"min_accuracy_percent": 0, "grade": "Needs Improvement"}
    ]
  }
}
```

### Expected Response Format

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
    "extra_words_within_limit": 0,
    "extra_words_beyond_limit": 0,
    "spelling_errors": 2,
    "capitalization_errors": 1,
    "punctuation_errors": 1
  },
  "highlights": {
    "top_error_types": [
      {
        "type": "spelling_errors",
        "count": 2,
        "examples": [
          {
            "user_text": "recieve",
            "expected_text": "receive",
            "position_word_index": 15
          }
        ]
      }
    ],
    "missing_word_examples": [
      {
        "expected_text": "the",
        "position_word_index": 25
      }
    ],
    "extra_word_examples": []
  },
  "comparison_preview": {
    "reference_used_excerpt": "First 60 words of reference text...",
    "user_used_excerpt": "First 58 words typed by user...",
    "notes": [
      "Comparison limited to first 60 words as per allowed_word_count",
      "User typed 58 words, 2 short of expected"
    ]
  },
  "verdict": {
    "met_expected_output_size": false,
    "speed_comment": "Typed 58 out of expected 60 words (96.7% of expected volume)",
    "accuracy_comment": "Word-level accuracy: 95.5% - Excellent performance",
    "actionable_tips": [
      "Focus on common spelling rules (e.g., 'i before e except after c')",
      "Pay attention to capitalization at sentence beginnings",
      "Practice typing complete sentences to avoid missing words"
    ]
  }
}
```

---

## Passing Criteria

### Current Rule
- **Pass if:** `word_level_accuracy_percent >= 97.00%`
- **Fail if:** `word_level_accuracy_percent < 97.00%`

### Available Metrics for Future Criteria
From OpenAI response, you can also use:
- `character_level_accuracy_percent`
- `overall_grade`
- `counts.spelling_errors`
- `counts.missing_words`
- `met_expected_output_size`

---

## Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Candidate Types Text (2 minutes max)                    │
│    Frontend: Captures raw text, timestamps                 │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Form Submission                                          │
│    POST to AIInterviewController                            │
│    Data: reference_paragraph, user_typed_text, times       │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Controller Prepares OpenAI Request                      │
│    - Constructs JSON with inputs + rules                   │
│    - Adds instruction: "Return ONLY JSON"                  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Call OpenAI Assistant                                    │
│    - CreateThreadAsync()                                    │
│    - AddMessageAsync(thread, "user", request)               │
│    - CreateRunAsync(thread, TypingAssessmentAssistant)     │
│    - WaitForRunAsync(thread, run)                          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. OpenAI Processes Request                                 │
│    - Compares first N words (allowed_word_count)           │
│    - Calculates word-level accuracy                        │
│    - Calculates character-level accuracy                   │
│    - Categorizes errors (spelling, missing, etc.)          │
│    - Generates grade and actionable tips                   │
│    - Returns JSON response                                 │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Controller Parses Response                               │
│    - Deserialize JSON                                       │
│    - Extract word_level_accuracy_percent                   │
│    - Determine isPassed (>= 97%)                           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 7. Save to Database                                         │
│    - All original data                                      │
│    - OpenAI analysis JSON (full response)                  │
│    - ThreadId, RunId (for tracking)                        │
│    - Pass/Fail status                                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 8. Continue Interview                                       │
│    Next question loads                                      │
└─────────────────────────────────────────────────────────────┘
```

---

## Query Examples

### View Full OpenAI Analysis
```sql
SELECT 
    SessionId,
    QuestionId,
    IsPassed,
    OpenAIAnalysisJson,
    OpenAIThreadId,
    CreatedAt
FROM TypingSpeedAssessment
WHERE SessionId = 'YOUR_SESSION_ID';
```

### Extract Specific Metrics from JSON
```sql
SELECT 
    SessionId,
    JSON_VALUE(OpenAIAnalysisJson, '$.scores.word_level_accuracy_percent') AS WordAccuracy,
    JSON_VALUE(OpenAIAnalysisJson, '$.scores.character_level_accuracy_percent') AS CharAccuracy,
    JSON_VALUE(OpenAIAnalysisJson, '$.scores.overall_grade') AS Grade,
    JSON_VALUE(OpenAIAnalysisJson, '$.counts.spelling_errors') AS SpellingErrors,
    JSON_VALUE(OpenAIAnalysisJson, '$.verdict.accuracy_comment') AS Comment,
    IsPassed
FROM TypingSpeedAssessment
ORDER BY CreatedAt DESC;
```

### Extract Actionable Tips
```sql
SELECT 
    SessionId,
    CreatedBy AS CandidateName,
    JSON_QUERY(OpenAIAnalysisJson, '$.verdict.actionable_tips') AS Tips,
    CreatedAt
FROM TypingSpeedAssessment
WHERE IsPassed = 0
ORDER BY CreatedAt DESC;
```

### Error Category Analysis
```sql
SELECT 
    JSON_VALUE(OpenAIAnalysisJson, '$.counts.spelling_errors') AS SpellingErrors,
    JSON_VALUE(OpenAIAnalysisJson, '$.counts.capitalization_errors') AS CapitalizationErrors,
    JSON_VALUE(OpenAIAnalysisJson, '$.counts.punctuation_errors') AS PunctuationErrors,
    JSON_VALUE(OpenAIAnalysisJson, '$.counts.missing_words') AS MissingWords,
    COUNT(*) AS AssessmentCount
FROM TypingSpeedAssessment
WHERE OpenAIAnalysisJson IS NOT NULL
GROUP BY 
    JSON_VALUE(OpenAIAnalysisJson, '$.counts.spelling_errors'),
    JSON_VALUE(OpenAIAnalysisJson, '$.counts.capitalization_errors'),
    JSON_VALUE(OpenAIAnalysisJson, '$.counts.punctuation_errors'),
    JSON_VALUE(OpenAIAnalysisJson, '$.counts.missing_words');
```

---

## Setup Instructions

### 1. Create OpenAI Assistant

1. Go to OpenAI Platform → Assistants
2. Create new assistant with:
   - **Name:** "Typing Assessment Analyzer"
   - **Instructions:** "You are a typing assessment analyzer. When given typing assessment data, analyze it comprehensively and return ONLY a valid JSON response with no additional text. The output must be strictly JSON only."
   - **Model:** gpt-4-turbo-preview (or latest)
   - **Tools:** None required
3. Copy the Assistant ID (e.g., `asst_abc123xyz`)

### 2. Update Assistant ID in Code

**File:** `Risebird.Entities\Data\OpenAIInterviewerAssistants.cs`

```csharp
public static readonly string TypingAssessmentAssistant = "asst_YOUR_ACTUAL_ID_HERE";
```

### 3. Run Database Scripts

```sql
-- 1. Update table
\Risebird.DB\Tables\TypingSpeedAssessment.sql

-- 2. Update stored procedure
\Risebird.DB\StoredProcedures\usp_SaveTypingSpeedAssessment.sql
```

### 4. Rebuild Solution

- Clean and rebuild the backend solution
- Ensure all dependencies are resolved

### 5. Test

1. Start application
2. Mark a question for typing assessment
3. Complete the typing test
4. Check database for:
   - `OpenAIAnalysisJson` populated
   - `OpenAIThreadId` recorded
   - `IsPassed` calculated correctly

---

## Troubleshooting

### Issue: OpenAI Analysis Not Saved

**Check:**
1. OpenAI Assistant ID is correct
2. OpenAI API key is configured
3. Application logs for errors
4. OpenAI thread/run status

**Query:**
```sql
SELECT 
    SessionId,
    OpenAIThreadId,
    OpenAIRunId,
    CASE 
        WHEN OpenAIAnalysisJson IS NULL THEN 'Missing'
        ELSE 'Present'
    END AS AnalysisStatus
FROM TypingSpeedAssessment
ORDER BY CreatedAt DESC;
```

### Issue: JSON Parsing Errors

**Symptoms:** "Failed to parse OpenAI response as JSON"

**Causes:**
- OpenAI returned text before/after JSON
- Assistant not following "JSON only" instruction

**Solution:**
- Update assistant instructions to emphasize "ONLY JSON"
- Add JSON extraction logic in controller (strip non-JSON text)

### Issue: Empty Response

**Check:**
```sql
SELECT 
    SessionId,
    LEN(OpenAIAnalysisJson) AS JsonLength,
    LEFT(OpenAIAnalysisJson, 100) AS JsonPreview
FROM TypingSpeedAssessment
WHERE OpenAIAnalysisJson IS NOT NULL;
```

---

## Benefits of OpenAI Integration

### 1. Comprehensive Analysis
- Word-level accuracy
- Character-level accuracy
- Error categorization (spelling, capitalization, punctuation, missing words)
- Comparison scope control (first N words only)

### 2. Rich Feedback
- Overall grade (Excellent, Very Good, Good, Fair, Needs Improvement)
- Specific error examples with positions
- Actionable tips for improvement
- Speed and accuracy comments

### 3. Flexibility
- Easy to adjust passing criteria
- Can use multiple metrics for grading
- Detailed data for reporting and analytics
- Supports future enhancements (e.g., difficulty levels)

### 4. Scalability
- Offloads complex logic to AI
- Consistent analysis across all assessments
- Can evolve criteria without code changes (assistant instructions)

---

## Future Enhancements

### Possible Additions:
1. **Real-time feedback** - Show errors as candidate types
2. **Difficulty levels** - Adjust `words_per_minute` and `total_minutes` based on role
3. **Custom grading** - Different criteria per job role
4. **Trend analysis** - Track improvement across multiple attempts
5. **Detailed reports** - Export PDF with full analysis
6. **Benchmarking** - Compare against other candidates

---

## Files Modified/Created

### Database
1. `Risebird.DB\Tables\TypingSpeedAssessment.sql` - Added 3 columns
2. `Risebird.DB\StoredProcedures\usp_SaveTypingSpeedAssessment.sql` - Added 3 parameters

### Backend
1. `Risebird.Entities\Data\OpenAIInterviewerAssistants.cs` - Added TypingAssessmentAssistant ID
2. `Risebird.Entities\ViewModel\TypingAssessmentOpenAIModels.cs` - NEW (request/response models)
3. `Risebird.Entities\ViewModel\TypingSpeedAssessment.cs` - Added 3 properties
4. `Risebird.Entities\ViewModel\ChatMessageViewModel.cs` - Added 2 properties
5. `Risebird.Repository\AIInterviewerRepository.cs` - Added 3 parameters to SP call
6. `Risebird.Web\Controllers\AIInterviewController.cs` - Integrated OpenAI call logic

### Frontend
1. `Risebird.Web\Views\AIInterview\Index.cshtml` - Simplified to send raw data

### Documentation
1. `TYPING_ASSESSMENT_OPENAI_INTEGRATION.md` - This file

---

**Last Updated:** January 30, 2026  
**Version:** 3.0  
**Changes:** Replaced client-side calculation with OpenAI Assistant integration for comprehensive typing assessment analysis

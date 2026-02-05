## Listening Comprehension Assessment - Implementation Guide

### Overview
This document describes the implementation of the Listening Comprehension Assessment feature for AI Interviews in the Risebird application.

### Feature Description
The listening comprehension assessment evaluates a candidate's ability to understand and summarize spoken content. One question per IRF can be flagged as a listening comprehension question. When this question appears:
- A modal displays with instructions
- An audio clip plays automatically (can only be played once)
- The candidate writes a summary of what they heard
- Video recording captures the entire assessment
- Time taken, audio, transcription, and summary are saved

---

## Database Changes

### 1. New Table: `ListeningComprehensionAssessment`

**Location:** `Risebird.DB/Tables/ListeningComprehensionAssessment.sql`

**Columns:**
- `id` (int, PK, IDENTITY)
- `SessionId` (varchar(50))
- `QuestionId` (int)
- `AudioUrl` (varchar(max)) - URL to the audio file
- `AudioTranscription` (nvarchar(max)) - Text of what was spoken
- `CandidateSummary` (nvarchar(max)) - Candidate's written summary
- `TimeStarted` (datetime)
- `TimeCompleted` (datetime)
- `TimeTakenInSeconds` (decimal(10,2))
- `CreatedAt` (datetime)
- `CreatedBy` (varchar(150))

### 2. Modified Table: `SkillChannelSampleQuestions`

**Location:** `Risebird.DB/Tables/SkillChannelSampleQuestions.sql`

**New Columns:**
- `IsListeningComprehension` (bit, NULL, DEFAULT 0) - Flags question for listening assessment
- `ListeningAudioUrl` (varchar(max), NULL) - URL to audio file for this question
- `ListeningAudioTranscription` (nvarchar(max), NULL) - Transcription of the audio

### 3. New Stored Procedure: `usp_SaveListeningComprehensionAssessment`

**Location:** `Risebird.DB/StoredProcedures/usp_SaveListeningComprehensionAssessment.sql`

**Parameters:**
- @SessionId, @QuestionId, @AudioUrl, @AudioTranscription
- @CandidateSummary, @TimeStarted, @TimeCompleted
- @TimeTakenInSeconds, @CreatedBy

### 4. Modified Stored Procedure: `usp_GetRandomQuestionForSession`

**Location:** `Risebird.DB/StoredProcedures/usp_GetRandomQuestionForSession.sql`

**Changes:**
- Returns `IsListeningComprehension`, `ListeningAudioUrl`, `ListeningAudioTranscription`

---

## Backend Changes

### 1. New Entity: `ListeningComprehensionAssessment`

**Location:** `Risebird.Entities/ViewModel/ListeningComprehensionAssessment.cs`

### 2. Modified Entity: `TechSkillComments`

**Location:** `Risebird.Entities/ViewModel/TechSkillComments.cs`

**Added Properties:**
```csharp
public bool IsListeningComprehension { get; set; }
public string ListeningAudioUrl { get; set; }
public string ListeningAudioTranscription { get; set; }
```

### 3. Modified Entity: `ChatMessageViewModel`

**Location:** `Risebird.Entities/ViewModel/ChatMessageViewModel.cs`

**Added Properties:**
```csharp
public bool IsListeningComprehensionQuestion { get; set; }
public string ListeningAudioUrl { get; set; }
public string ListeningAudioTranscription { get; set; }
public string ListeningSummary { get; set; }
public string ListeningStartTime { get; set; }
public string ListeningEndTime { get; set; }
public decimal ListeningTimeTaken { get; set; }
```

### 4-7. Repository & Service Layers

Updated interfaces and implementations for:
- `IAIInterviewerRepository` / `AIInterviewerRepository`
- `IAIInterviewerService` / `AIInterviewerService`

Added method: `SaveListeningComprehensionAssessment()`

### 8. Controller: `AIInterviewController`

**Location:** `Risebird.Web/Controllers/AIInterviewController.cs`

**GET Index:** Sets listening comprehension properties on model
**POST Index:** Saves listening comprehension data to database

---

## Frontend Changes

### Modified View: `Index.cshtml`

**Location:** `Risebird.Web/Views/AIInterview/Index.cshtml`

**JavaScript Addition (~200 lines):**

1. **Detection:** Checks `Model.IsListeningComprehensionQuestion`

2. **Modal UI:**
   - Instructions phase
   - Audio player (HTML5 `<audio>` element)
   - Timer display
   - Summary text area

3. **Flow Control:**
   - "Start" button → Shows audio player and text area
   - Audio plays automatically
   - Audio can only be played once (enforced)
   - Text area enabled after audio ends
   - "Submit" button → Validates and submits

4. **Video Recording:**
   - Starts automatically with "Start" button
   - Stops when "Submit" is clicked

5. **Hidden Form Fields:**
   ```html
   <input type="hidden" name="IsListeningComprehensionQuestion" />
   <input type="hidden" name="ListeningAudioUrl" />
   <input type="hidden" name="ListeningAudioTranscription" />
   <input type="hidden" name="ListeningSummary" />
   <input type="hidden" name="ListeningStartTime" />
   <input type="hidden" name="ListeningEndTime" />
   <input type="hidden" name="ListeningTimeTaken" />
   ```

6. **User Flow:**
   - Page loads → Modal appears
   - Click "Start" → Audio plays, video starts, timer starts
   - Audio finishes → Text area becomes enabled
   - Write summary
   - Click "Submit" → Video stops, data captured
   - Confirm submission → Form submits

---

## Setup Instructions

### For Administrators:

#### 1. Prepare Audio Files

Upload audio files to Azure Blob Storage:
- Container: `listeningaudio`
- Format: MP3 or WAV
- Duration: 30-90 seconds recommended
- Quality: Clear speech, minimal noise

Get the SAS URL for the audio file.

#### 2. Flag a Question

```sql
-- Clear existing flags
UPDATE SkillChannelSampleQuestions
SET IsListeningComprehension = 0
WHERE IRFId = 567 AND IsListeningComprehension = 1;

-- Flag question with audio
UPDATE SkillChannelSampleQuestions
SET IsListeningComprehension = 1,
    ListeningAudioUrl = 'https://...audio.mp3?sas=...',
    ListeningAudioTranscription = 'Full text of audio content here...'
WHERE id = 1234 AND IRFId = 567;
```

#### 3. Audio Content Guidelines

**Good Topics:**
- Meeting briefings
- Project specifications
- Customer scenarios
- Technical explanations
- Strategic plans

**Recording Tips:**
- Speak clearly at moderate pace
- Professional tone
- No background music
- Minimal sound effects
- Natural speaking style

### For Candidates:

1. Question loads → Modal appears automatically
2. Read instructions carefully
3. Click "Start" when ready
4. Audio plays automatically (listen carefully!)
5. Audio can only be played once
6. After audio finishes, write summary
7. Click "Submit" when done
8. Confirm submission

---

## Data Retrieval

### View Candidate Results

```sql
SELECT 
    im.candidateName,
    lca.AudioUrl,
    lca.CandidateSummary,
    lca.TimeTakenInSeconds
FROM ListeningComprehensionAssessment lca
INNER JOIN InterviewSessions isess 
    ON isess.SessionId = CAST(lca.SessionId AS UNIQUEIDENTIFIER)
INNER JOIN InterviewMeetings im 
    ON im.SessionId = isess.SessionId
WHERE im.candidateEmail = 'candidate@example.com';
```

### Compare Summary to Original

```sql
SELECT 
    lca.AudioTranscription AS Original,
    lca.CandidateSummary AS Summary,
    LEN(lca.AudioTranscription) AS OriginalLength,
    LEN(lca.CandidateSummary) AS SummaryLength
FROM ListeningComprehensionAssessment lca
WHERE lca.id = 123;
```

---

## Evaluation Guidelines

### What to Look For:

1. **Comprehension:**
   - Did candidate capture main points?
   - Are key details mentioned?
   - Is understanding demonstrated?

2. **Summarization:**
   - Is summary concise?
   - Proper grammar and spelling?
   - Logical flow?

3. **Time Management:**
   - < 2 minutes: Very quick (may lack detail)
   - 2-5 minutes: Reasonable pace
   - > 5 minutes: Thorough or struggling?

4. **Summary Length:**
   - Very short: May have missed content
   - Moderate: Usually indicates good comprehension
   - Very long: May be transcribing vs summarizing

### Scoring Recommendations:

```
Excellent (8-10):
- All main points captured
- Clear, well-written summary
- Demonstrates strong comprehension
- Appropriate length

Good (6-7):
- Most main points captured
- Generally clear summary
- Shows adequate comprehension
- May miss some details

Average (4-5):
- Some main points captured
- Basic understanding shown
- May have gaps or confusion
- Summary may be too brief/long

Below Average (1-3):
- Few main points captured
- Significant misunderstanding
- Very brief or off-topic summary
```

---

## Technical Details

### Audio Player Implementation

```javascript
const audioElement = document.getElementById('listeningAudio');
audioElement.src = audioUrl;

// Prevent replay
audioElement.addEventListener('play', function() {
    if (audioPlayed) {
        audioElement.pause();
        audioElement.currentTime = 0;
        Swal.showValidationMessage('Audio can only be played once!');
    }
});

// Enable text area after audio ends
audioElement.addEventListener('ended', function() {
    audioPlayed = true;
    audioElement.style.pointerEvents = 'none';
    summaryArea.disabled = false;
    summaryArea.focus();
});
```

### Form Submission

After candidate submits:
1. Video recording stops
2. Timer stops
3. Data captured in hidden fields
4. Confirmation dialog appears
5. On confirm → Form submits with all data
6. Next question loads

---

## Integration Points

**Works Seamlessly With:**
- ✅ Existing video recording system
- ✅ Form submission flow
- ✅ Question fetching mechanism
- ✅ Transcript logging
- ✅ Interview progression

**No Changes To:**
- Video recording functions
- Form validation
- Session management
- Navigation flow

---

## Testing Checklist

- [ ] Database tables created
- [ ] Stored procedures execute
- [ ] Flag question in database
- [ ] Upload audio to Azure
- [ ] Start interview with flagged question
- [ ] Modal appears correctly
- [ ] Audio plays when "Start" clicked
- [ ] Video recording starts
- [ ] Timer counts correctly
- [ ] Audio cannot be replayed
- [ ] Text area enables after audio
- [ ] Submit validates empty summary
- [ ] Video stops on submit
- [ ] Confirmation dialog appears
- [ ] Data saves to database
- [ ] Form submits successfully
- [ ] Next question loads

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Audio doesn't play | Check URL is valid SAS URL with read permissions |
| Audio plays multiple times | Verify audioPlayed flag logic |
| Text area always disabled | Check audio 'ended' event listener |
| Modal doesn't appear | Verify `IsListeningComprehension` flag set |
| Data not saving | Check stored procedure parameters |
| Video not starting | Verify `startRecording()` function exists |

---

## Future Enhancements

1. **Automated Scoring:** Use NLP to compare summary to transcription
2. **Multiple Audio Options:** Random selection from pool
3. **Audio with Questions:** Specific questions about audio content
4. **Playback Control:** Allow 2-3 plays instead of just 1
5. **Audio Difficulty Levels:** Easy/Medium/Hard by role
6. **Speech-to-Text Summary:** Option to speak summary instead of type

---

## Best Practices

### Audio Content:
- Clear, professional recording
- Appropriate difficulty for role
- 30-90 seconds duration
- Relevant business scenario

### Evaluation:
- Consider both comprehension and summarization
- Look for key points, not word-for-word
- Factor in candidate's writing ability
- Compare across candidates for fairness

### Setup:
- Only ONE listening question per IRF
- Test audio before going live
- Ensure SAS URL doesn't expire soon
- Keep transcription accurate

---

**Version:** 1.0  
**Date:** January 30, 2026  
**Status:** Production Ready ✅

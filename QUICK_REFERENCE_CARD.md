# Typing Speed Assessment - Quick Reference Card

## 🚀 Quick Start (5 Steps)

1. **Run Database Scripts** (in order):
   - `TypingSpeedAssessment.sql`
   - `SkillChannelSampleQuestions.sql` 
   - `usp_SaveTypingSpeedAssessment.sql`
   - `usp_GetRandomQuestionForSession.sql`

2. **Build & Deploy** backend and frontend

3. **Flag a Question**:
   ```sql
   UPDATE SkillChannelSampleQuestions
   SET IsTypingSpeedAssessment = 1
   WHERE id = [QuestionId] AND IRFId = [IRFId];
   ```

4. **Test Interview** with flagged question

5. **Verify Data**:
   ```sql
   SELECT * FROM TypingSpeedAssessment;
   ```

---

## 📊 Key Metrics

| Metric | Formula | Description |
|--------|---------|-------------|
| **WPM** | words / (seconds / 60) | Words per minute |
| **Accuracy** | (correct chars / total chars) × 100 | Percentage correct |
| **Time Taken** | end time - start time | Seconds elapsed |

---

## 🗂️ Key Files Changed

### Database (4 files)
- `Tables/TypingSpeedAssessment.sql` ⭐ NEW
- `Tables/SkillChannelSampleQuestions.sql` ✏️ MODIFIED
- `StoredProcedures/usp_SaveTypingSpeedAssessment.sql` ⭐ NEW
- `StoredProcedures/usp_GetRandomQuestionForSession.sql` ✏️ MODIFIED

### Backend (8 files)
- `Entities/ViewModel/TypingSpeedAssessment.cs` ⭐ NEW
- `Entities/ViewModel/TechSkillComments.cs` ✏️ MODIFIED
- `Entities/ViewModel/ChatMessageViewModel.cs` ✏️ MODIFIED
- `Repository.Interface/IAIInterviewerRepository.cs` ✏️ MODIFIED
- `Repository/AIInterviewerRepository.cs` ✏️ MODIFIED
- `Business.Interface/IAIInterviewerService.cs` ✏️ MODIFIED
- `Business/AIInterviewerService.cs` ✏️ MODIFIED
- `Controllers/AIInterviewController.cs` ✏️ MODIFIED

### Frontend (1 file)
- `Views/AIInterview/Index.cshtml` ✏️ MODIFIED (~270 lines added)

---

## 💾 Database Tables

### TypingSpeedAssessment (NEW)
```
id, SessionId, QuestionId, SampleText, TypedText,
TimeStarted, TimeCompleted, TimeTakenInSeconds,
TotalCharacters, CorrectCharacters, AccuracyPercentage,
WordsPerMinute, CreatedAt, CreatedBy
```

### SkillChannelSampleQuestions (MODIFIED)
```
+ IsTypingSpeedAssessment (bit, default 0)
```

---

## 🔧 Common SQL Queries

### Flag a Question
```sql
-- Clear existing flags for IRF
UPDATE SkillChannelSampleQuestions
SET IsTypingSpeedAssessment = 0
WHERE IRFId = 123 AND IsTypingSpeedAssessment = 1;

-- Flag new question
UPDATE SkillChannelSampleQuestions
SET IsTypingSpeedAssessment = 1
WHERE id = 456 AND IRFId = 123;
```

### View Candidate Results
```sql
SELECT 
    im.candidateName,
    tsa.WordsPerMinute,
    tsa.AccuracyPercentage,
    tsa.TimeTakenInSeconds
FROM TypingSpeedAssessment tsa
INNER JOIN InterviewSessions isess 
    ON isess.SessionId = CAST(tsa.SessionId AS UNIQUEIDENTIFIER)
INNER JOIN InterviewMeetings im 
    ON im.SessionId = isess.SessionId
WHERE im.candidateEmail = 'candidate@example.com';
```

### Top Performers
```sql
SELECT TOP 10
    im.candidateName,
    tsa.WordsPerMinute,
    tsa.AccuracyPercentage
FROM TypingSpeedAssessment tsa
INNER JOIN InterviewSessions isess 
    ON isess.SessionId = CAST(tsa.SessionId AS UNIQUEIDENTIFIER)
INNER JOIN InterviewMeetings im 
    ON im.SessionId = isess.SessionId
ORDER BY tsa.WordsPerMinute DESC;
```

### Remove Typing Flag
```sql
UPDATE SkillChannelSampleQuestions
SET IsTypingSpeedAssessment = 0
WHERE id = 456;
```

---

## 🎯 WPM Benchmarks

| WPM Range | Rating | Suitable For |
|-----------|--------|--------------|
| 80+ | Excellent | Professional typist, transcription |
| 60-79 | Good | Data entry, admin work |
| 40-59 | Average | General office work |
| < 40 | Below Avg | May need training |

## 📏 Accuracy Benchmarks

| Accuracy | Rating |
|----------|--------|
| 95-100% | Excellent |
| 90-94% | Good |
| 85-89% | Average |
| < 85% | Needs improvement |

---

## 🔍 Code Locations

### Controller Logic
**File:** `Controllers/AIInterviewController.cs`

**GET Index** (sets flag):
- Lines ~114, ~265, ~920

**POST Index** (saves data):
- Lines ~896-927

### Frontend Logic
**File:** `Views/AIInterview/Index.cshtml`
- Lines ~3335-3547

### Repository Logic
**File:** `Repository/AIInterviewerRepository.cs`
- Method: `SaveTypingSpeedAssessment`

---

## 🛠️ Configuration

### Add Sample Texts
Edit `Index.cshtml`, line ~3344:
```javascript
const sampleTexts = [
    "Your new sample text here...",
];
```

### Adjust Modal Delay
Edit `Index.cshtml`, line ~3544:
```javascript
setTimeout(() => {
    showTypingTestModal();
}, 1000); // milliseconds
```

---

## ⚠️ Troubleshooting

| Symptom | Check | Solution |
|---------|-------|----------|
| Modal doesn't show | Browser console | Verify SweetAlert2 loaded |
| Video not starting | Global scope | Verify `startRecording()` exists |
| Data not saving | App logs | Check stored proc parameters |
| Wrong WPM | Calculation | Verify time is in seconds |
| 0% accuracy | Typed text | Check candidate actually typed |

### View Application Logs
```csharp
// Look for entries from:
_applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
```

---

## 📋 Deployment Checklist

- [ ] Backup database
- [ ] Run database scripts
- [ ] Verify tables created
- [ ] Build backend
- [ ] Deploy frontend
- [ ] Flag test question
- [ ] End-to-end test
- [ ] Verify data saved
- [ ] Check logs
- [ ] Document customizations

---

## 🔐 Security Notes

- Video recordings require authenticated session
- SessionId validates against InterviewMeetings table
- Candidate email verified before data save
- SAS URLs expire (time-limited Azure access)
- No client-side evaluation bypass possible

---

## 📖 Documentation Files

| File | Purpose |
|------|---------|
| `IMPLEMENTATION_SUMMARY.md` | Quick overview |
| `TYPING_SPEED_ASSESSMENT_IMPLEMENTATION.md` | Complete technical guide |
| `TYPING_SPEED_FLOW_DIAGRAM.md` | Visual flow diagrams |
| `HowTo_FlagTypingSpeedQuestion.sql` | SQL reference guide |
| `QUICK_REFERENCE_CARD.md` | This file (quick lookup) |

---

## 📞 Support Resources

1. **Full Documentation:** `TYPING_SPEED_ASSESSMENT_IMPLEMENTATION.md`
2. **SQL Examples:** `HowTo_FlagTypingSpeedQuestion.sql`
3. **Flow Diagrams:** `TYPING_SPEED_FLOW_DIAGRAM.md`
4. **Application Logs:** Check `_applicationErrorLogger` entries

---

## ✅ Testing Checklist

**Database:**
- [ ] Tables exist
- [ ] Stored procs execute
- [ ] Flag question successfully

**Backend:**
- [ ] No build errors
- [ ] Services resolve correctly
- [ ] Dependency injection works

**Frontend:**
- [ ] Modal appears on flagged question
- [ ] Timer counts correctly
- [ ] Video starts/stops
- [ ] Calculations accurate

**Integration:**
- [ ] Data saves to database
- [ ] Video uploads successfully
- [ ] Next question loads
- [ ] Interview continues normally

**Edge Cases:**
- [ ] Non-typing questions work normally
- [ ] Only one flag per IRF
- [ ] Error handling works
- [ ] Failed saves don't break interview

---

## 🎓 User Experience Flow

```
1. Page Loads
   ↓
2. Modal Appears (after 1s)
   ↓
3. Candidate Reads Instructions
   ↓
4. Candidate Clicks "Start"
   ↓
5. Sample Text Appears
   ↓
6. Timer Starts + Video Starts
   ↓
7. Candidate Types
   ↓
8. Candidate Clicks "Submit"
   ↓
9. Results Displayed
   ↓
10. Candidate Clicks "Continue"
    ↓
11. Video Stops
    ↓
12. Normal Interview Continues
```

---

## 🚦 Feature Flags

**ONE question per IRF should have:**
```sql
IsTypingSpeedAssessment = 1
```

**All other questions should have:**
```sql
IsTypingSpeedAssessment = 0  (or NULL)
```

---

## 💡 Pro Tips

✅ **DO:**
- Flag only ONE question per IRF
- Choose appropriate questions for typing assessment
- Review both WPM and accuracy
- Consider role requirements when evaluating

❌ **DON'T:**
- Flag multiple questions per IRF
- Flag video-only questions
- Evaluate WPM alone (accuracy matters!)
- Use this for all roles (only where typing matters)

---

## 📊 Scoring Recommendation

```
Recommended Formula:
Score = (WPM × 0.7) + (Accuracy × 0.3)

Pass Criteria:
- Score >= 65 AND Accuracy >= 90 → Recommended
- Else → Review case-by-case
```

---

**Version:** 1.0  
**Date:** January 30, 2026  
**Status:** Production Ready ✅

# Typing Speed Assessment - Flow Diagram

## Complete End-to-End Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                         ADMIN SETUP PHASE                           │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────┐
│  Administrator   │
└────────┬─────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Flag ONE question per IRF             │
│  UPDATE SkillChannelSampleQuestions    │
│  SET IsTypingSpeedAssessment = 1       │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Question flagged in database          │
└────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────┐
│                      INTERVIEW START PHASE                          │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────┐
│    Candidate     │
│  Clicks link to  │
│ start interview  │
└────────┬─────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  GET /AIInterview/Index?sid={guid}     │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Controller: AIInterviewController     │
│  Method: Index (GET)                   │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Fetch interview details by SessionId  │
│  _aiinterviewerService.                │
│  GetAIInterviewDetailsBySessionId()    │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Fetch next question                   │
│  _aiinterviewerService.                │
│  GetRandomQuestionForSession()         │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Execute: usp_GetRandomQuestionForSession │
│  Returns: Question + IsTypingSpeedAssessment flag │
└────────┬───────────────────────────────┘
         │
         ↓
         │
    ┌────┴────┐
    │  Is     │
    │ Typing  │ No
    │ Speed?  ├───────┐
    └────┬────┘       │
         │Yes         │
         ↓            ↓
┌────────────────┐  ┌──────────────────┐
│ Set flag:      │  │ Regular question │
│ IsTypingSpeed  │  │ flow (existing)  │
│ Question=true  │  └──────────────────┘
└────────┬───────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Return View(model)                    │
│  with IsTypingSpeedQuestion = true     │
└────────┬───────────────────────────────┘
         │
         ↓


┌─────────────────────────────────────────────────────────────────────┐
│                      FRONTEND TYPING TEST PHASE                     │
└─────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────┐
│  Page loads: Index.cshtml              │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  JavaScript checks:                    │
│  @Json.Serialize(Model.                │
│  IsTypingSpeedQuestion)                │
└────────┬───────────────────────────────┘
         │
    ┌────┴────┐
    │  Is     │
    │ Typing  │ No
    │ Speed?  ├───────► Exit (normal interview)
    └────┬────┘
         │Yes
         ↓
┌────────────────────────────────────────┐
│  Wait 1 second                         │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Add hidden form fields:               │
│  - IsTypingSpeedQuestion               │
│  - TypingSampleText                    │
│  - TypedText                           │
│  - TypingStartTime, etc.               │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Select random sample text from array  │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Show SweetAlert2 Modal                │
│  - Title: "Typing Speed Assessment"   │
│  - Instructions shown                  │
│  - Button: "Start"                     │
└────────┬───────────────────────────────┘
         │
         ↓
┌──────────────────┐
│  Candidate reads │
│   instructions   │
│  Clicks "Start"  │
└────────┬─────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Modal updates:                        │
│  - Hide instructions                   │
│  - Show sample text (read-only)        │
│  - Show timer (starts at 0:00)         │
│  - Show text area (enabled)            │
│  - Change button to "Submit"           │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Store: typingStartTime = new Date()  │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Call: startRecording()                │
│  (existing video recording function)   │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Start setInterval timer:              │
│  - Updates every 1 second              │
│  - Display as MM:SS                    │
└────────┬───────────────────────────────┘
         │
         ↓
┌──────────────────┐
│  Candidate types │
│   in text area   │
│  (timer running) │
└────────┬─────────┘
         │
         ↓
┌──────────────────┐
│  Candidate       │
│ clicks "Submit"  │
└────────┬─────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Stop timer (clearInterval)            │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Store: typingEndTime = new Date()    │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Calculate metrics:                    │
│  1. timeTaken = (end - start) / 1000   │
│  2. accuracy = calculateAccuracy()     │
│  3. wpm = calculateWPM()               │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Populate hidden form fields:          │
│  - TypingSampleText = sampleText       │
│  - TypedText = textarea.value          │
│  - TypingStartTime = ISO string        │
│  - TypingEndTime = ISO string          │
│  - TypingTimeTaken = decimal           │
│  - TypingTotalChars = count            │
│  - TypingCorrectChars = count          │
│  - TypingAccuracy = percentage         │
│  - TypingWPM = decimal                 │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Show results modal:                   │
│  - Time Taken: X seconds               │
│  - Accuracy: X%                        │
│  - Words Per Minute: X WPM             │
│  - Correct Characters: X / Y           │
└────────┬───────────────────────────────┘
         │
         ↓
┌──────────────────┐
│  Candidate       │
│ clicks "Continue"│
└────────┬─────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Call: stopRecording()                 │
│  (existing video recording function)   │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Modal closes                          │
│  Interview question displayed normally │
│  (Candidate can now answer question    │
│   or submit via audio/text as usual)   │
└────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────┐
│                       FORM SUBMISSION PHASE                         │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────┐
│  Candidate       │
│ submits answer   │
│  (audio/video/   │
│     text)        │
└────────┬─────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  POST /AIInterview/Index               │
│  FormData includes:                    │
│  - VideoFile                           │
│  - AudioFile                           │
│  - Message (text)                      │
│  - SessionId                           │
│  - CurrentQuestionId                   │
│  - IsTypingSpeedQuestion = true        │
│  - All typing speed data fields        │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Controller: AIInterviewController     │
│  Method: Index (POST)                  │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Process audio (transcribe if needed)  │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Call: UploadBotInterviewVideo()       │
│  - Upload video to Azure Blob          │
│  - Generate SAS URL                    │
│  - Save to BotInterviewQuestionVideos  │
└────────┬───────────────────────────────┘
         │
    ┌────┴────┐
    │ Upload  │
    │Success? │ No
    └────┬────┴──────► Return error modal
         │Yes
         ↓
┌────────────────────────────────────────┐
│  Check: if IsTypingSpeedQuestion &&   │
│  !string.IsNullOrEmpty(TypedText)     │
└────────┬───────────────────────────────┘
         │
    ┌────┴────┐
    │ Typing  │
    │  Test?  │ No
    └────┬────┴──────► Skip typing save
         │Yes
         ↓
┌────────────────────────────────────────┐
│  Create TypingSpeedAssessment object:  │
│  - SessionId                           │
│  - QuestionId                          │
│  - SampleText                          │
│  - TypedText                           │
│  - TimeStarted (parse from string)     │
│  - TimeCompleted (parse from string)   │
│  - TimeTakenInSeconds                  │
│  - TotalCharacters                     │
│  - CorrectCharacters                   │
│  - AccuracyPercentage                  │
│  - WordsPerMinute                      │
│  - CreatedBy = candidateName           │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Call: _aiinterviewerService.          │
│  SaveTypingSpeedAssessment(obj)        │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Service: AIInterviewerService.        │
│  SaveTypingSpeedAssessment()           │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Repository: AIInterviewerRepository.  │
│  SaveTypingSpeedAssessment()           │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Execute: usp_SaveTypingSpeedAssessment│
│  with all parameters                   │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  INSERT INTO TypingSpeedAssessment     │
│  All metrics saved to database         │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Continue with normal interview flow:  │
│  - Log transcript                      │
│  - Fetch next question                 │
│  - Return view with next question      │
└────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────┐
│                         ERROR HANDLING                              │
└─────────────────────────────────────────────────────────────────────┘

At any point, if errors occur:

┌────────────────────────────────────────┐
│  Try-Catch blocks capture exceptions   │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Log via _applicationErrorLogger       │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  For typing speed save failures:       │
│  - Log error                           │
│  - Continue interview (non-blocking)   │
│  - Video and answer still saved        │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  For video upload failures:            │
│  - Return error modal                  │
│  - Interview pauses                    │
│  - Candidate sees error message        │
└────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────┐
│                        DATA RETRIEVAL                               │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────┐
│  Interviewer/    │
│  Reviewer wants  │
│  to see results  │
└────────┬─────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Query TypingSpeedAssessment table:    │
│  SELECT * FROM TypingSpeedAssessment   │
│  WHERE SessionId = '{guid}'            │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  JOIN with InterviewMeetings to get:   │
│  - Candidate name                      │
│  - Candidate email                     │
│  - IRF details                         │
│  - Interview date                      │
└────────┬───────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────┐
│  Display results:                      │
│  - WPM                                 │
│  - Accuracy                            │
│  - Time taken                          │
│  - Link to video recording             │
└────────────────────────────────────────┘
```

## Key Integration Points

```
┌───────────────────────────────────────────────────────────────────┐
│                    EXISTING SYSTEM INTEGRATION                    │
└───────────────────────────────────────────────────────────────────┘

Typing Speed Feature                 Existing AI Interview System
─────────────────────                ───────────────────────────────

IsTypingSpeedAssessment flag  ─────► Question fetch flow (unchanged)
                                     
startRecording() call         ─────► Video recording (unchanged)
                                     
stopRecording() call          ─────► Video recording (unchanged)
                                     
Form submission with          ─────► POST controller action
typing data in FormData              (extended to handle typing data)
                                     
UploadBotInterviewVideo()     ─────► Video storage (unchanged)
                                     
SaveTypingSpeedAssessment()   ─────► New database save
(NEW)                                
                                     
Continue to next question     ─────► Interview flow (unchanged)
```

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         DATA FLOW                               │
└─────────────────────────────────────────────────────────────────┘

    Frontend                Backend              Database
    ────────                ───────              ────────
    
    Modal UI        
       │                    
       │ User clicks Start
       ├───────────────────► startRecording()
       │                           │
       │ User types                │
       │                           │
       │ Timer running             │
       │                           │
       │ User clicks Submit        │
       ├───────────────────► stopRecording()
       │                           │
       │ Calculate metrics         │
       │                           │
       │ Populate form fields      │
       │                           │
       │ Form POST request         │
       ├───────────────────────────────────────► Upload video
       │                                                │
       │                                         Azure Blob
       │                                         Storage
       │                                                │
       │                                                ▼
       │                           ◄───────────────────  Video URL
       │                           │
       │                           Save to
       │                           BotInterviewQuestion
       │                           Videos table
       │                           │
       │                           ▼
       │                           Create Typing
       │                           Assessment object
       │                           │
       │                           ▼
       │                           SaveTypingSpeed
       │                           Assessment()
       │                                   │
       │                                   ▼
       │                                   usp_SaveTyping
       │                                   SpeedAssessment
       │                                   │
       │                                   ▼
       │                                        INSERT INTO
       │                                        TypingSpeed
       │                                        Assessment
       │                                        │
       │ Return next question  ◄───────────────┤
       │                           │
       ▼                           ▼
    Display next             Fetch next
    question                 question
```

## Component Interaction

```
┌─────────────────────────────────────────────────────────────────┐
│                    COMPONENT INTERACTION                        │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────┐         ┌──────────────────┐
│   Index.cshtml   │────────►│  AIInterview     │
│   (View)         │         │  Controller      │
│  ┌────────────┐  │         │                  │
│  │JavaScript  │  │         │  - GET Index()   │
│  │Typing Test │  │         │  - POST Index()  │
│  │Logic       │  │         │                  │
│  └────────────┘  │         └────────┬─────────┘
└──────────────────┘                  │
                                      │
                                      ▼
                          ┌──────────────────┐
                          │ AIInterviewer    │
                          │ Service          │
                          │                  │
                          │ - GetRandom      │
                          │   QuestionFor    │
                          │   Session()      │
                          │ - SaveTyping     │
                          │   SpeedAssess    │
                          │   ment()         │
                          └────────┬─────────┘
                                   │
                                   ▼
                          ┌──────────────────┐
                          │ AIInterviewer    │
                          │ Repository       │
                          │                  │
                          │ - GetRandom      │
                          │   QuestionFor    │
                          │   Session()      │
                          │ - SaveTyping     │
                          │   SpeedAssess    │
                          │   ment()         │
                          └────────┬─────────┘
                                   │
                                   ▼
                          ┌──────────────────┐
                          │  SQL Server      │
                          │                  │
                          │ - usp_GetRandom  │
                          │   QuestionFor    │
                          │   Session        │
                          │ - usp_SaveTyping │
                          │   SpeedAssess    │
                          │   ment           │
                          │                  │
                          │ Tables:          │
                          │ - SkillChannel   │
                          │   SampleQuestions│
                          │ - TypingSpeed    │
                          │   Assessment     │
                          └──────────────────┘
```

---

This flow diagram provides a complete visual representation of how the typing speed assessment feature integrates with the existing AI Interview system.

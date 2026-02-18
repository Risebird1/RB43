# Vapi.ai Interview Booking - Architecture

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         RISEBIRD PORTAL                                  │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    PRESENTATION LAYER                            │   │
│  │                                                                  │   │
│  │  ┌──────────────────┐         ┌──────────────────┐             │   │
│  │  │  Dashboard UI    │         │  Controllers     │             │   │
│  │  │  - Index.cshtml  │◄────────│  - VapiInterview │             │   │
│  │  │  - Call List     │         │    CallController│             │   │
│  │  │  - Analytics     │         │  - Endpoints     │             │   │
│  │  └──────────────────┘         │  - Webhooks      │             │   │
│  │         │                      └────────┬─────────┘             │   │
│  └─────────┼─────────────────────────────┼───────────────────────┘   │
│            │                              │                            │
│  ┌─────────┼──────────────────────────────┼───────────────────────┐   │
│  │         v              BUSINESS LAYER  v                        │   │
│  │                                                                  │   │
│  │  ┌──────────────────────────────────────────────────────────┐  │   │
│  │  │           VapiInterviewService                            │  │   │
│  │  │  - Batch calling                                          │  │   │
│  │  │  - Statistics                                             │  │   │
│  │  │  - Booking processing                                     │  │   │
│  │  │  - Follow-up management                                   │  │   │
│  │  └────────────────────┬──────────────────────────────────────┘  │   │
│  └────────────────────────┼─────────────────────────────────────────┘   │
│                           │                                             │
│  ┌────────────────────────┼─────────────────────────────────────────┐   │
│  │                        v         SERVICE LAYER                   │   │
│  │                                                                  │   │
│  │  ┌───────────────────────┐        ┌──────────────────────┐     │   │
│  │  │   VapiService         │        │  VapiCallRepository  │     │   │
│  │  │  - API Client         │        │  - Database Access   │     │   │
│  │  │  - InitiateCall       │        │  - CRUD Operations   │     │   │
│  │  │  - ProcessWebhook     │        │  - Queries           │     │   │
│  │  │  - Tool Handling      │        │  - Analytics         │     │   │
│  │  └──────────┬────────────┘        └──────────┬───────────┘     │   │
│  └─────────────┼────────────────────────────────┼─────────────────┘   │
│                │                                 │                      │
│                │                                 v                      │
│                │                    ┌────────────────────────┐         │
│                │                    │  SQL Server Database   │         │
│                │                    │  - VapiCallLogs        │         │
│                │                    │  - Interviews          │         │
│                │                    │  - Candidates          │         │
│                │                    └────────────────────────┘         │
│                │                                                        │
└────────────────┼────────────────────────────────────────────────────────┘
                 │
                 │ HTTPS (REST API)
                 │
┌────────────────▼──────────────────────────────────────────────────────┐
│                        VAPI.AI PLATFORM                                │
│                                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │                    VOICE AI ASSISTANT                            │ │
│  │                                                                  │ │
│  │  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐     │ │
│  │  │ Speech-to-   │───▶│     LLM      │───▶│ Text-to-     │     │ │
│  │  │ Text         │    │  Processing  │    │ Speech       │     │ │
│  │  │ (Deepgram)   │    │ (GPT-4o-mini)│    │ (ElevenLabs) │     │ │
│  │  └──────────────┘    └──────────────┘    └──────────────┘     │ │
│  │         │                    │                    │             │ │
│  └─────────┼────────────────────┼────────────────────┼─────────────┘ │
│            │                    │                    │               │
│            └────────────────────┼────────────────────┘               │
│                                 │                                    │
│                    ┌────────────▼───────────────┐                   │
│                    │  Telephony (Twilio/Vonage) │                   │
│                    └────────────┬───────────────┘                   │
└─────────────────────────────────┼─────────────────────────────────────┘
                                  │
                                  │ Phone Call (PSTN)
                                  │
                     ┌────────────▼───────────────┐
                     │  📱 Candidate's Phone      │
                     └────────────────────────────┘
```

---

## 🔄 Call Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CALL LIFECYCLE                                │
└─────────────────────────────────────────────────────────────────────┘

1. INITIATION
   ┌────────────┐
   │   Portal   │ POST /InitiateInterviewCall
   │   Admin    ├────────────────────────────────┐
   └────────────┘                                │
                                                 v
                                        ┌────────────────┐
                                        │  VapiService   │
                                        │  - Create      │
                                        │    request     │
                                        └────────┬───────┘
                                                 │
                                                 v
                                        ┌────────────────┐
                                        │   Vapi API     │
                                        │   POST /call   │
                                        └────────┬───────┘
                                                 │
                                                 v

2. RINGING
   ┌────────────┐                       ┌────────────────┐
   │  Webhook:  │◄──────────────────────│   Vapi sends   │
   │  status-   │  "status": "ringing"  │   webhook      │
   │  update    │                       └────────────────┘
   └────────────┘
        │
        v
   ┌────────────┐
   │  Database  │
   │  Update    │ CallStatus = 'ringing'
   └────────────┘

3. IN PROGRESS
   ┌────────────┐                       ┌────────────────┐
   │ Candidate  │ Answers call          │   Vapi sends   │
   │ Answers    ├──────────────────────▶│   webhook      │
   └────────────┘                       │ "in-progress"  │
                                        └────────┬───────┘
                                                 │
                                                 v
   ┌────────────┐                       ┌────────────────┐
   │  Real-time │◄──────────────────────│   Transcript   │
   │  Transcript│  Webhook events       │   webhooks     │
   └────────────┘                       └────────────────┘

4. TOOL CALLS
   ┌────────────┐
   │    AI:     │ "Let me check available times..."
   │  Needs to  │
   │  check     │
   │  calendar  │
   └─────┬──────┘
         │
         v
   ┌────────────────┐
   │  Webhook:      │ POST /Webhook
   │  tool-calls    ├──────────────────────┐
   └────────────────┘                      │
                                           v
                                  ┌────────────────┐
                                  │  Portal        │
                                  │  Processes     │
                                  │  tool call     │
                                  └────────┬───────┘
                                           │
                                           v
                                  ┌────────────────┐
                                  │  Check         │
                                  │  Calendar      │
                                  │  System        │
                                  └────────┬───────┘
                                           │
                                           v
                                  ┌────────────────┐
                                  │  Return        │
                                  │  Available     │
                                  │  Slots         │
                                  └────────┬───────┘
                                           │
                                           v
   ┌────────────┐                 ┌────────────────┐
   │    AI:     │◄────────────────│  Response      │
   │  "We have  │  JSON result    │  sent to Vapi  │
   │  10 AM..." │                 └────────────────┘
   └────────────┘

5. BOOKING
   ┌────────────┐
   │ Candidate: │ "2 PM works"
   │  Confirms  │
   └─────┬──────┘
         │
         v
   ┌────────────────┐
   │  AI calls      │
   │  book_         │
   │  interview     │
   │  tool          │
   └────────┬───────┘
            │
            v
   ┌────────────────┐
   │  Portal        │ Updates:
   │  - Interview   │ - Interview table
   │  - Sends email │ - Candidate status
   │  - Creates     │ - Calendar event
   │    event       │ - Sends confirmation
   └────────────────┘

6. END OF CALL
   ┌────────────┐
   │  Goodbye   │ Call ends
   └─────┬──────┘
         │
         v
   ┌────────────────┐
   │  Webhook:      │ Complete report:
   │  end-of-call-  │ - Transcript
   │  report        │ - Recording
   └────────┬───────┘ - Cost
            │         - Analysis
            v
   ┌────────────────┐
   │  Database      │ Final update:
   │  Complete      │ - CallEndedAt
   │  Update        │ - Duration
   └────────────────┘ - Transcript
                      - Cost
```

---

## 📊 Data Flow

```
USER ACTION                 SYSTEM                      VAPI.AI
    │                          │                          │
    │  Click "Call"            │                          │
    ├─────────────────────────▶│                          │
    │                          │                          │
    │                          │  POST /call/phone        │
    │                          ├─────────────────────────▶│
    │                          │                          │
    │                          │  200 OK {callId}         │
    │                          │◄─────────────────────────┤
    │                          │                          │
    │  "Call initiated"        │  Save to DB              │
    │◄─────────────────────────┤  INSERT VapiCallLogs     │
    │                          │                          │
    │                          │                          │
    │                          │  Webhook: ringing        │
    │                          │◄─────────────────────────┤
    │                          │  UPDATE CallStatus       │
    │                          │                          │
    │                          │  Webhook: in-progress    │
    │                          │◄─────────────────────────┤
    │                          │  UPDATE CallStatus       │
    │                          │                          │
    │                          │  Webhook: transcript     │
    │                          │◄─────────────────────────┤
    │                          │  Log transcript          │
    │                          │                          │
    │                          │  Webhook: tool-calls     │
    │                          │◄─────────────────────────┤
    │                          │                          │
    │                          │  Process function        │
    │                          │  - check_availability    │
    │                          │  - book_interview        │
    │                          │                          │
    │                          │  Return results          │
    │                          ├─────────────────────────▶│
    │                          │                          │
    │                          │  Webhook: end-of-call    │
    │                          │◄─────────────────────────┤
    │                          │                          │
    │                          │  Final DB update         │
    │                          │  - Transcript            │
    │  Dashboard refresh       │  - Recording             │
    │◄─────────────────────────┤  - Cost                  │
    │  Shows completed call    │  - Booking status        │
    │                          │                          │
```

---

## 🗃️ Database Schema

```sql
VapiCallLogs
├── Id (PK, INT)
├── VapiCallId (VARCHAR, UNIQUE) ──┐
├── InterviewId (FK) ───────────────┤
├── CandidateId (FK) ───────────────┤
├── CandidatePhone                  │
├── CandidateName                   │
├── CandidateEmail                  │
│                                   │
├── Call Details                    │
│   ├── CallStatus                  │
│   ├── CallStartedAt               │
│   ├── CallEndedAt                 │
│   ├── EndedReason                 │
│   └── DurationSeconds             │
│                                   │
├── Content                         │
│   ├── Transcript (MAX)            │
│   ├── RecordingUrl                │
│   └── Summary (MAX)               │
│                                   │
├── Booking Info                    │
│   ├── InterviewBooked (BIT)      │
│   ├── BookedInterviewDateTime     │
│   └── BookingConfirmationDetails  │
│                                   │
├── Metadata                        │
│   ├── AssistantId                 │
│   ├── StructuredData (JSON)       │
│   ├── CallCost                    │
│   ├── CreatedAt                   │
│   ├── UpdatedAt                   │
│   ├── CreatedBy                   │
│   └── UpdatedBy                   │
│                                   │
└── Indexes                         │
    ├── IX_VapiCallId               │
    ├── IX_InterviewId              │
    ├── IX_CandidateId              │
    └── IX_CreatedAt (DESC)         │
```

---

## 🔄 Webhook Event Flow

```
┌──────────────────────────────────────────────────────────────┐
│                    WEBHOOK EVENTS                             │
└──────────────────────────────────────────────────────────────┘

Timeline of events during a call:

0:00   status-update           "queued"
       │
0:02   status-update           "ringing"
       │  └─▶ Candidate's phone rings
       │
0:08   status-update           "in-progress"
       │  └─▶ Candidate answered
       │
0:10   transcript              "Hello! This is Sarah..."
       │  └─▶ Real-time transcript (assistant)
       │
0:15   transcript              "Yes, speaking"
       │  └─▶ Real-time transcript (user)
       │
0:45   tool-calls              check_availability
       │  └─▶ AI needs to check calendar
       │  └─▶ Portal processes and responds
       │
1:20   tool-calls              book_interview
       │  └─▶ AI books the interview
       │  └─▶ Portal updates database
       │
2:30   status-update           "forwarding" (if transfer)
       │  └─▶ Transferring to human agent
       │
3:00   status-update           "ended"
       │  └─▶ Call completed
       │
3:02   end-of-call-report      Full report
       └─▶ Transcript
       └─▶ Recording URL
       └─▶ Cost breakdown
       └─▶ Analysis
       └─▶ Structured data
```

---

## 🔌 API Integration Points

```
┌─────────────────────────────────────────────────────────────────┐
│                  INTEGRATION POINTS                              │
└─────────────────────────────────────────────────────────────────┘

Your System                 Vapi.ai API
────────────                ────────────

InitiateCall()      ────▶   POST /call/phone
                            Request: { phoneNumber, assistantId }
                            Response: { id, status }

GetCallDetails()    ────▶   GET /call/{id}
                            Response: { full call object }

CreateAssistant()   ────▶   POST /assistant
                            Request: { name, model, voice, tools }
                            Response: { id }

ListAssistants()    ────▶   GET /assistant
                            Response: [ {assistant objects} ]

DeleteCall()        ────▶   DELETE /call/{id}
                            Response: 204 No Content

Webhook Handler     ◀────   POST /your-webhook-url
                            Various event types
                            Must respond with tool results
```

---

## 🧩 Component Interaction

```
┌──────────────────────────────────────────────────────────────────┐
│                 COMPONENT INTERACTIONS                            │
└──────────────────────────────────────────────────────────────────┘

Controller
    │
    ├─▶ VapiInterviewService
    │       │
    │       ├─▶ VapiService (API calls)
    │       │       │
    │       │       └─▶ HttpClient ────▶ Vapi.ai API
    │       │
    │       └─▶ VapiCallRepository
    │               │
    │               └─▶ SQL Server
    │
    └─▶ Other Services
            │
            ├─▶ EmailService (confirmations)
            ├─▶ CalendarService (availability)
            ├─▶ InterviewService (booking)
            └─▶ NotificationService (alerts)


Webhook Handling
    │
    ├─▶ Vapi.ai sends POST
    │
    └─▶ Controller receives
            │
            ├─▶ status-update ────▶ Update DB
            │
            ├─▶ transcript ────▶ Log transcript
            │
            ├─▶ tool-calls ────▶ Process function
            │       │
            │       ├─▶ check_availability
            │       │       └─▶ Query calendar
            │       │       └─▶ Return slots
            │       │
            │       └─▶ book_interview
            │               └─▶ Update interview
            │               └─▶ Send confirmation
            │               └─▶ Create calendar event
            │
            └─▶ end-of-call-report ────▶ Final update
                    └─▶ Save transcript
                    └─▶ Save recording
                    └─▶ Calculate cost
```

---

## 🎭 Conversation State Machine

```
┌──────────────────────────────────────────────────────────────────┐
│              CONVERSATION FLOW STATE MACHINE                      │
└──────────────────────────────────────────────────────────────────┘

START
  │
  v
┌─────────────────┐
│  INTRODUCTION   │ "Hello! This is Sarah from Risebird..."
│  - Greet        │
│  - Identify     │
└────────┬────────┘
         │
         v
    ┌────────┐
    │ Person │
    │ Found? │
    └───┬────┘
        │
    Yes │         No
        │         └─▶ WRONG_NUMBER ──▶ END
        v
┌─────────────────┐
│  VERIFICATION   │ "Am I speaking with John Doe?"
│  - Confirm      │
│    identity     │
└────────┬────────┘
         │
         v
┌─────────────────┐
│  PITCH          │ "Calling about Senior Developer position..."
│  - Job details  │ "Would you like to schedule an interview?"
│  - Interview    │
└────────┬────────┘
         │
         v
    ┌────────┐
    │Interest│
    │  ed?   │
    └───┬────┘
        │
    Yes │         No
        │         └─▶ NOT_INTERESTED ──▶ POLITE_CLOSE ──▶ END
        v
┌─────────────────┐
│  AVAILABILITY   │ "When works best for you?"
│  - Ask pref     │
│  - Listen       │
└────────┬────────┘
         │
         v
┌─────────────────┐
│  CHECK_SLOTS    │ [TOOL CALL: check_availability]
│  - Call API     │
│  - Get slots    │
└────────┬────────┘
         │
         v
┌─────────────────┐
│  PROPOSE_TIMES  │ "We have 10 AM, 2 PM, 4 PM available..."
│  - List options │
│  - Wait choice  │
└────────┬────────┘
         │
         v
┌─────────────────┐
│  CONFIRMATION   │ "Tuesday at 2 PM, correct?"
│  - Repeat back  │
│  - Confirm      │
└────────┬────────┘
         │
         v
    ┌────────┐
    │Confirm │
    │  ed?   │
    └───┬────┘
        │
    Yes │         No (back to AVAILABILITY)
        │
        v
┌─────────────────┐
│  BOOK           │ [TOOL CALL: book_interview]
│  - Save booking │
│  - Send email   │
└────────┬────────┘
         │
         v
┌─────────────────┐
│  CLOSING        │ "Interview booked! You'll get email..."
│  - Thank        │ "Any questions?"
│  - Confirm      │
└────────┬────────┘
         │
         v
       END
```

---

## 🛠️ Technology Stack

```
┌──────────────────────────────────────────────────────────────────┐
│                     TECHNOLOGY STACK                              │
└──────────────────────────────────────────────────────────────────┘

Backend
├── .NET 8.0                (Framework)
├── ASP.NET Core           (Web Framework)
├── C#                     (Language)
├── Entity Framework       (Existing ORM)
├── Dapper                 (Existing micro-ORM)
└── SQL Server             (Database)

Vapi.ai Services
├── Deepgram               (Speech-to-Text)
├── OpenAI GPT-4o-mini     (LLM)
├── ElevenLabs             (Text-to-Speech)
└── Twilio                 (Telephony)

Frontend (Existing)
├── Razor Pages            (Views)
├── jQuery                 (JavaScript)
├── Bootstrap              (CSS)
└── Chart.js               (Analytics)

DevOps (Existing)
├── Azure App Service      (Hosting)
├── Azure SQL              (Database)
├── Application Insights   (Monitoring)
└── Azure Key Vault        (Secrets)
```

---

## 🔐 Security Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                   SECURITY LAYERS                                 │
└──────────────────────────────────────────────────────────────────┘

1. Authentication
   ├── Azure AD B2C (User auth)
   └── API Key (Vapi.ai)

2. Data Protection
   ├── HTTPS/TLS (In transit)
   ├── Database encryption (At rest)
   └── Azure Key Vault (Secrets)

3. Webhook Security
   ├── HTTPS only
   ├── IP whitelist (optional)
   └── Signature verification (optional)

4. Compliance
   ├── HIPAA mode (if needed)
   ├── PCI mode (if needed)
   ├── GDPR compliant (call deletion)
   └── Recording consent

5. Access Control
   ├── Role-based access
   ├── Audit logging
   └── IP restrictions
```

---

## 📈 Scalability Design

```
┌──────────────────────────────────────────────────────────────────┐
│                    SCALABILITY                                    │
└──────────────────────────────────────────────────────────────────┘

Current Capacity
├── Concurrent calls: 100+ (Vapi handles)
├── Requests/second: 50+ (Your server)
└── Database: 100,000+ records

Bottlenecks & Solutions
├── Database
│   ├── Indexes on key columns ✓
│   ├── Partitioning by date (future)
│   └── Read replicas (future)
│
├── API Rate Limits
│   ├── Vapi: 100 req/sec (generous)
│   ├── Implement request queuing
│   └── Exponential backoff
│
└── Webhook Processing
    ├── Async processing ✓
    ├── Message queue (future)
    └── Horizontal scaling (Azure)

Growth Path
├── 100 calls/month    ──▶ Current setup ✓
├── 1,000 calls/month  ──▶ Add caching
├── 10,000 calls/month ──▶ Add queue
└── 100,000 calls/month ──▶ Microservices
```

---

## 🎯 Deployment Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                  DEPLOYMENT DIAGRAM                               │
└──────────────────────────────────────────────────────────────────┘

Internet
   │
   v
┌──────────────────┐
│  Azure Front     │ HTTPS, DDoS protection
│  Door / CDN      │
└────────┬─────────┘
         │
         v
┌──────────────────┐
│  Azure App       │ Risebird.Web
│  Service         │ - Controllers
│  (Production)    │ - Views
└────────┬─────────┘
         │
         ├─────────────────┐
         │                 │
         v                 v
┌──────────────┐   ┌─────────────────┐
│  Azure SQL   │   │  Vapi.ai Cloud  │
│  Database    │   │  - Voice AI     │
│  - VapiCall  │   │  - Telephony    │
│    Logs      │   │  - ML Models    │
└──────────────┘   └─────────────────┘
         │
         v
┌──────────────┐
│ App Insights │ Monitoring
│ Log Analytics│ Alerts
└──────────────┘
```

---

## 🔀 Error Handling Flow

```
┌──────────────────────────────────────────────────────────────────┐
│                  ERROR HANDLING                                   │
└──────────────────────────────────────────────────────────────────┘

Call Initiation
    │
    ├─▶ Network Error
    │   └─▶ Retry (3x with backoff)
    │       └─▶ If all fail: Log error, notify admin
    │
    ├─▶ Invalid Phone Number
    │   └─▶ Return error immediately
    │       └─▶ Update candidate record
    │
    ├─▶ Vapi API Error
    │   └─▶ Check status code
    │       ├─▶ 401: Invalid API key
    │       ├─▶ 429: Rate limited (wait & retry)
    │       └─▶ 5xx: Vapi issue (retry later)
    │
    └─▶ Database Error
        └─▶ Log to Application Insights
            └─▶ Continue call, retry save later

During Call
    │
    ├─▶ Webhook Processing Error
    │   └─▶ Log error (don't fail webhook)
    │       └─▶ Return 200 OK (prevent retries)
    │
    ├─▶ Tool Call Error
    │   └─▶ Return error message to AI
    │       └─▶ AI handles gracefully
    │           └─▶ "Let me check with my team..."
    │
    └─▶ Timeout
        └─▶ Return partial result
            └─▶ AI works with what it has

Post-Call
    │
    └─▶ Failed to Book
        └─▶ Mark for follow-up
            └─▶ Send SMS
            └─▶ Schedule retry call
```

---

## 📊 Monitoring Dashboard

```
┌──────────────────────────────────────────────────────────────────┐
│                  MONITORING POINTS                                │
└──────────────────────────────────────────────────────────────────┘

Application Level
├── Request/Response times
├── Error rates
├── Memory usage
└── CPU usage

Vapi Integration Level
├── Call success rate
├── Average call duration
├── Booking conversion rate
├── Cost per call
├── Cost per booking
└── API response times

Business Level
├── Interviews booked
├── Candidate satisfaction
├── Time saved
├── Cost savings
└── ROI

Alerts
├── Error rate > 5%
├── Cost > $X/day
├── Success rate < 60%
└── API downtime
```

---

## 🎨 UI Component Hierarchy

```
VapiInterviewCall/Index.cshtml
│
├── Dashboard Header
│   ├── Title
│   └── Action Buttons
│       ├── [Initiate New Call]
│       └── [Setup Assistant]
│
├── Statistics Cards (4)
│   ├── Total Calls
│   ├── Interviews Booked
│   ├── Conversion Rate
│   └── Total Cost
│
├── Calls Table
│   ├── Headers
│   │   ├── Call ID
│   │   ├── Candidate
│   │   ├── Phone
│   │   ├── Status
│   │   ├── Duration
│   │   ├── Booked
│   │   └── Actions
│   │
│   └── Rows (foreach call)
│       ├── Call Data
│       └── Action Buttons
│           ├── [View Details]
│           └── [Play Recording]
│
├── Modals
│   ├── Initiate Call Modal
│   │   └── Form Fields
│   │       ├── Interview ID
│   │       ├── Candidate Details
│   │       ├── Phone Number
│   │       └── [Start Call]
│   │
│   └── Call Details Modal
│       ├── Status Info
│       ├── Transcript
│       └── Summary
│
└── JavaScript
    ├── initiateCall()
    ├── viewCallDetails()
    ├── loadAnalytics()
    └── pollCallStatus()
```

---

## 🔄 State Transitions

```
┌──────────────────────────────────────────────────────────────────┐
│                 CALL STATUS STATES                                │
└──────────────────────────────────────────────────────────────────┘

    START
      │
      v
  ┌────────┐
  │ QUEUED │ Call request sent to Vapi
  └───┬────┘
      │
      v
  ┌─────────┐
  │ RINGING │ Candidate's phone ringing
  └───┬─────┘
      │
      ├─────────────────────────┐
      │                         │
      v                         v
┌────────────┐           ┌──────────┐
│IN-PROGRESS │           │NO-ANSWER │──▶ END
│(Connected) │           └──────────┘
└──────┬─────┘
       │                 ┌──────────┐
       ├────────────────▶│  BUSY    │──▶ END
       │                 └──────────┘
       │
       │                 ┌──────────┐
       ├────────────────▶│ VOICEMAIL│──▶ LEAVE_MESSAGE ──▶ END
       │                 └──────────┘
       │
       v
┌───────────────┐
│  CONVERSATION │
│  - Talk       │
│  - Check avail│
│  - Book       │
└───────┬───────┘
        │
        v
   ┌─────────┐
   │  ENDED  │
   └────┬────┘
        │
        ├──▶ Booked: InterviewBooked = true
        ├──▶ Not Booked: InterviewBooked = false
        └──▶ Incomplete: InterviewBooked = null
        │
        v
      END
```

---

## 💾 Data Persistence Flow

```
┌──────────────────────────────────────────────────────────────────┐
│                  DATA PERSISTENCE                                 │
└──────────────────────────────────────────────────────────────────┘

Call Initiated
    │
    v
INSERT INTO VapiCallLogs
    ├── VapiCallId
    ├── CandidateId
    ├── CallStatus = 'queued'
    └── CreatedAt

    │
    v
Webhook: status-update (ringing)
    │
    v
UPDATE VapiCallLogs
    ├── CallStatus = 'ringing'
    └── UpdatedAt

    │
    v
Webhook: status-update (in-progress)
    │
    v
UPDATE VapiCallLogs
    ├── CallStatus = 'in-progress'
    ├── CallStartedAt
    └── UpdatedAt

    │
    v
Webhook: tool-calls (booking)
    │
    v
UPDATE VapiCallLogs
    ├── InterviewBooked = true
    ├── BookedInterviewDateTime
    ├── BookingConfirmationDetails
    └── UpdatedAt

    │
    v
Webhook: end-of-call-report
    │
    v
UPDATE VapiCallLogs (FINAL)
    ├── CallStatus = 'ended'
    ├── CallEndedAt
    ├── EndedReason
    ├── DurationSeconds
    ├── Transcript
    ├── RecordingUrl
    ├── Summary
    ├── StructuredData
    ├── CallCost
    └── UpdatedAt
```

---

## 🎯 Decision Tree for Developers

```
Start Here
    │
    v
Do you want to...
    │
    ├─▶ Test quickly?
    │   └─▶ Read: VAPI_QUICKSTART_GUIDE.md
    │
    ├─▶ Implement properly?
    │   └─▶ Read: IMPLEMENTATION_GUIDE.md
    │
    ├─▶ Understand architecture?
    │   └─▶ Read: This file + VAPI_SOLUTION_SUMMARY.md
    │
    ├─▶ See code examples?
    │   └─▶ Read: VapiIntegrationExamples.md
    │
    ├─▶ Troubleshoot issues?
    │   └─▶ Read: TestVapiIntegration.md
    │
    └─▶ Just configure?
        └─▶ Read: SERVICE_REGISTRATION_GUIDE.md
```

---

## 📞 System Capabilities Matrix

| Feature | Supported | Configuration Required |
|---------|-----------|------------------------|
| Outbound calls | ✅ Yes | API key only |
| Inbound calls | ✅ Yes | Phone number purchase |
| Voicemail detection | ✅ Yes | Enable in config |
| Call recording | ✅ Yes | Enabled by default |
| Transcription | ✅ Yes | Enabled by default |
| Multi-language | ✅ Yes | Configure language |
| Custom tools | ✅ Yes | Add tool definitions |
| Calendar integration | ⚙️ Ready | Implement handlers |
| Email integration | ⚙️ Ready | Implement handlers |
| SMS fallback | ⚙️ Ready | Use Twilio integration |
| Analytics | ✅ Yes | Built-in |
| Cost tracking | ✅ Yes | Built-in |

Legend:
- ✅ Yes = Works immediately
- ⚙️ Ready = Code provided, needs your integration

---

## 🎊 Final Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    HIGH-LEVEL ARCHITECTURE                       │
└─────────────────────────────────────────────────────────────────┘

┌────────────────┐         ┌────────────────┐
│   Recruiter    │         │   Candidate    │
│   (Portal)     │         │   (Phone)      │
└───────┬────────┘         └───────▲────────┘
        │                          │
        │ Clicks "Call"            │ Receives call
        │                          │
        v                          │
┌────────────────────────────────┐│
│    Risebird Application        ││
│                                 ││
│  ┌──────────────────────────┐  ││
│  │ VapiInterviewCall        │  ││
│  │ Controller               │  ││
│  │ - InitiateCall()        │  ││
│  │ - Webhook()             │  ││
│  └──────────┬───────────────┘  ││
│             │                   ││
│  ┌──────────▼───────────────┐  ││
│  │ VapiService              │  ││
│  │ - HTTP Client            │  ││
│  │ - API Integration        │  ││
│  └──────────┬───────────────┘  ││
│             │                   ││
│  ┌──────────▼───────────────┐  ││
│  │ VapiCallRepository       │  ││
│  │ - Database Access        │  ││
│  └──────────┬───────────────┘  ││
│             │                   ││
│             v                   ││
│     ┌──────────────┐            ││
│     │  SQL Server  │            ││
│     │  VapiCallLogs│            ││
│     └──────────────┘            ││
└────────────────────────────────┘│
        │                          │
        │ HTTPS API                │
        │                          │
        v                          │
┌────────────────────────────────┐│
│        Vapi.ai Cloud            ││
│                                 ││
│  ┌──────────────────────────┐  ││
│  │  Voice AI Engine         │  ││
│  │  - STT → LLM → TTS       │  ││
│  └──────────┬───────────────┘  ││
│             │                   ││
│  ┌──────────▼───────────────┐  ││
│  │  Telephony Network       │  ││
│  │  (Twilio)                │  ││
│  └──────────┬───────────────┘  ││
└─────────────┼──────────────────┘│
              │                    │
              │ PSTN/VoIP         │
              │                    │
              └────────────────────┘
```

---

## 🎉 You're Ready!

**Everything is set up and documented.** 

**Choose your starting point above and begin! 🚀**

**Questions?** All guides have troubleshooting sections.

**Stuck?** Check the Discord community or Vapi support.

**Success?** Share your results with the team!

---

**Good luck automating your interview process! 🌟**

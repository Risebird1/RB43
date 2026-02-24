# Schema Clarification: IRF Table Relationships

## ⚠️ CRITICAL: Understanding IRFId Across Tables

The column name **"IRFId"** appears in multiple tables but has **different meanings** in each context.

---

## Complete Schema Mapping

| Table | Column | Type | References/Stores |
|-------|--------|------|-------------------|
| **IRF** | `id` | INT | Auto-increment Primary Key (e.g., 1, 2, 3) |
| **IRF** | `IRFId` | VARCHAR(200) | Business identifier (e.g., "IRF-2026-001", "SKL-2026-042") |
| **Candidate** | `IRFId` | INT | Foreign Key → IRF.id |
| **VapiCallLogs** | `IRFId` | INT | Foreign Key → IRF.id |
| **InterviewMeetings** | `IRFId` | VARCHAR(200) | Foreign Key → IRF.IRFId |

---

## The Key Insight

**IRF table has TWO identifier columns**:
1. `id` (INT) - Used for internal foreign key relationships (Candidate, VapiCallLogs)
2. `IRFId` (VARCHAR) - Business identifier used for display (InterviewMeetings)

**Different tables reference different columns from IRF**:
- Candidate.IRFId → IRF.id (INT to INT)
- VapiCallLogs.IRFId → IRF.id (INT to INT)
- InterviewMeetings.IRFId → IRF.IRFId (VARCHAR to VARCHAR)

---

## Example Data

### IRF Table
```
id  | IRFId         | (Other columns...)
----|---------------|-------------------
1   | IRF-2026-001  | Java Developer, Client A, ...
2   | IRF-2026-002  | Python Developer, Client B, ...
3   | IRF-2026-003  | DevOps Engineer, Client A, ...
```

### Candidate Table
```
id | IRFId | Name      | EmailId
---|-------|-----------|------------------
10 | 1     | John Doe  | john@example.com
11 | 2     | John Doe  | john@example.com
12 | 3     | Jane Doe  | jane@example.com
```
Note: Candidate.IRFId stores IRF.id (1, 2, 3), not IRF.IRFId

### InterviewMeetings Table
```
id | IRFId         | candidateEmail      | comfirmationStatus
---|---------------|---------------------|-------------------
1  | IRF-2026-001  | john@example.com    | 1
2  | IRF-2026-002  | alice@example.com   | 1
```
Note: InterviewMeetings.IRFId stores IRF.IRFId ("IRF-2026-001"), not IRF.id

### VapiCallLogs Table
```
Id | IRFId | RecipientEmail      | CallStatus
---|-------|---------------------|------------
1  | 1     | john@example.com    | ended
2  | 2     | john@example.com    | ended
3  | 3     | jane@example.com    | ended
```
Note: VapiCallLogs.IRFId stores IRF.id (1, 2, 3), not IRF.IRFId

---

## Correct JOIN Patterns

### ✅ Joining Candidate with IRF

```sql
-- Uses IRF.id (INT)
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id
-- c.IRFId (INT) = irf.id (INT) ✅
```

### ✅ Joining VapiCallLogs with IRF

```sql
-- Uses IRF.id (INT)
FROM VapiCallLogs vcl
INNER JOIN IRF irf ON vcl.IRFId = irf.id
-- vcl.IRFId (INT) = irf.id (INT) ✅
```

### ✅ Joining InterviewMeetings with IRF

```sql
-- Uses IRF.IRFId (VARCHAR)
FROM InterviewMeetings im
INNER JOIN IRF irf ON im.IRFId = irf.IRFId
-- im.IRFId (VARCHAR) = irf.IRFId (VARCHAR) ✅
```

### ✅ Joining Candidate with VapiCallLogs

```sql
-- Both reference IRF.id (INT)
FROM Candidate c
INNER JOIN VapiCallLogs vcl ON c.IRFId = vcl.IRFId
-- c.IRFId (INT) = vcl.IRFId (INT) ✅
```

### ❌ WRONG: Joining Candidate/VapiCallLogs with InterviewMeetings

```sql
-- WRONG: Comparing INT with VARCHAR
FROM Candidate c
INNER JOIN InterviewMeetings im ON c.IRFId = im.IRFId
-- c.IRFId (INT: 1) ≠ im.IRFId (VARCHAR: "IRF-2026-001") ❌

-- CORRECT: Must join through IRF table
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id
INNER JOIN InterviewMeetings im ON im.IRFId = irf.IRFId
-- Step 1: c.IRFId (INT: 1) = irf.id (INT: 1) ✅
-- Step 2: im.IRFId (VARCHAR: "IRF-2026-001") = irf.IRFId (VARCHAR: "IRF-2026-001") ✅
```

---

## Our Stored Procedure - All Comparisons

### Line 68: Get IRF Details
```sql
INNER JOIN IRF irf ON c.IRFId = irf.id
```
✅ Candidate.IRFId (INT) = IRF.id (INT)

### Line 72: Count Call Attempts  
```sql
LEFT JOIN CandidateCallCounts ccc 
    ON COALESCE(c.EmailId, cm.EmailId, u.EmailId) = ccc.RecipientEmail 
    AND c.IRFId = ccc.IRFId
```
✅ Candidate.IRFId (INT) = VapiCallLogs.IRFId (INT)

### Line 85: Check InterviewMeetings
```sql
AND NOT EXISTS (
    SELECT 1 
    FROM InterviewMeetings im
    WHERE im.candidateEmail = COALESCE(c.EmailId, cm.EmailId, u.EmailId)
    AND im.IRFId = irf.IRFId  -- ✅ CORRECT!
    AND im.comfirmationStatus = '1'
)
```
✅ InterviewMeetings.IRFId (VARCHAR) = IRF.IRFId (VARCHAR)

### Line 97: Check Booking Status
```sql
OR NOT EXISTS (
    SELECT 1 
    FROM VapiCallLogs vcl
    WHERE vcl.RecipientEmail = COALESCE(c.EmailId, cm.EmailId, u.EmailId)
    AND vcl.IRFId = c.IRFId  -- ✅ CORRECT!
    AND vcl.InterviewBooked = 1
)
```
✅ VapiCallLogs.IRFId (INT) = Candidate.IRFId (INT)

---

## Why VapiCallLogs Uses INT (Not VARCHAR)

We store `IRF.id` (INT) in `VapiCallLogs.IRFId` because:

1. **Consistency**: Matches `Candidate.IRFId` type (INT)
2. **Performance**: Integer comparisons and grouping are faster
3. **Normalization**: Stores the primary key reference
4. **CTE Efficiency**: Grouping by INT is more efficient than VARCHAR

**Trade-off**: When joining with InterviewMeetings, we need to go through the IRF table, but this is acceptable since we already join IRF for other reasons (getting IRF name for display).

---

## 🧪 Test Query to Verify Schema Understanding

```sql
-- This query demonstrates the correct relationships
SELECT 
    'IRF' AS TableName,
    irf.id AS IRF_id_INT,
    irf.IRFId AS IRF_IRFId_VARCHAR,
    NULL AS StoredValue,
    NULL AS MatchType
FROM IRF irf
WHERE irf.id <= 3

UNION ALL

SELECT 
    'Candidate' AS TableName,
    irf.id AS IRF_id_INT,
    irf.IRFId AS IRF_IRFId_VARCHAR,
    CAST(c.IRFId AS VARCHAR) AS StoredValue,
    'Uses IRF.id (INT)' AS MatchType
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id
WHERE c.id <= 3

UNION ALL

SELECT 
    'InterviewMeetings' AS TableName,
    irf.id AS IRF_id_INT,
    irf.IRFId AS IRF_IRFId_VARCHAR,
    im.IRFId AS StoredValue,
    'Uses IRF.IRFId (VARCHAR)' AS MatchType
FROM InterviewMeetings im
INNER JOIN IRF irf ON im.IRFId = irf.IRFId
WHERE im.id <= 3

UNION ALL

SELECT 
    'VapiCallLogs' AS TableName,
    irf.id AS IRF_id_INT,
    irf.IRFId AS IRF_IRFId_VARCHAR,
    CAST(vcl.IRFId AS VARCHAR) AS StoredValue,
    'Uses IRF.id (INT)' AS MatchType
FROM VapiCallLogs vcl
INNER JOIN IRF irf ON vcl.IRFId = irf.id
WHERE vcl.Id <= 3;
```

---

## 📋 Developer Guidelines

### When Writing Queries Involving IRF

**Rule 1**: Always join with IRF table first
```sql
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id  -- Get IRF details
```

**Rule 2**: Use the correct IRF column based on the other table

| Joining Table | Column Type | IRF Column to Use |
|---------------|-------------|-------------------|
| Candidate | INT | irf.id |
| VapiCallLogs | INT | irf.id |
| InterviewMeetings | VARCHAR | irf.IRFId |

**Rule 3**: Never directly compare INT columns with VARCHAR columns
```sql
-- ❌ WRONG
WHERE Candidate.IRFId = InterviewMeetings.IRFId

-- ✅ CORRECT
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id
WHERE irf.IRFId = InterviewMeetings.IRFId
```

---

## 🎯 Quick Reference

### Common Query Patterns

#### Get Candidate's IRF Identifier (VARCHAR)
```sql
SELECT c.*, irf.IRFId AS IRFIdentifier
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id;
```

#### Check if Candidate Has Interview Meeting
```sql
SELECT c.*
FROM Candidate c
INNER JOIN IRF irf ON c.IRFId = irf.id
WHERE EXISTS (
    SELECT 1 FROM InterviewMeetings im
    WHERE im.candidateEmail = c.EmailId
    AND im.IRFId = irf.IRFId  -- Use irf.IRFId!
);
```

#### Count Vapi Calls Per IRF
```sql
SELECT 
    irf.IRFId AS IRFIdentifier,
    COUNT(*) AS TotalCalls
FROM VapiCallLogs vcl
INNER JOIN IRF irf ON vcl.IRFId = irf.id  -- Use irf.id!
GROUP BY irf.IRFId;
```

---

## Summary

### The Correct Relationships

```
IRF Table:
  - id (INT) ←──────┬─── Candidate.IRFId (INT)
  - IRFId (VARCHAR) │
                    └─── VapiCallLogs.IRFId (INT)
  
  - IRFId (VARCHAR) ←─── InterviewMeetings.IRFId (VARCHAR)
```

### Join Rules

| Join | Syntax |
|------|--------|
| IRF ↔ Candidate | `irf.id = c.IRFId` |
| IRF ↔ VapiCallLogs | `irf.id = vcl.IRFId` |
| IRF ↔ InterviewMeetings | `irf.IRFId = im.IRFId` |
| Candidate ↔ VapiCallLogs | `c.IRFId = vcl.IRFId` (both INT) |

---

**Status**: ✅ All Relationships Documented  
**Stored Procedure**: ✅ Uses Correct Joins  
**Date**: February 24, 2026  

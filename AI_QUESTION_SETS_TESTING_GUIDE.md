# Manual Test Guide — AI Interview Question Sets (Set Rotation)

## What this feature does, in plain terms

A **preset AI interview** skill channel holds a pool of questions. Before this feature, every
candidate on that channel was asked the same questions in the same order, so anyone who
interviewed second already knew what was coming.

This feature deals that pool into **balanced sets** — think exam papers. Every set covers every
rubric parameter exactly once, no question appears in two sets, and each interview is locked to a
single set for its whole life. Candidates booked together therefore get different papers.

Whether a channel does this at all is an explicit switch on the channel (**Use these sets for
interviews on this channel**). A channel with the switch off behaves exactly as it did before the
feature existed.

### The one thing most likely to be mis-reported as a bug

**Switching sets on changes how many questions an interview asks.** A set holds exactly one
question per rubric parameter. So a channel with 2 rubric parameters and 6 questions asks
**2 questions** once sets are in use, not 6. That is the intended design, not a defect. If you see
the interview finish "early", check the rubric parameter count first.

---

## Part 0 — Preconditions (read before starting)

### 0.1 Database scripts, in this exact order

The stored procedures reference a new column, and SQL Server validates columns when a procedure is
created. Applying them out of order fails outright.

1. `Risebird.DB\Tables\IRF.sql` — adds `UseAIQuestionSets`
2. `Risebird.DB\Tables\AIQuestionSets.sql`
3. `Risebird.DB\Tables\AIQuestionSetItems.sql`
4. `Risebird.DB\Tables\InterviewAIQuestionSet.sql`
5. `Risebird.DB\TableTypes\AIQuestionSetItems_TableType.sql`
6. **`Risebird.DB\Migrations\Backfill_UseAIQuestionSets_ForChannelsWithSets.sql`** — mandatory, not optional
7. `Risebird.DB\StoredProcedures\usp_SaveAIQuestionSets.sql`
8. `Risebird.DB\StoredProcedures\usp_AssignAIQuestionSetToInterview.sql`
9. `Risebird.DB\StoredProcedures\usp_SetAIQuestionSetsEnabled.sql`
10. `Risebird.DB\StoredProcedures\usp_GetAIQuestionSetsByIRF.sql`
11. `Risebird.DB\StoredProcedures\usp_GetAIQuestionPoolForSets.sql`
12. `Risebird.DB\StoredProcedures\usp_GetNextAIQuestionForInterview.sql`
13. `Risebird.DB\StoredProcedures\usp_DeleteSkillSampleQuestions.sql` — carries the guard tested in
    Part 12

Ask the deploying developer to confirm step 6 ran and to tell you how many channels it switched on
(it prints the count). Skipping it would silently revert any channel that already deals papers.

### 0.2 BLOCKER — configuration key

Set creation calls a stored OpenAI prompt. The key is currently **empty** in `appsettings.json`:

```json
"SetCreationPromptId": "",
```

While it is empty, clicking **Create sets** returns:

> Question set creation is not configured yet: OpenAI:SetCreationPromptId is empty. Add the stored
> prompt id to configuration and try again.

**You cannot test anything from Part 3 onward until this is filled in.** Raise it with the
developer/ops before you begin. Everything in Parts 1 and 2, and test case TC-06, can be tested
without it.

### 0.3 Login you need

| Profile | Can see and change question sets? |
|---|---|
| Delivery Manager, Admin, or any other internal operational profile | Yes |
| Client, Public | No — must be refused (this is a test case, see Part 8) |
| Interviewer | Only if they are an assigned SME on that specific channel |

Use an internal operational login for Parts 1–7. You will need a Client/Public login for Part 8.

### 0.4 Environment

- Use a **test/staging** environment. Several cases book and run real interviews.
- Have a DBA or developer available for the SQL checks in Appendix A and for Part 10.
- Have access to the application error log (the feature writes deliberate Error/Warning lines).

---

## Part 1 — Create the test channel and check the new form fields

### TC-01 — "Is AI Interview" appears on the Add screen

1. Go to **Skills Channel → Add** (`/IRF/Add`).
2. Fill in the mandatory fields as you normally would.
3. Find the checkbox row containing **Is Tech QC**.

**Expected:** a new checkbox **Is AI Interview** sits in that row. It is unticked.

### TC-02 — Interview type is required when the box is ticked

1. Tick **Is AI Interview**.

   **Expected:** an **AI Interview Type** dropdown appears below it, showing "Select".

2. Leave the dropdown on "Select" and submit the form.

   **Expected:** the form does **not** submit. The message **"Please select an AI interview type."**
   appears next to the dropdown.

### TC-03 — Question sets state shows only for Preset, and is read-only

1. In **AI Interview Type**, choose **Adaptive**.

   **Expected:** no question sets block appears.

2. Change it to **Preset**.

   **Expected:** a **Question sets** block appears reading *"Not in use. Every candidate is asked
   this channel's questions in its standard order."* with a note that it is read-only and is set up
   from the Delivery Manager page.

3. Try to tick that checkbox.

   **Expected:** it cannot be ticked (disabled).

4. Switch back to **Adaptive**.

   **Expected:** the Question sets block disappears again.

5. Set it back to **Preset** and save the channel.

**Note the channel's numeric id and its channel code** — you need both for the SQL checks. The code
is the value shown as the channel/IRF id (a string like `RB-1234`).

### TC-04 — Edit screen mirrors the state, still read-only

1. Open the channel in **Edit** (`/IRF/EditIRF?id={numeric id}`).

**Expected:**
- **Is AI Interview** is ticked and **AI Interview Type** is **Preset**.
- The **Question sets** block shows *"Not in use…"* and cannot be ticked.
- Ticking off the type (setting it back to "Select") and submitting is refused with the same
  **"Please select an AI interview type."** message as TC-02.

---

## Part 2 — Build the rubric and the question pool

This part decides everything that follows, so follow the numbers exactly.

**The rule that governs set count:** the number of sets equals the rubric parameter with the
**fewest** questions. Every set must cover every parameter, and no question is reused, so the
thinnest parameter caps the channel.

### TC-05 — Create the fixture

1. On the channel's **Tech Skill** rubric, create exactly **two** parameters. Suggested names:
   - `System Design`
   - `Concurrency`

2. On the **Delivery Manager** page for the channel
   (`/IRF/DeliveryHeadInstruction?irfid={numeric id}`), in the **Sample Questions** section, add
   **three questions under each parameter** — six in total. Make the text obviously distinguishable,
   for example:

   | Rubric parameter | Questions |
   |---|---|
   | System Design | SD-1 rate limiter, SD-2 URL shortener, SD-3 notification fan-out |
   | Concurrency | CON-1 data race, CON-2 optimistic locking, CON-3 deadlock |

   The question's skill **must** be one of the two rubric parameters. A question filed under
   anything else can never be dealt into a set.

3. Mark **exactly two** questions as coding questions — **SD-3 and CON-3**, one under each rubric
   parameter. Two, not one, so the spreading rule in TC-07 has something to spread. For each:
   - Edit the question and tick the coding-question option.
   - Supply a **question gist** — this is mandatory. Marking a question as coding without a gist is
     refused, because the avatar narrates the gist instead of reading the code problem aloud.
   - Set a **time limit** (must be between 1 and 180 minutes; use 5 for testing).

**Expected fixture:** 2 rubric parameters × 3 questions = **3 sets of 2 questions each**, and 2
coding questions to be spread across those 3 sets.

---

## Part 3 — The switch cannot be turned on before sets exist

### TC-06 — Refusal when there are no sets *(testable without the config key)*

1. On the Delivery Manager page, expand **AI interview question sets**.

   **Expected:** the panel opens and reads *"No sets have been created for this channel yet."* The
   header count reads *"no sets yet"*.

2. The panel shows a **Use these sets for interviews on this channel** switch, currently **Off**.
   Turn it **On**.

**Expected:**
- The switch snaps straight back to **Off**.
- A message appears: **"Create the question sets first. A channel switched on with no sets cannot
  serve any questions."**

**Why this matters:** a channel switched on with no sets cannot serve a single question — the
system refuses to fall back to the shared question order. This guard is what stops that state from
being reachable at all.

3. Reload the page and confirm the switch is still **Off** (nothing was saved).

---

## Part 4 — Create the sets

### TC-07 — Sets are created and balanced

1. Click **Create sets**.

**Expected:**
- Message: **"Created 3 question set(s)."**
- The header count reads **"3 sets"**.
- A note appears: *"These sets are not in use yet. Switch this channel on to start dealing them."*
- Three sets listed as **Set A**, **Set B**, **Set C**.

2. Inspect each set and verify all four of these:

| Check | Expected |
|---|---|
| Size | Each set holds exactly **2** questions |
| Coverage | Each set has exactly one **System Design** and one **Concurrency** question |
| No reuse | All six questions appear, each in exactly one set — no question in two sets |
| Ask order | Within each set, **System Design** is listed first, because it is first in the rubric — *not* alphabetical order |

3. **Coding questions are spread, not piled up.** With 2 coding questions and 3 sets, the correct
   result is **two sets holding one coding question each, and one set holding none**. Note which
   sets they are — TC-17 needs one of them.

   A set holding **both** coding questions while another holds none is a defect: those two
   candidates would sit materially different interviews, one writing code twice against a clock and
   the other not at all. Report it as **high** severity and ask the developer to check the
   application log, which records an uneven spread as a **Warning** naming the per-set counts.

   A gap of one is unavoidable and correct here: 2 does not divide by 3.

4. Each set shows **"assigned to 0 interview(s)"**.

Appendix A.2 and A.3 give SQL that proves the same things independently.

### TC-08 — A parameter with no questions is refused

1. Add a **third** rubric parameter (`Testing`) and give it **no** questions.
2. Click **Create sets**.

**Expected:** refusal naming the empty parameter:

> Rubric parameter "Testing" has no questions, so no set can cover the full rubric. Add a question
> for it and try again.

3. Confirm the existing three sets are unchanged (still 3 sets, same contents).
4. **Remove the `Testing` parameter** before continuing.

### TC-09 — The thinnest parameter caps the set count

> **Order matters here.** Do this case *before* Part 6, while no interview is booked. Once an
> interview is locked to a paper the channel's pool is frozen and the delete is refused (TC-29).
> Deleting a dealt question now discards the channel's sets, which is expected — step 3 re-creates
> them. TC-31 covers that behaviour on purpose.

1. Delete one **Concurrency** question, leaving System Design with 3 and Concurrency with 2.
2. Click **Create sets**.

**Expected:** **2** sets are created, not 3 — *"Created 2 question set(s)."* One System Design
question is left unused, which is correct.

3. Restore the deleted question and re-create so you are back to **3 sets** before Part 5. (If
   re-creation is refused because sets are already bound to interviews, you have run Part 6 out of
   order — use a fresh channel.)

---

## Part 5 — Switch the channel on

### TC-10 — Turning it on

1. Turn the **Use these sets** switch **On**.

**Expected:** the switch stays **On** and the message reads **"This channel now deals questions
from balanced sets. Each new interview is locked to one set."**

2. Reload the Delivery Manager page.

   **Expected:** the switch is still **On**.

3. Open the channel in **Edit**.

   **Expected:** the read-only **Question sets** block now reads *"In use. Each interview is locked
   to one balanced set, so no two candidates in a batch answer the same paper."*

---

## Part 6 — Rotation (the core of the feature)

Sets are bound to an interview **when it is booked**, not only when the candidate starts. So you can
verify rotation before anyone launches anything.

### TC-11 — Three bookings get three different papers

1. Book **three** interviews on this channel, for three different candidates (A, B, C), through the
   normal scheduling flow.
2. Reload the Delivery Manager page and re-open the sets panel.

**Expected:** each of **Set A, Set B and Set C** shows **"assigned to 1 interview(s)"**. No set has
2 and none has 0.

3. Confirm with SQL (Appendix A.4): three rows in `InterviewAIQuestionSet`, three **distinct**
   `SetId` values.

### TC-12 — The fourth booking reuses the least-used set

1. Book a **fourth** interview (candidate D) on the same channel.

**Expected:** all three sets were tied at 1, so the tie is broken by age and the **oldest set
(Set A)** is picked. Set A now shows **"assigned to 2 interview(s)"**; B and C stay at 1.

### TC-13 — Re-creating sets is refused once they are in use

1. Click **Create sets** again.

**Expected:** refusal —

> Question sets for this channel are already in use by one or more interviews and cannot be
> re-created. Nothing was saved.

2. Confirm the three sets and their contents are unchanged.

---

## Part 7 — Run an interview

Get each candidate's session id (Appendix A.4 returns it) and open the interview at:

```
/AIInterview/Index?sid={SessionId}
```

### TC-14 — Questions come only from the bound set, in rubric order

1. Launch candidate **A**'s interview.

**Expected:** the first question is the **System Design** question from **A's set** — not
necessarily the channel's first question, and not the same as candidate B will get.

2. Answer it and submit.

   **Expected:** the next question is the **Concurrency** question from the **same** set.

3. Cross-check against the sets panel: the two questions asked match A's set exactly, in that order.

### TC-15 — The interview ends when the set is exhausted

1. Answer the second question and submit.

**Expected:** the interview closes with **"Interview Complete — The interviewer is through with all
the questions. This interview is now closed."**

**This is the expectation from the warning at the top of this guide:** the interview asked
**2** questions (one per rubric parameter), not all 6 in the channel. That is correct behaviour.

2. Confirm the interview is marked completed and a report is generated, as for any other AI
   interview.

### TC-16 — Reconnect resumes on the same paper

1. Launch candidate **B**'s interview and answer only the **first** question.
2. Close the browser tab, then reopen the same `sid` URL.

**Expected:**
- The same set, and the interview resumes on the **second** question — it does not restart, and it
  does not jump to a question from another set.
- The transcript so far is intact.

### TC-17 — A coding question served from a set behaves normally

1. Launch the interview for a candidate bound to one of the sets that holds a coding question
   (from TC-07 step 3).
2. Reach the coding question.

**Expected:**
- The coding IDE opens.
- The countdown reflects the time limit you set in TC-05 (5 minutes).
- The avatar narrates the **question gist**, not the raw question text.
- The spoken-answer and video requirements are waived, exactly as for a coding question served the
  old way.
- Submitted code is saved against the question.

3. Reload the page while on the coding question.

   **Expected:** the IDE reopens with the coding state intact — it does not degrade into a plain
   spoken question.

### TC-18 — Two candidates genuinely get different papers

1. Compare the questions asked to candidates A and B (from their transcripts).

**Expected:** the two sets of questions are different. Both cover both rubric parameters. This is
the whole point of the feature — if A and B were asked the same questions, that is a **critical
defect**.

---

## Part 8 — Confidentiality

Question sets must never reach a candidate or a client. There are three independent guards and all
three should hold.

### TC-19 — Client share link cannot see sets

1. Open the channel's client share link (the `ublc` / client-share entry to the Delivery Manager
   page).

**Expected:** the **AI interview question sets** section is **completely absent** — not present and
collapsed, not present and empty. View the page source and confirm there is no
`dmV4QuestionSetsPanel` element at all.

### TC-20 — Client and Public logins cannot see sets

1. Log in with a **Client** profile and open the same channel's Delivery Manager page.

**Expected:** no question sets section.

2. Repeat with a **Public** profile.

**Expected:** no question sets section.

### TC-21 — The endpoints refuse a client caller

Ask the developer to help with this one, or use browser dev tools while logged in as **Client**.

1. `POST /IRF/GetAIQuestionSetsByIRF` with `irfid={numeric id}`.

   **Expected:** `{"success":false,"message":"Forbidden"}` — and no set data anywhere in the
   response.

2. `POST /IRF/SetAIQuestionSetsEnabled` with `irfid={numeric id}` and `enabled=false`.

   **Expected:** `{"success":false,"message":"Forbidden"}`, and the channel's switch is **unchanged**
   when you check it as an internal user afterwards.

### TC-22 — The candidate sees nothing about sets

1. During any interview from Part 7, inspect the candidate page and its source.

**Expected:** no set name, no set letter, no scores, no time or point totals, and no sign of any
question other than the one currently being asked.

---

## Part 9 — Switching off, and "locked for life"

This is the subtlest behaviour in the feature. An interview already on a paper must keep it, but new
interviews must respect the switch.

### TC-23 — A candidate mid-interview keeps their paper

1. Launch candidate **C**'s interview and answer only the **first** question. Leave it open.
2. As an internal user, turn the channel's **Use these sets** switch **Off**.

   **Expected:** message *"This channel no longer deals questions from sets. Interviews already
   bound to a set keep it."*

3. Go back to candidate C and answer the current question.

**Expected:** the next question is still the **second question of C's original set**. The question
order must **not** change under a candidate who is already part-way through. If C suddenly gets a
question from the channel's standard order, that is a **critical defect**.

### TC-24 — A new interview on a switched-off channel uses the standard order

1. With the switch still **Off**, book a **new** interview (candidate E) on the channel.

**Expected:** the sets panel counts are **unchanged** (no set gained an interview). SQL (A.4) shows
**no** `InterviewAIQuestionSet` row for candidate E's interview.

2. Launch candidate E's interview and walk through it.

**Expected:** E is asked the channel's questions in the standard order — all six, starting from the
first — exactly as the channel behaved before the feature existed.

3. Ask the developer to check the application log.

   **Expected:** a **Warning** line saying the channel has question sets but is not switched on to
   use them.

### TC-25 — Switching back on

1. Turn the switch back **On**.
2. Book another interview (candidate F).

**Expected:** F is bound to a set again, and the least-used set is chosen.

---

## Part 10 — Fail-closed check

This proves the system refuses to leak the pool when it cannot resolve a set. The toggle will not
create this state, but **TC-31 in Part 12 reaches it from the UI**, so no database access is needed.
The SQL route below is an alternative if a DBA is on hand.

> **Do this on the test channel only, in a test environment.**

### TC-26 — Switched on with no sets refuses to serve

1. Reach the state in one of two ways:
   - **No DBA needed:** follow **TC-31** in Part 12. Deleting a dealt question on a switched-on
     channel with no paper in use discards its sets and leaves it switched on with none.
   - **With a DBA:** run Appendix A.5 against the test channel, which deletes its sets while
     leaving `UseAIQuestionSets = 1`.
2. Book and launch a **new** interview on that channel.

**Expected:**
- The interview does **not** ask any question from the channel pool. It closes immediately with the
  "Interview Complete" screen.
- The application log contains an **Error** line stating the channel is switched on for question
  sets but has none, and that it is refusing to serve questions outside a set.

**What must NOT happen:** the candidate being asked the channel's questions in the standard order.
Serving the pool here would hand every candidate the same questions, which is exactly what the
feature exists to prevent.

3. Restore the channel: re-create the sets, or switch the channel off.

---

## Part 11 — Regression: channels that do not use this feature

### TC-27 — Nothing else changed

Verify each of these behaves exactly as it did before this release:

1. An **Adaptive** AI interview channel — no question sets section on the Delivery Manager page;
   interviews behave as before.
2. A **non-AI** channel (human panel) — no question sets section; scheduling and interviews
   unaffected.
3. A **Preset** AI channel that has **no** sets and the switch **Off** — serves its questions in the
   standard order, and coding questions marked in the question pool still open the IDE normally.

Item 3 matters: marking a question as a coding question must keep working on channels that have not
adopted sets.

---

## Part 12 — Editing the question pool after sets exist

Sets are snapshots: they record *which* questions belong to each paper. Editing the pool afterwards
can therefore contradict the papers already dealt, so removing a question is now guarded. These
cases need **no DBA** — everything is done from the Delivery Manager page.

The rule the guard follows: **once any interview on a channel is locked to a paper, that channel's
question pool is frozen.** Before that point the pool can still be edited, but the undealt papers
are thrown away because a paper that lost a question no longer covers the rubric.

### TC-28 — Editing a question's wording is picked up

1. On a channel with sets created and switched **on**, edit the **wording** of a question that
   belongs to a set. Do not delete it.
2. Launch an interview bound to that set and reach that question.

**Expected:** the candidate is asked the **updated** wording, and the paper still holds the same two
questions, one per rubric parameter. Only deletion is guarded; editing is always allowed.

### TC-29 — Removing a question is refused once a paper is in use

1. Use a channel where sets are created, switched **on**, and **bound to at least one booked
   interview** (that is, after Part 6).
2. Note which two questions one bound interview's set holds.
3. In the **Sample Questions** section, try to **delete** one of those two questions.

**Expected:** the delete is **refused** with a **"Not removed"** dialog reading:

> This question has been dealt into a question set, and one or more interviews on this channel are
> already locked to their sets. Removing it would leave an interview a question short and missing a
> rubric parameter.

4. Confirm the question is **still listed**, the sets panel still shows the same set contents, and
   the assignment counts are unchanged.
5. Launch or resume the bound interview and confirm it still asks **both** questions.

### TC-30 — The refusal covers the whole channel, not just the paper in use

1. On the same channel, pick a question belonging to a set that is **not** bound to any interview
   (assignment count 0) while another set on that channel **is** bound.
2. Try to delete it.

**Expected:** refused with the same message. This is deliberate — papers are dealt and re-dealt as a
family, so a channel with any paper in use has its whole pool frozen. If this delete were allowed it
would have to discard the bound paper too.

### TC-31 — Removing a question before any paper is used discards the sets

1. This case needs a channel where **no interview is bound** (every set shows "assigned to 0
   interview(s)"). The main test channel no longer qualifies once Part 6 has run, so build a second
   channel for this: repeat TC-05 to create the rubric and questions, then TC-07 to create sets, and
   switch it **On** as in TC-10. Do not book anything on it.
2. Delete a question that belongs to one of those sets.

**Expected:**
- The delete **succeeds**, with a **"Question sets discarded"** warning reading *"Question removed.
  The 3 question set(s) on this channel were discarded because they no longer cover the rubric.
  Create sets again to re-deal the remaining questions."*
- The sets panel now reads **"No sets have been created for this channel yet."**
- The channel's **other questions are all still listed** — only the papers were discarded, not the
  pool.
- Sets on **any other channel** are untouched.

3. If the channel's switch was **On**, it stays On, which leaves it switched on with no sets. Book
   and launch an interview now.

   **Expected:** the fail-closed behaviour of TC-26 — the interview refuses to serve questions
   rather than falling back to the shared order. This is the intended safety net, reachable here
   without any SQL.

4. Click **Create sets** to recover.

   **Expected:** sets are created again from the reduced pool, and the channel works normally.
   Remember the set count now follows the thinnest rubric parameter, so removing a question may
   reduce it (TC-09).

### TC-32 — Removing a question that was never dealt into a set

1. Add a brand new question to a channel that has sets, without re-creating the sets, so the new
   question belongs to no paper.
2. Delete it.

**Expected:** removed normally with a plain confirmation. **No** sets are discarded and the sets
panel is unchanged. A channel that uses sets must still allow ordinary pool housekeeping.

---

## Appendix A — SQL verification queries

Replace `@ChannelId` with the channel's **numeric** id and `@ChannelCode` with its **code**.

`InterviewMeetings.IRFId` holds the channel **code** (a string), not the numeric id. That is not a
bug — it is this database's long-standing convention.

### A.1 — Is the channel switched on?

```sql
SELECT id, IRFId, IsAIInterview, AIInterviewType,
       ISNULL(UseAIQuestionSets, 0) AS UseAIQuestionSets
FROM IRF
WHERE id = @ChannelId;
```

Expect `IsAIInterview = 1`, `AIInterviewType = 2` (Preset), and `UseAIQuestionSets` matching the
switch on screen.

### A.2 — What is in each set?

```sql
SELECT s.SetName, i.id AS ItemId, q.SkillName, q.Comment AS Question,
       ISNULL(q.IsCodingQuestion, 0) AS IsCodingQuestion
FROM AIQuestionSets s
JOIN AIQuestionSetItems i ON i.SetId = s.id
JOIN SkillChannelSampleQuestions q ON q.id = i.QuestionId
WHERE s.IRFId = @ChannelId
ORDER BY s.SetName, i.id;   -- item id order IS ask order; do not re-sort
```

### A.3 — Do the balance rules hold?

```sql
-- Any set holding two questions for the same parameter, or missing one: expect 0 rows.
SELECT s.SetName, i.SkillName, COUNT(*) AS Cnt
FROM AIQuestionSets s
JOIN AIQuestionSetItems i ON i.SetId = s.id
WHERE s.IRFId = @ChannelId
GROUP BY s.SetName, i.SkillName
HAVING COUNT(*) <> 1;

-- Any question used in more than one set: expect 0 rows.
SELECT i.QuestionId, COUNT(*) AS TimesUsed
FROM AIQuestionSets s
JOIN AIQuestionSetItems i ON i.SetId = s.id
WHERE s.IRFId = @ChannelId
GROUP BY i.QuestionId
HAVING COUNT(*) > 1;

-- Coding questions per set. The highest and lowest counts must differ by at most 1 (TC-07).
SELECT s.SetName, SUM(CASE WHEN ISNULL(q.IsCodingQuestion, 0) = 1 THEN 1 ELSE 0 END) AS CodingQuestions
FROM AIQuestionSets s
JOIN AIQuestionSetItems i ON i.SetId = s.id
JOIN SkillChannelSampleQuestions q ON q.id = i.QuestionId
WHERE s.IRFId = @ChannelId
GROUP BY s.SetName
ORDER BY s.SetName;
```

### A.4 — Which interview is on which paper?

```sql
SELECT im.id AS InterviewId, im.SessionId, s.SetName, ias.AssignedOn
FROM InterviewMeetings im
LEFT JOIN InterviewAIQuestionSet ias ON ias.InterviewId = im.id
LEFT JOIN AIQuestionSets s ON s.id = ias.SetId
WHERE im.IRFId = @ChannelCode
ORDER BY im.id;
```

`SessionId` is what goes in the `/AIInterview/Index?sid=` URL. A `NULL` set name means the interview
is not bound and will use the standard question order.

### A.5 — Setup for TC-26 only (destructive, test channel only)

```sql
-- Leaves the channel switched ON with no sets, which the UI will not allow.
DELETE FROM AIQuestionSetItems
WHERE SetId IN (SELECT id FROM AIQuestionSets WHERE IRFId = @ChannelId);

DELETE FROM InterviewAIQuestionSet
WHERE SetId IN (SELECT id FROM AIQuestionSets WHERE IRFId = @ChannelId);

DELETE FROM AIQuestionSets WHERE IRFId = @ChannelId;
```

### A.6 — Is any paper missing a question? (should always return 0 rows)

```sql
-- A set item whose question has been deleted. The runtime inner-joins the question table, so a row
-- here means a paper silently serves fewer questions than the rubric has parameters.
SELECT s.IRFId, s.SetName, i.QuestionId, i.SkillName
FROM AIQuestionSetItems i
JOIN AIQuestionSets s ON s.id = i.SetId
WHERE NOT EXISTS (SELECT 1 FROM SkillChannelSampleQuestions q WHERE q.id = i.QuestionId);
```

Worth running once at the end of Part 12. Any row is a defect, whichever case produced it.

---

## Appendix B — Exact messages you should see

Report any wording that differs, and any message that appears at the wrong time.

| Situation | Message |
|---|---|
| Config key empty | Question set creation is not configured yet: OpenAI:SetCreationPromptId is empty. Add the stored prompt id to configuration and try again. |
| No sets yet, panel opened | No sets have been created for this channel yet. |
| Switching on with no sets | Create the question sets first. A channel switched on with no sets cannot serve any questions. |
| Sets created | Created 3 question set(s). |
| Sets created but channel still off | These sets are not in use yet. Switch this channel on to start dealing them. |
| A rubric parameter has no questions | Rubric parameter "Testing" has no questions, so no set can cover the full rubric. Add a question for it and try again. |
| Re-creating sets already in use | Question sets for this channel are already in use by one or more interviews and cannot be re-created. Nothing was saved. |
| Switched on | This channel now deals questions from balanced sets. Each new interview is locked to one set. |
| Switched off | This channel no longer deals questions from sets. Interviews already bound to a set keep it. |
| Removing a question when a paper on that channel is in use | This question has been dealt into a question set, and one or more interviews on this channel are already locked to their sets. Removing it would leave an interview a question short and missing a rubric parameter. |
| Removing a dealt question when no paper is in use | Question removed. The 3 question set(s) on this channel were discarded because they no longer cover the rubric. Create sets again to re-deal the remaining questions. |
| Removing a question that was never dealt into a set | Question removed. |
| Interview finished all questions in its set | Interview Complete — The interviewer is through with all the questions. This interview is now closed. |
| AI interview type not chosen | Please select an AI interview type. |
| Client/candidate calling a sets endpoint | Forbidden |

---

## Appendix C — What to include in a defect report

For anything in Parts 6, 7, 9 or 10, include all of these or the developer cannot trace it:

- Channel **numeric id** and **code**
- **Interview id** and **SessionId**
- **Set name** the interview should have been on, from Appendix A.4
- The **questions actually asked**, in order (from the transcript)
- Screenshot of the sets panel showing set contents and assignment counts
- Timestamp, so the application log can be matched
- Whether the channel's switch was On or Off at the time

### Severity guidance

Treat these as **critical**:

- Two candidates on a switched-on channel asked the **same** questions
- A candidate's questions **changing mid-interview** (TC-23)
- A switched-on channel with no sets **serving the channel pool** instead of refusing (TC-26)
- A **client or candidate** seeing set contents, set names, scores, or other candidates' questions
  (Part 8)

Treat these as **high**:

- Wrong number of sets created for a given pool
- A set missing a rubric parameter, or reusing a question
- An interview not resuming on the same paper after reconnect
- A coding question served from a set failing to open the IDE
- An interview asking fewer questions than its set holds, or skipping a rubric parameter, after a
  question was deleted from the pool (TC-29 to TC-32). A question behind a paper in use should not
  be deletable at all; if one is, treat it as **critical**.
- A set item left pointing at a deleted question, so the paper silently shrinks

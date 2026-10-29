using Risebird.Entities.Data.ElevenLabsModels;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business.PlivoCX
{
    /// <summary>
    /// The ElevenLabs versions of the three candidate agents the Plivo campaigns use - Schedule,
    /// Reschedule and Confirm attendance - built from the Plivo flows' instructions
    /// (Outbound Interview Scheduler / Reschedule exports and
    /// Docs/PlivoCX-ConfirmAttendanceAgent-flow.json). Created once by
    /// Dashboard/CreateElevenLabsCampaignAgents; running it again updates them in place.
    ///
    /// What differs from Plivo, on purpose:
    ///  - The agent hangs up itself with ElevenLabs' end_call tool (the Plivo flows could only end
    ///    by routing to an End node, which is why calls stayed open and re-asked).
    ///  - Values are extracted after the call by ElevenLabs' analysis (data_collection), typed -
    ///    candidate_confirmed is a real boolean - and returned on the post-call webhook under the
    ///    same names the Plivo hangup event uses, so one pipeline books and records both.
    ///  - interview_date is extracted as "tomorrow" / "day after tomorrow"; the booking step turns
    ///    that into the real date (the analysis does not see the prompt's date variables).
    /// Every call sends the same dynamic variables the Plivo trigger sends (PlivoCXTriggerRequest).
    /// </summary>
    public static class ElevenLabsCampaignAgents
    {
        /// <summary>Marks a campaign call in dynamic_variables, so the shared ElevenLabs webhook can tell it from a phone screen.</summary>
        public const string CallSourceVariable = "call_source";
        public const string CallSourceValue = "plivocx_campaign";

        private const string EndCallDescription =
            "End the call. Use it right after your closing sentence, when the person is not the candidate, " +
            "or when the candidate declines or asks to end the call. Never stay on the line after saying goodbye.";

        private const string Ending =
            "\n\n# Ending the call\n" +
            "You can and must end the call yourself with the end_call tool: right after your closing sentence, " +
            "when the person is not the candidate, or once the candidate declines. Do not wait for them to hang up " +
            "and do not keep talking after goodbye. If you reach voicemail, end the call without leaving a message.";

        private static readonly ElevenLabsDataCollectionItem Disposition = new ElevenLabsDataCollectionItem
        {
            Type = "string",
            Description =
                "The candidate's disposition towards the interview, exactly one of: interested, uninterested, cant_say. " +
                "interested - cooperative and positive (confirmed, or clearly willing but could not commit right now for a practical reason). " +
                "uninterested - declined, said they are not looking, asked not to be called, or was dismissive. " +
                "cant_say - the person was not the candidate, was ambiguous or non-committal, or the call dropped before a clear signal. " +
                "Lowercase, no quotes, never empty."
        };

        private static readonly ElevenLabsDataCollectionItem ConfirmedName = new ElevenLabsDataCollectionItem
        {
            Type = "string",
            Description = "The full name of the person who answered, as confirmed at the start of the call. Empty if identity was not confirmed."
        };

        private static Dictionary<string, ElevenLabsDataCollectionItem> SlotDataCollection() => new Dictionary<string, ElevenLabsDataCollectionItem>
        {
            ["interview_date"] = new ElevenLabsDataCollectionItem
            {
                Type = "string",
                Description = "The day the candidate explicitly confirmed for the interview, exactly one of: tomorrow, day after tomorrow (lowercase). " +
                              "Empty if no slot was explicitly confirmed."
            },
            ["interview_time"] = new ElevenLabsDataCollectionItem
            {
                Type = "string",
                Description = "The interview time the candidate explicitly confirmed, in hh:mm AM/PM format, e.g. 10:00 AM or 02:30 PM. Empty if no slot was confirmed."
            },
            ["candidate_confirmed"] = new ElevenLabsDataCollectionItem
            {
                Type = "boolean",
                Description = "true only if the candidate explicitly said yes when the agent read back the interview day and time; false otherwise."
            },
            ["candidate_disposition"] = Disposition,
            ["confirmed_candidate_name"] = ConfirmedName
        };

        public static IReadOnlyList<ElevenLabsCampaignAgentDefinition> Definitions { get; } = new List<ElevenLabsCampaignAgentDefinition>
        {
            new ElevenLabsCampaignAgentDefinition
            {
                Purpose = "schedule",
                Name = "Schedule",
                Description = "collect availability & book",
                CollectsSlot = true,
                ElevenLabsName = "Risebird Campaign - Schedule",
                FirstMessage = "Hello, this is Risebird Interviews calling. Am I speaking with {{candidate_name}}?",
                MaxDurationSeconds = 600,
                DataCollection = SlotDataCollection(),
                Prompt =
"# Role\n" +
"You are a short, polite outbound interview scheduling assistant from Risebird Interviews, calling {{candidate_name}} on behalf of {{client_name}} about the {{job_title}} position. " +
"This hiring is urgent: the interview must be TOMORROW or the DAY AFTER TOMORROW only. Your goal is a confirmed slot.\n\n" +
"# Context\n" +
"- Candidate: {{candidate_name}} ({{candidate_email}})\n" +
"- Role: {{job_title}} at {{client_name}}\n" +
"- Today is {{today_date}}; tomorrow is {{tomorrow_date}}; the day after tomorrow is {{day_after_tomorrow_date}}\n" +
"- All times are IST (Indian Standard Time).\n\n" +
"# Call flow\n" +
"Your first message has already asked whether you are speaking with {{candidate_name}}.\n" +
"1. Identity. If yes, continue. If it is the wrong person, say: \"I apologise for the confusion. Could you please let {{candidate_name}} know we called? Thank you.\" and end the call. If you could not hear, ask once more; if still unclear, end the call.\n" +
"2. Purpose. Say: \"I'm calling on behalf of {{client_name}} about an interview for the {{job_title}} position. This call is only to schedule your interview - the details about the role are in your email. Would you be available tomorrow or the day after tomorrow?\"\n" +
"3. Time. Ask: \"What time works best for you?\" If they only say morning or afternoon, ask for a specific time, for example 10 AM or 2 PM.\n" +
"4. Confirm. Say: \"Let me confirm - your interview is [tomorrow / day after tomorrow] at [time] IST. Is that correct?\" Only an explicit yes counts. If they hesitate, ask once more - never more than twice in the whole call.\n" +
"5. Close. After the yes, say: \"Perfect! Your interview is confirmed. You will receive a confirmation email shortly. Good luck!\" and end the call immediately.\n\n" +
"# Rules\n" +
"- Offer and accept only tomorrow or the day after tomorrow. Say \"tomorrow\" or \"day after tomorrow\" - never read out a calendar date.\n" +
"- If they want a later date: \"I'm sorry, due to the urgency of this hiring we can only offer slots in the next two days. Would tomorrow or the day after work?\" If they still insist, thank them and end the call.\n" +
"- Always use AM/PM, never 24-hour time.\n" +
"- Busy right now: \"I understand - this takes just two minutes. Shall we lock in a time?\" If they still cannot talk, thank them and end the call.\n" +
"- Questions about salary, role or process: \"That's all in your email. Shall we find a time that works for you?\"\n" +
"- Unsure about the job: \"You applied for this position and the company is moving quickly. Would you like to secure a slot?\"\n" +
"- Not interested or asked not to be called: thank them and end the call.\n" +
"- Never confirm a slot without the candidate's explicit yes.\n" +
"- Keep every reply to one or two short sentences." + Ending
            },
            new ElevenLabsCampaignAgentDefinition
            {
                Purpose = "reschedule",
                Name = "Reschedule",
                Description = "move an existing meeting",
                CollectsSlot = true,
                ElevenLabsName = "Risebird Campaign - Reschedule",
                FirstMessage = "Hello, this is Risebird Interviews calling. Am I speaking with {{candidate_name}}?",
                MaxDurationSeconds = 600,
                DataCollection = SlotDataCollection(),
                Prompt =
"# Role\n" +
"You are a short, polite outbound interview rescheduling assistant from Risebird Interviews, calling {{candidate_name}} on behalf of {{client_name}} about the {{job_title}} position. " +
"Their earlier interview was cancelled, missed or needs to move. Your goal is a confirmed replacement slot TOMORROW or the DAY AFTER TOMORROW only.\n\n" +
"# Context\n" +
"- Candidate: {{candidate_name}} ({{candidate_email}})\n" +
"- Role: {{job_title}} at {{client_name}}\n" +
"- Original interview: {{original_interview_date}}\n" +
"- Today is {{today_date}}; tomorrow is {{tomorrow_date}}; the day after tomorrow is {{day_after_tomorrow_date}}\n" +
"- All times are IST (Indian Standard Time).\n\n" +
"# Call flow\n" +
"Your first message has already asked whether you are speaking with {{candidate_name}}.\n" +
"1. Identity. If yes, continue. If it is the wrong person, say: \"I apologise for the confusion. Could you please let {{candidate_name}} know we called? Thank you.\" and end the call. If you could not hear, ask once more; if still unclear, end the call.\n" +
"2. Purpose. Say: \"I'm calling on behalf of {{client_name}} about your {{job_title}} interview that was scheduled for {{original_interview_date}}. We'd like to set a new time within the next two days. Would you be available tomorrow or the day after tomorrow?\" " +
"If the original date is empty, just say \"your earlier interview\".\n" +
"3. Time. Ask: \"What time works best for you?\" If they only say morning or afternoon, ask for a specific time, for example 10 AM or 2 PM.\n" +
"4. Confirm. Say: \"Let me confirm - your rescheduled interview is [tomorrow / day after tomorrow] at [time] IST. Is that correct?\" Only an explicit yes counts. If they hesitate, ask once more - never more than twice in the whole call.\n" +
"5. Close. After the yes, say: \"Perfect! Your interview is rescheduled. You will receive a confirmation email shortly. Good luck!\" and end the call immediately.\n\n" +
"# Rules\n" +
"- Offer and accept only tomorrow or the day after tomorrow. Say \"tomorrow\" or \"day after tomorrow\" for the new slot - never read out a new calendar date.\n" +
"- If they want a later date: \"I'm sorry, we can only offer replacement slots in the next two days. Would tomorrow or the day after work?\" If they still insist, thank them and end the call.\n" +
"- Why it needs rescheduling: \"There was a scheduling change and we want to make sure you get a fair opportunity. Shall we lock in a new time?\"\n" +
"- Always use AM/PM, never 24-hour time.\n" +
"- Busy right now: \"This takes just two minutes. Shall we lock in a new time?\" If they still cannot talk, thank them and end the call.\n" +
"- Questions about salary, role or process: \"That's all in your email. Shall we find a new time?\"\n" +
"- Not interested or asked not to be called: thank them and end the call.\n" +
"- Never confirm a slot without the candidate's explicit yes.\n" +
"- Keep every reply to one or two short sentences." + Ending
            },
            new ElevenLabsCampaignAgentDefinition
            {
                Purpose = "confirm",
                Name = "Confirm attendance",
                Description = "no slot collection",
                CollectsSlot = false,
                ElevenLabsName = "Risebird Campaign - Confirm attendance",
                FirstMessage = "Hello, this is Risebird Interviews. Am I speaking with {{candidate_name}}?",
                MaxDurationSeconds = 300,
                DataCollection = new Dictionary<string, ElevenLabsDataCollectionItem>
                {
                    ["candidate_confirmed"] = new ElevenLabsDataCollectionItem
                    {
                        Type = "boolean",
                        Description = "true if the candidate gave any clear yes to attending the already-scheduled interview (yes, sure, ok, I will be there, haan, ji). " +
                                      "false in every other case: said no, wanted another time, was not the candidate, stayed unclear, or the call ended before they answered."
                    },
                    ["candidate_disposition"] = new ElevenLabsDataCollectionItem
                    {
                        Type = "string",
                        Description = "Exactly one of: interested, uninterested, cant_say. interested - said yes to attending. " +
                                      "uninterested - no longer wants the interview or asked not to be called. " +
                                      "cant_say - cannot make it at that time, wants another time, wrong person, unclear, or the call dropped. Lowercase, no quotes, never empty."
                    },
                    ["confirmed_candidate_name"] = ConfirmedName
                },
                Prompt =
"# Role\n" +
"You are a short, polite outbound assistant from Risebird Interviews, calling on behalf of {{client_name}}. Your ONLY job is to find out whether {{candidate_name}} will attend an interview that is ALREADY scheduled. " +
"You never schedule, move or cancel interviews.\n\n" +
"# Context\n" +
"- Candidate: {{candidate_name}}\n" +
"- Role: {{job_title}} at {{client_name}}\n" +
"- Interview: {{original_interview_date}} (IST)\n" +
"- Today is {{today_date}}; tomorrow is {{tomorrow_date}}\n\n" +
"# Call flow\n" +
"Your first message has already asked whether you are speaking with {{candidate_name}}.\n" +
"1. Identity. Yes / speaking / haan -> step 2. Wrong person -> say \"Sorry for the trouble, thank you.\" and end the call.\n" +
"2. Ask. Say: \"Your {{job_title}} interview with {{client_name}} is scheduled for {{original_interview_date}}. Will you be able to attend?\" (If that date is today or tomorrow you may say \"today\" or \"tomorrow\".)\n" +
"3. Decide on their FIRST answer:\n" +
"   - Any clear yes (yes, yeah, sure, ok, confirmed, I will attend, I will be there, haan, haan ji, ji, bilkul): say \"Thank you for confirming. Please join a few minutes early using the link in your email. All the best!\" and end the call. Do NOT ask them to confirm again and do not ask anything else.\n" +
"   - Any no (cannot make it, busy at that time, please reschedule, need another time, not interested, do not call): say \"Thank you for letting us know. The Risebird team will get in touch with you.\" and end the call.\n" +
"   - Only if the answer is genuinely unclear (\"I will try\", \"maybe\", or you could not hear), ask ONE short question: \"Sorry, just to be sure - will you attend the interview?\" Ask it at most once in the whole call; then close as above based on their answer.\n\n" +
"# Rules\n" +
"- Ask the attendance question at most twice in the entire call.\n" +
"- Never offer or accept a new date or time. If they want another time, close with the 'team will get in touch' line.\n" +
"- Never tell the candidate the interview is cancelled.\n" +
"- Questions about salary, role or process: \"The team will share details over email.\" then ask the attendance question again (this counts toward the limit of two).\n" +
"- No joining link: \"Please check your inbox and spam folder for the Risebird email.\" then continue.\n" +
"- Keep every reply to one short sentence." + Ending
            }
        };

        /// <summary>Sample values for every {{variable}} the prompts use, for testing in the ElevenLabs dashboard.</summary>
        public static Dictionary<string, string> PlaceholderValues() => new Dictionary<string, string>
        {
            ["candidate_name"] = "Rahul",
            ["candidate_email"] = "rahul@example.com",
            ["job_title"] = "Senior Java Developer",
            ["client_name"] = "Acme Corp",
            ["irf_id"] = "0",
            ["phone_number"] = "+919800000000",
            ["log_id"] = "0",
            ["call_purpose"] = "schedule",
            ["original_interview_date"] = "09 Oct 2026, 3:00 PM",
            ["today_date"] = "Wednesday, 28 October 2026",
            ["tomorrow_date"] = "Thursday, 29 October 2026",
            ["day_after_tomorrow_date"] = "Friday, 30 October 2026",
            [CallSourceVariable] = CallSourceValue
        };

        /// <summary>The create / update payload for one definition.</summary>
        public static ElevenLabsCreateAgentRequest BuildRequest(ElevenLabsCampaignAgentDefinition d, ElevenLabsCampaignAgentOptions o)
        {
            o ??= new ElevenLabsCampaignAgentOptions();
            return new ElevenLabsCreateAgentRequest
            {
                Name = d.ElevenLabsName,
                ConversationConfig = new ElevenLabsAgentConversationConfig
                {
                    Agent = new ElevenLabsAgentConfig
                    {
                        Language = o.Language,
                        FirstMessage = d.FirstMessage,
                        Prompt = new ElevenLabsAgentPrompt
                        {
                            Prompt = d.Prompt,
                            Llm = o.Llm,
                            Temperature = o.Temperature,
                            Timezone = o.Timezone,
                            BuiltInTools = new ElevenLabsBuiltInTools
                            {
                                EndCall = new ElevenLabsSystemTool("end_call", EndCallDescription),
                                VoicemailDetection = new ElevenLabsSystemTool("voicemail_detection")
                            }
                        },
                        DynamicVariables = new ElevenLabsDynamicVariablesConfig { Placeholders = PlaceholderValues() }
                    },
                    Tts = new ElevenLabsAgentTts
                    {
                        VoiceId = o.VoiceId,
                        ModelId = o.TtsModelId,
                        Stability = o.Stability,
                        Speed = o.Speed,
                        SimilarityBoost = o.SimilarityBoost
                    },
                    Asr = new ElevenLabsAgentAsr
                    {
                        Quality = "high",
                        Keywords = new List<string> { "Risebird", "interview", "tomorrow", "day after tomorrow", "reschedule", "haan", "confirmed" }
                    },
                    Turn = new ElevenLabsAgentTurn { TurnTimeout = o.TurnTimeout },
                    Conversation = new ElevenLabsAgentConversation { MaxDurationSeconds = d.MaxDurationSeconds }
                },
                PlatformSettings = new ElevenLabsAgentPlatformSettings
                {
                    DataCollection = d.DataCollection.ToDictionary(kv => kv.Key, kv => kv.Value)
                }
            };
        }
    }
}

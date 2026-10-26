using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Helpers
{
    /// <summary>
    /// Gives a client report the matcher that puts each discussed question's candidate code under it. Loaded once per
    /// request and interview (both skill sections of a report share it). Any failure gives the disabled matcher, and the
    /// report then renders exactly as it did without code sessions.
    /// </summary>
    public static class CodeSessionReportHelper
    {
        private const string CacheKeyPrefix = "rb-code-session-matcher-";

        public static async Task<CodeSessionQuestionMatcher> GetMatcherAsync(HttpContext context, int interviewId,
            IEnumerable<QuestionsDiscussed> questions, IEnumerable<QuestionsDiscussed> otherSkillQuestions = null)
        {
            if (context == null || interviewId <= 0)
            {
                return CodeSessionQuestionMatcher.Disabled;
            }

            var key = CacheKeyPrefix + interviewId;
            if (context.Items.TryGetValue(key, out var cached) && cached is CodeSessionQuestionMatcher existing)
            {
                return existing;
            }

            CodeSessionQuestionMatcher matcher;
            try
            {
                var service = context.RequestServices.GetService<IInterviewAlignmentService>();
                var sessions = service == null ? null : await service.GetSessionsForReportAsync(interviewId);
                var discussed = (questions ?? Enumerable.Empty<QuestionsDiscussed>())
                    .Concat(otherSkillQuestions ?? Enumerable.Empty<QuestionsDiscussed>())
                    .Where(q => q != null)
                    .Select(q => q.Comment);
                matcher = new CodeSessionQuestionMatcher(sessions, discussed);
            }
            catch (Exception)
            {
                matcher = CodeSessionQuestionMatcher.Disabled;
            }

            context.Items[key] = matcher;
            return matcher;
        }
    }
}

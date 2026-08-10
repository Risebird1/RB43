using System.Collections.Generic;
using Risebird.Entities.ViewModel;

namespace Risebird.Entities.Search
{
    /// <summary>
    /// One page of interviewer roster hits from Azure AI Search (or SQL fallback).
    /// </summary>
    public sealed class InterviewerSearchPageResult
    {
        public IReadOnlyList<InterviewerProfileVM> Items { get; init; } =
            System.Array.Empty<InterviewerProfileVM>();

        public long TotalCount { get; init; }

        public int PageNumber { get; init; }

        public int PageSize { get; init; }
    }
}

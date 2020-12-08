using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business
{
    public class CandidateService : ICandidateService
    {
        private readonly ICandidateRepository _candidateRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public CandidateService(ICandidateRepository candidateRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _candidateRepository = candidateRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
    }
}

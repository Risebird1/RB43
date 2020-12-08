using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Repository
{
    public class CandidateRepository: ICandidateRepository
    {

        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public CandidateRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
    }
}

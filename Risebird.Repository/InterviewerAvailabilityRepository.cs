using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using System;
using Risebird.Repository.Interface;

namespace Risebird.Repository
{
    public class InterviewerAvailabilityRepository:IInterviewerAvailabilityRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public InterviewerAvailabilityRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<bool> Add(List<InterviewerAvailabilityVM> interviewerAvailabilities)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable interviewerAvailabilityDt = new DataTable();
                    interviewerAvailabilityDt.Columns.Add("InterviewerProfileId", typeof(int));
                    interviewerAvailabilityDt.Columns.Add("AvailabilityDate", typeof(DateTime));
                    interviewerAvailabilityDt.Columns.Add("Hour_00_TO_01", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_01_TO_02", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_02_TO_03", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_03_TO_04", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_04_TO_05", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_05_TO_06", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_06_TO_07", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_07_TO_08", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_08_TO_09", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_09_TO_10", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_10_TO_11", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_11_TO_12", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_12_TO_13", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_13_TO_14", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_14_TO_15", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_15_TO_16", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_16_TO_17", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_17_TO_18", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_18_TO_19", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_19_TO_20", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_20_TO_21", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_21_TO_22", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_22_TO_23", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("Hour_23_TO_00", typeof(Boolean));
                    interviewerAvailabilityDt.Columns.Add("CreatedOn", typeof(DateTime));
                    interviewerAvailabilityDt.Columns.Add("ModifiedOn", typeof(DateTime));
                    interviewerAvailabilityDt.Columns.Add("CreatedBy", typeof(string));
                    interviewerAvailabilityDt.Columns.Add("ModifiedBy", typeof(string));


                    foreach (var item in interviewerAvailabilities)
                    {
                        interviewerAvailabilityDt.Rows.Add(item.InterviewerProfileId,
                            item.AvailabilityDate, item.Hour_00_TO_01,
                            item.Hour_01_TO_02, item.Hour_02_TO_03, item.Hour_03_TO_04, item.Hour_04_TO_05,
                            item.Hour_05_TO_06, item.Hour_06_TO_07, item.Hour_07_TO_08, item.Hour_08_TO_09,
                            item.Hour_09_TO_10, item.Hour_10_TO_11, item.Hour_11_TO_12, item.Hour_12_TO_13,
                            item.Hour_13_TO_14, item.Hour_14_TO_15, item.Hour_15_TO_16, item.Hour_16_TO_17,
                            item.Hour_17_TO_18, item.Hour_18_TO_19, item.Hour_19_TO_20, item.Hour_20_TO_21,
                            item.Hour_21_TO_22, item.Hour_22_TO_23, item.Hour_23_TO_00,item.CreatedOn,item.ModifiedOn,item.CreatedBy,item.ModifiedBy
                            );
                    }

                    SqlCommand cmdJoinability = new SqlCommand("usp_InterviewerAvailabilityBulkInsert", con);
                    cmdJoinability.CommandType = CommandType.StoredProcedure;
                    cmdJoinability.Parameters.Add(new SqlParameter("@@InterviewerAvailabilityBulkData", SqlDbType.Structured));
                    cmdJoinability.Parameters["@@InterviewerAvailabilityBulkData"].Value = interviewerAvailabilityDt;
                    con.Open();
                    if (await cmdJoinability.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }
                    return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

    }
}

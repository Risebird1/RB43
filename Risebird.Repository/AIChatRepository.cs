using Microsoft.Extensions.Configuration;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;

namespace Risebird.Repository
{
    public class AIChatRepository : IAIChatRepository
    {
        private readonly string _connectionString;

        public AIChatRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<bool> SaveChatMessageAsync(string userId, string userMessage, string aiResponse, DateTime timestamp)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Save user message
                    var userParams = new DynamicParameters();
                    userParams.Add("@UserId", userId);
                    userParams.Add("@Message", userMessage);
                    userParams.Add("@Role", "user");
                    userParams.Add("@Timestamp", timestamp);
                    
                    await connection.ExecuteAsync(
                        "INSERT INTO ChatHistory (UserId, Message, Role, Timestamp) VALUES (@UserId, @Message, @Role, @Timestamp)",
                        userParams
                    );

                    // Save AI response
                    var aiParams = new DynamicParameters();
                    aiParams.Add("@UserId", userId);
                    aiParams.Add("@Message", aiResponse);
                    aiParams.Add("@Role", "assistant");
                    aiParams.Add("@Timestamp", timestamp);
                    
                    await connection.ExecuteAsync(
                        "INSERT INTO ChatHistory (UserId, Message, Role, Timestamp) VALUES (@UserId, @Message, @Role, @Timestamp)",
                        aiParams
                    );

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<(string Message, string Role, DateTime Timestamp)>> GetChatHistoryAsync(string userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var results = await connection.QueryAsync<(string Message, string Role, DateTime Timestamp)>(
                    "SELECT Message, Role, Timestamp FROM ChatHistory WHERE UserId = @UserId ORDER BY Timestamp",
                    new { UserId = userId }
                );

                return results.AsList();
            }
        }
    }
} 
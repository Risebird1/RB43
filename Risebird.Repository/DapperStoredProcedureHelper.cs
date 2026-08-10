using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    /// <summary>
    /// Centralizes stored-procedure access so repositories stop embedding inline SQL.
    /// </summary>
    internal static class DapperStoredProcedureHelper
    {
        public static IEnumerable<T> Query<T>(
            SqlConnection connection,
            string procedureName,
            object parameters = null,
            int? commandTimeout = null)
        {
            return SqlMapper.Query<T>(
                connection,
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout);
        }

        public static async Task<IEnumerable<T>> QueryAsync<T>(
            SqlConnection connection,
            string procedureName,
            object parameters = null,
            int? commandTimeout = null)
        {
            return await SqlMapper.QueryAsync<T>(
                connection,
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout);
        }

        public static T QueryFirstOrDefault<T>(
            SqlConnection connection,
            string procedureName,
            object parameters = null,
            int? commandTimeout = null)
        {
            return Query<T>(connection, procedureName, parameters, commandTimeout).FirstOrDefault();
        }

        public static async Task<T> QueryFirstOrDefaultAsync<T>(
            SqlConnection connection,
            string procedureName,
            object parameters = null,
            int? commandTimeout = null)
        {
            var rows = await QueryAsync<T>(connection, procedureName, parameters, commandTimeout);
            return rows.FirstOrDefault();
        }

        public static int Execute(
            SqlConnection connection,
            string procedureName,
            object parameters = null,
            int? commandTimeout = null)
        {
            return SqlMapper.Execute(
                connection,
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout);
        }

        public static async Task<int> ExecuteAsync(
            SqlConnection connection,
            string procedureName,
            object parameters = null,
            int? commandTimeout = null)
        {
            return await SqlMapper.ExecuteAsync(
                connection,
                procedureName,
                parameters,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout);
        }
    }
}

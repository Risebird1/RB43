using System;
using Azure;
using Azure.Data.Tables;
using Risebird.Entities.Data;
using Risebird.Entities.Storage;
using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.Storage
{
    /// <summary>
    /// These entities are the rows the portal writes to and reads from Azure Table Storage
    /// (Risebirdlogs, RisebirdSurvey, SpeechToTextTbl, the daily-count table). The migration off
    /// the retired WindowsAzure.Storage SDK reparented them from that SDK's <c>TableEntity</c> onto
    /// <see cref="RbTableEntity"/>, which implements the current <see cref="ITableEntity"/> contract.
    /// If any of these stopped being an <see cref="ITableEntity"/>, or lost its PartitionKey/RowKey
    /// plumbing, the Azure.Data.Tables client would refuse the type at compile time in the helper's
    /// generic constraint — or silently persist rows with empty keys. These tests pin the contract.
    /// </summary>
    public sealed class RbTableEntityTests
    {
        [Fact]
        public void RbTableEntity_satisfies_the_ITableEntity_contract_used_by_the_helper_generics()
        {
            // The helper's table methods are all `where T : class, ITableEntity, new()`.
            Assert.True(typeof(ITableEntity).IsAssignableFrom(typeof(RbTableEntity)));
        }

        [Theory]
        [InlineData(typeof(LogData))]
        [InlineData(typeof(SurveyVM))]
        [InlineData(typeof(LogLevelStampEntity))]
        [InlineData(typeof(SpeechToTextServiceModel))]
        [InlineData(typeof(ApplicationLogDailyCountEntity))]
        public void Every_migrated_entity_is_an_ITableEntity_with_a_parameterless_constructor(Type entityType)
        {
            // ITableEntity: required by the client for (de)serialization.
            Assert.True(typeof(ITableEntity).IsAssignableFrom(entityType),
                $"{entityType.Name} must implement ITableEntity to be usable with Azure.Data.Tables.");
            // new(): required by the `T : new()` constraint the query methods carry.
            Assert.NotNull(Activator.CreateInstance(entityType));
        }

        [Fact]
        public void The_four_system_columns_round_trip_through_the_base_class()
        {
            var when = DateTimeOffset.UtcNow;
            var etag = new ETag("W/\"datetime'2026-01-01'\"");

            var entity = new LogData
            {
                PartitionKey = "pk-1",
                RowKey = "rk-1",
                Timestamp = when,
                ETag = etag,
            };

            Assert.Equal("pk-1", entity.PartitionKey);
            Assert.Equal("rk-1", entity.RowKey);
            Assert.Equal(when, entity.Timestamp);
            Assert.Equal(etag, entity.ETag);
        }

        [Fact]
        public void LogData_key_constructor_sets_partition_and_row_keys()
        {
            // ApplicationErrorLogger builds every log row via this ctor; empty keys = an unwritable row.
            var row = new LogData("reverse-ticks", "guid-row")
            {
                LogeLevel = 3,
                LogMessage = "boom",
            };

            Assert.Equal("reverse-ticks", row.PartitionKey);
            Assert.Equal("guid-row", row.RowKey);
            Assert.Equal(3, row.LogeLevel);
            Assert.Equal("boom", row.LogMessage);
        }

        [Fact]
        public void LogData_has_a_parameterless_constructor_for_query_materialization()
        {
            // Azure.Data.Tables materializes query results via new() + property setters.
            var row = new LogData();
            Assert.Null(row.PartitionKey);
            Assert.Null(row.RowKey);
        }

        [Fact]
        public void ApplicationLogDailyCount_constructor_encodes_the_utc_date_as_the_partition_key()
        {
            var date = new DateTime(2026, 3, 9, 13, 45, 0, DateTimeKind.Utc);

            var entity = new ApplicationLogDailyCountEntity(date);

            Assert.Equal("2026-03-09", entity.PartitionKey);
            Assert.Equal(ApplicationLogDailyCountEntity.CountsRowKey, entity.RowKey);
        }

        [Fact]
        public void ApplicationLogDailyCount_partition_key_round_trips_back_to_its_utc_date()
        {
            var date = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            var entity = new ApplicationLogDailyCountEntity(date);

            Assert.Equal(date.Date, entity.GetUtcDate().Date);
        }
    }
}

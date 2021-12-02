using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using ResearchXBRL.Domain.FinancialAnalysis.TimeSeriesAnalysis;
using ResearchXBRL.Domain.FinancialAnalysis.TimeSeriesAnalysis.AccountPeriods;
using ResearchXBRL.Domain.FinancialAnalysis.TimeSeriesAnalysis.Corporations;
using ResearchXBRL.Domain.FinancialAnalysis.TimeSeriesAnalysis.Units;

namespace ResearchXBRL.Infrastructure.FinancialAnalysis.TimeSeriesAnalysis
{
    public sealed class TimeSeriesAnalysisResultRepository : ITimeSeriesAnalysisResultRepository, IDisposable, IAsyncDisposable
    {
        private readonly NpgsqlConnection connection;
        private readonly ICorporationRepository corporationRepository;

        public TimeSeriesAnalysisResultRepository(ICorporationRepository corporationRepository)
        {
            this.corporationRepository = corporationRepository;
            var server = Environment.GetEnvironmentVariable("DB_SERVERNAME");
            var userId = Environment.GetEnvironmentVariable("DB_USERID");
            var dbName = Environment.GetEnvironmentVariable("DB_NAME");
            var port = Environment.GetEnvironmentVariable("DB_PORT");
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
            var connectionString = $"Server={server};Port={port};Database={dbName};User Id={userId};Password={password};";
            connection = new NpgsqlConnection(connectionString);
            connection.Open();
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await connection.DisposeAsync();
        }

        public async Task<TimeSeriesAnalysisResult> GetConsolidateResult(string corporationId, string accountItemName)
        {
            var (unit, accountValues) = await ReadUnitAndConsolidateAccountValues(connection, corporationId, accountItemName);
            return new TimeSeriesAnalysisResult
            {
                AccountName = accountItemName,
                Unit = unit,
                Values = accountValues,
                Corporation = await corporationRepository.Get(corporationId)
                    ?? throw new ArgumentException("指定された企業は存在しません")
            };
        }

        private static IUnit GetUnit(NpgsqlDataReader reader)
        {
            var measureColumn = reader.GetOrdinal("measure");
            var unitNameColumn = reader.GetOrdinal("unit_name");
            var measure = reader[measureColumn];
            if (measure is DBNull)
            {
                var unitNumeratorColumn = reader.GetOrdinal("unit_numerator");
                var unitDenominatorColumn = reader.GetOrdinal("unit_denominator");
                return new DividedUnit
                {
                    Name = $"{reader[unitNameColumn]}",
                    UnitNumerator = $"{reader[unitNumeratorColumn]}",
                    UnitDenominator = $"{reader[unitDenominatorColumn]}"
                };
            }
            return new NormalUnit
            {
                Name = $"{reader[unitNameColumn]}",
                Measure = $"{reader[measureColumn]}"
            };
        }
        private static async Task<(IUnit, IReadOnlyList<AccountValue>)> ReadUnitAndConsolidateAccountValues(NpgsqlConnection connection, string corporationId, string accountItemName)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
SELECT
    A.amount,
    A.unit_name,
    E.measure,
    E.unit_numerator,
    E.unit_denominator,
    C.context_name,
    C.period_from,
    C.period_to,
    C.instant_date
FROM
    report_items A
INNER JOIN
    account_element B
ON
    A.xbrl_name = B.xbrl_name
INNER JOIN
    contexts C
ON
    A.report_id = C.report_id
INNER JOIN
    company_master D
INNER JOIN
    units E
ON
    A.unit_name = E.unit_name
WHERE
    B.account_name = @accountName
AND
    C.context_name LIKE 'CurrentYearInstant%'
AND
    C.context_name NOT LIKE '%_NonConsolidateMember' -- 非連結は除外
AND
    D.code = @corporationId
ORDER BY
    period_from, instant_date: 
";
            command.Parameters.Add("@accountName", NpgsqlDbType.Varchar)
                .Value = $"%{accountItemName}%";
            command.Parameters.Add("@corporationId", NpgsqlDbType.Varchar)
                .Value = $"%{corporationId}%";
            return await GetAccountValues(await command.ExecuteReaderAsync());
        }
        private static async Task<(IUnit, IReadOnlyList<AccountValue>)> GetAccountValues(NpgsqlDataReader reader)
        {
            var values = new List<AccountValue>();
            var amountColumn = reader.GetOrdinal("amount");
            var instantDateColumn = reader.GetOrdinal("instant_date");
            var fromDateColumn = reader.GetOrdinal("period_from");
            var toDateColumn = reader.GetOrdinal("period_to");
            IUnit? unit = null;
            while (await reader.ReadAsync())
            {
                unit ??= GetUnit(reader);
                values.Add(new AccountValue
                {
                    FinalAccountsPeriod = GetAccountsPeriod(reader, instantDateColumn, fromDateColumn, toDateColumn),
                    Amount = decimal.Parse($"{reader[amountColumn]}")
                });
            }
            return (unit ?? throw new Exception("単位を特定できませんでした"), values);
        }

        public async Task<TimeSeriesAnalysisResult> GetNonConsolidateResult(string corporationId, string accountItemName)
        {
            var (unit, accountValues) = await ReadUnitAndNonConsolidateAccountValues(connection, corporationId, accountItemName);
            return new TimeSeriesAnalysisResult
            {
                AccountName = accountItemName,
                Unit = unit,
                Values = accountValues,
                Corporation = await corporationRepository.Get(corporationId)
                    ?? throw new ArgumentException("指定された企業は存在しません")
            };
        }

        private static async Task<(IUnit, IReadOnlyList<AccountValue>)> ReadUnitAndNonConsolidateAccountValues(NpgsqlConnection connection, string corporationId, string accountItemName)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
SELECT
    A.amount,
    A.unit_name,
    E.measure,
    E.unit_numerator,
    E.unit_denominator,
    C.context_name,
    C.period_from,
    C.period_to,
    C.instant_date
FROM
    report_items A
INNER JOIN
    account_element B
ON
    A.xbrl_name = B.xbrl_name
INNER JOIN
    contexts C
ON
    A.report_id = C.report_id
INNER JOIN
    company_master D
INNER JOIN
    units E
ON
    A.unit_name = E.unit_name
WHERE
    B.account_name = @accountName
AND
    C.context_name LIKE 'CurrentYearInstant%'
AND
    C.context_name LIKE '%_NonConsolidateMember' -- 非連結のみ
AND
    D.code = @corporationId
ORDER BY
    period_from, instant_date: 
";
            command.Parameters.Add("@accountName", NpgsqlDbType.Varchar)
                .Value = $"%{accountItemName}%";
            command.Parameters.Add("@corporationId", NpgsqlDbType.Varchar)
                .Value = $"%{corporationId}%";
            return await GetAccountValues(await command.ExecuteReaderAsync());
        }
        private static IAccountsPeriod GetAccountsPeriod(NpgsqlDataReader reader, int instantDateColumn, int fromDateColumn, int toDateColumn)
        {
            if (reader[instantDateColumn] is DBNull)
            {
                return new DurationPeriod
                {
                    From = DateTime.Parse($"{reader[fromDateColumn]}"),
                    To = DateTime.Parse($"{reader[toDateColumn]}")
                };
            }
            return new InstantPeriod
            {
                Instant = DateTime.Parse($"{reader[instantDateColumn]}")
            };
        }
    }
}
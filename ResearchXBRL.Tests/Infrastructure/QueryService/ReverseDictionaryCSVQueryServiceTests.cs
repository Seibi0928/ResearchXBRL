using System;
using Xunit;
using ResearchXBRL.Infrastructure.Shared.FileStorages;
using ResearchXBRL.Infrastructure.QueryServices.ReverseLookupAccountItems;
using System.Linq;
using ResearchXBRL.Application.DTO.ReverseLookupAccountItems;
using System.Threading.Tasks;
using ResearchXBRL.Application.DTO.Results;
using System.Collections.Generic;

namespace ResearchXBRL.Tests.Infrastructure.QueryService;

public sealed class ReverseDictionaryCSVQueryServiceTests
{
    private readonly LocalFileStorage fileStorage = new(".");

    [Fact(DisplayName = "ファイルがない場合中断オブジェクトを返却する")]
    public void Test1()
    {
        // arrange
        var service = new ReverseDictionaryCSVQueryService(fileStorage, "notExists.csv");

        // act & assert
        Assert.IsType<Abort<IAsyncEnumerable<FinancialReport>>>(service.Get());
    }

    [Fact(DisplayName = "csvのデータを取得できる")]
    public async Task Test2()
    {
        // arrange
        var service = new ReverseDictionaryCSVQueryService(fileStorage, "ReverseLookupDictionary.csv");

        // act
        if (service.Get() is not Success<IAsyncEnumerable<FinancialReport>> success)
        {
            throw new Exception("Test Failed.");
        }

        var actual = await success.Value.ElementAtAsync(3);

        // assert
        Assert.Equal(1301, actual.SecuritiesCode);
        Assert.Equal(AccountingStandards.Japanese, actual.AccountingStandard);
        Assert.Equal(new DateOnly(2020, 3, 31), actual.FiscalYear);
        Assert.Equal(262519000000, actual.AccountAmounts["NetSales"]);
        Assert.Equal(111184000000, actual.AccountAmounts["TotalAssets"]);
        Assert.Equal(32593000000, actual.AccountAmounts["NetAssets"]);
        Assert.Equal(3608000000, actual.AccountAmounts["OrdinaryIncome"]);
        Assert.Equal(2918000000, actual.AccountAmounts["OperatingIncome"]);
        Assert.Equal(2037000000, actual.AccountAmounts["ProfitLossAttributableToOwnersOfParent"]);
        Assert.Equal(24245000000, actual.AccountAmounts["GrossProfit"]);
        Assert.Equal(9410000000, actual.AccountAmounts["NetCashProvidedByUsedInOperatingActivities"]);
        Assert.Equal(78591000000, actual.AccountAmounts["Liabilities"]);
        Assert.Equal(70, actual.AccountAmounts["DividendPaidPerShareSummaryOfBusinessResults"]);
    }

    [Fact(DisplayName = "NAのデータは取得しない")]
    public async Task Test3()
    {
        // arrange
        var service = new ReverseDictionaryCSVQueryService(fileStorage, "ReverseLookupDictionary.csv");

        // act
        if (service.Get() is not Success<IAsyncEnumerable<FinancialReport>> success)
        {
            throw new Exception("Test Failed.");
        }
        var actual = await success.Value.ElementAtAsync(2);

        // assert
        Assert.False(actual.AccountAmounts.ContainsKey("NetSales"));
    }

    [Fact(DisplayName = "ディレクトリが指定された場合中断オブジェクトを返却する")]
    public void Test4()
    {
        // arrange
        var service = new ReverseDictionaryCSVQueryService(fileStorage, "directory");

        // act & assert
        Assert.IsType<Abort<IAsyncEnumerable<FinancialReport>>>(service.Get());
    }
}

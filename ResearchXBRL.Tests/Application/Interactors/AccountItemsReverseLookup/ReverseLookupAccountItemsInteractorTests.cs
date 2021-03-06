using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using ResearchXBRL.Application.DTO.Results;
using ResearchXBRL.Application.DTO.ReverseLookupAccountItems;
using ResearchXBRL.Application.Interactors.ReverseLookupAccountItems;
using ResearchXBRL.Application.QueryServices.ReverseLookupAccountItems;
using ResearchXBRL.Application.Usecase.ReverseLookupAccountItems;
using ResearchXBRL.Domain.ReverseLookupAccountItems.AccountItems;
using Xunit;

namespace ResearchXBRL.Tests.Application.Interactors.ReverseLookupAccountItems;

public sealed class ReverseLookupAccountItemsInteractorTests
{
    public sealed class HandleTests
    {
        private readonly Mock<IReverseDictionaryQueryService> reverseDictionaryQueryServiceMock = new();
        private readonly Mock<IReverseLookupQueryService> reverseLookupQueryServiceMock = new();
        private readonly Mock<IAccountItemsRepository> repositoryMock = new();

        [Fact(DisplayName = "逆引き辞書の要素数だけ逆引きを行う")]
        public async Task Test1()
        {
            // arrange
            var lookupParameters = new List<FinancialReport>
            {
                new FinancialReport
                {
                    SecuritiesCode = 1111,
                    AccountAmounts = new Dictionary<string, decimal?>()
                },
                new FinancialReport
                {
                    SecuritiesCode = 1112,
                    AccountAmounts = new Dictionary<string, decimal?>()
                },
            };
            reverseDictionaryQueryServiceMock
                .Setup(x => x.Get())
                .Returns(new Succeeded<IAsyncEnumerable<FinancialReport>>(lookupParameters.ToAsyncEnumerable()));
            var reverseLookupResult = CreateReverseLookupQueryServiceMock();
            repositoryMock
                .Setup(x => x.Add(It.IsAny<IAsyncEnumerable<AccountItem>>()))
                .Callback<IAsyncEnumerable<AccountItem>>(async x => await x.ToArrayAsync());

            var interactor = new ReverseLookupAccountItemsInteractor(
                reverseDictionaryQueryServiceMock.Object,
                reverseLookupQueryServiceMock.Object,
                repositoryMock.Object,
                new Mock<ILogger<ReverseLookupAccountItemsInteractor>>().Object
            );

            // act
            await interactor.Handle();

            // assert
            reverseLookupQueryServiceMock
                .Verify(x => x.Lookup(It.IsAny<FinancialReport>()),
                Times.Exactly(lookupParameters.Count), "逆引き辞書の要素数だけ逆引きを行う");
        }

        [Fact(DisplayName = "逆引き辞書をもとに逆引きを行う")]
        public async Task Test2()
        {
            // arrange
            var lookupParameters = new List<FinancialReport>
            {
                new FinancialReport
                {
                    SecuritiesCode = 1111,
                    AccountAmounts = new Dictionary<string, decimal?>()
                },
                new FinancialReport
                {
                    SecuritiesCode = 1112,
                    AccountAmounts = new Dictionary<string, decimal?>()
                },
            };
            reverseDictionaryQueryServiceMock
                .Setup(x => x.Get())
                .Returns(new Succeeded<IAsyncEnumerable<FinancialReport>>(lookupParameters.ToAsyncEnumerable()));
            var reverseLookupResult = CreateReverseLookupQueryServiceMock();
            var interactor = new ReverseLookupAccountItemsInteractor(
                reverseDictionaryQueryServiceMock.Object,
                reverseLookupQueryServiceMock.Object,
                repositoryMock.Object,
                new Mock<ILogger<ReverseLookupAccountItemsInteractor>>().Object
            );
            // lookup処理は遅延実行なので以下設定がないと動かない。
            // 処理を動かすため、Addメソッド実行時に引数のリスト要素全件を評価している
            repositoryMock
                .Setup(x => x.Add(It.IsAny<IAsyncEnumerable<AccountItem>>()))
                .Callback<IAsyncEnumerable<AccountItem>>(async x => await x.ToArrayAsync());

            // act
            await interactor.Handle();

            // assert
            reverseLookupQueryServiceMock
                .Verify(x => x.Lookup(It.Is<FinancialReport>(p => p == lookupParameters[0])),
                Times.Once);
            reverseLookupQueryServiceMock
                .Verify(x => x.Lookup(It.Is<FinancialReport>(p => p == lookupParameters[1])),
                Times.Once);
        }

        [Fact(DisplayName = "逆引き結果をリポジトリへ登録する")]
        public async Task Test3()
        {
            // arrange
            var lookupParameters = new List<FinancialReport>
            {
                new FinancialReport
                {
                    SecuritiesCode = 1111,
                    AccountAmounts = new Dictionary<string, decimal?>()
                },
            };
            reverseDictionaryQueryServiceMock
                .Setup(x => x.Get())
                .Returns(new Succeeded<IAsyncEnumerable<FinancialReport>>(lookupParameters.ToAsyncEnumerable()));
            var reverseLookupResult = CreateReverseLookupQueryServiceMock();
            var interactor = new ReverseLookupAccountItemsInteractor(
                reverseDictionaryQueryServiceMock.Object,
                reverseLookupQueryServiceMock.Object,
                repositoryMock.Object,
                new Mock<ILogger<ReverseLookupAccountItemsInteractor>>().Object
            );

            // act
            await interactor.Handle();

            // assert
            repositoryMock
            .Verify(x =>
                x.Add(
                    It.Is<IAsyncEnumerable<AccountItem>>(
                        x => x.ToEnumerable().ElementAt(0).NormalizedName == reverseLookupResult[0].NormalizedName
                         && x.ToEnumerable().ElementAt(0).OriginalName == reverseLookupResult[0].OriginalName))
                , Times.Once);
            repositoryMock
            .Verify(x =>
                x.Add(
                    It.Is<IAsyncEnumerable<AccountItem>>(
                        x => x.ToEnumerable().ElementAt(1).NormalizedName == reverseLookupResult[1].NormalizedName
                         && x.ToEnumerable().ElementAt(1).OriginalName == reverseLookupResult[1].OriginalName))
                , Times.Once);
        }

        [Fact(DisplayName = "revereDictionaryQueryServiceが中断したとき、付随するメッセージをPresenterへ送る")]
        public async void Test4()
        {
            // arrange
            var expectedMessage = "中断";
            reverseDictionaryQueryServiceMock
                .Setup(x => x.Get())
                .Returns(new Abort<IAsyncEnumerable<FinancialReport>>
                {
                    Message = expectedMessage
                });
            var mockLogger = new Mock<ILogger<ReverseLookupAccountItemsInteractor>>();
            var interactor = new ReverseLookupAccountItemsInteractor(
                reverseDictionaryQueryServiceMock.Object,
                reverseLookupQueryServiceMock.Object,
                repositoryMock.Object,
                mockLogger.Object
            );

            // act
            await interactor.Handle();

            // assert
            mockLogger.Verify(x => x.Log<It.IsAnyType>(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        private IReadOnlyList<ReverseLookupResult> CreateReverseLookupQueryServiceMock()
        {
            var reverseLookupResult = new List<ReverseLookupResult>
            {
                new ReverseLookupResult("NetSales", "hoge", 1, System.DateOnly.MaxValue),
                new ReverseLookupResult("ROE", "fuga", 1, System.DateOnly.MaxValue)
            };
            reverseLookupQueryServiceMock
                .Setup(x => x.Lookup(It.IsAny<FinancialReport>()))
                .ReturnsAsync(reverseLookupResult);
            return reverseLookupResult;
        }
    }
}

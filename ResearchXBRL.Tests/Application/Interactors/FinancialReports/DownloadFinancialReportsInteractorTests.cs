﻿using Moq;
using ResearchXBRL.Application.DTO;
using ResearchXBRL.Application.FinancialReports;
using ResearchXBRL.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ResearchXBRL.Tests.Application.Interactors.FinancialReports
{
    public sealed class DownloadFinancialReportsInteractorTests
    {
        public class HandleTests
        {
            private readonly Mock<IEdinetXBRLDownloader> downloader;
            private readonly Mock<IEdinetXBRLParser> parser;

            public HandleTests()
            {
                downloader = new();
                parser = new();
            }

            public sealed class 正常系 : HandleTests
            {
                [Fact]
                public async Task 引数と同じ値をDonwloaderへ渡す()
                {
                    // arrange
                    RegisterDownloadResult(AsyncEnumerable.Empty<EdinetXBRLData>());
                    var interactor = CreateInteractor();
                    var (expectedStart, expectedEnd) = (new DateTimeOffset(2019, 11, 24, 14, 15, 1, TimeSpan.FromHours(9)),
                        new DateTimeOffset(2021, 1, 3, 4, 5, 10, TimeSpan.FromHours(9)));

                    // act
                    await interactor
                        .Handle(expectedStart, expectedEnd);

                    // assert
                    downloader
                        .Verify(x => x.Download(expectedStart, expectedEnd),
                        Times.Once);
                }

                [Fact]
                public async Task Downloaderから返った値を全てParseする()
                {
                    // arrange
                    var expectedDownloadResult = new EdinetXBRLData[]
                    {
                        new EdinetXBRLData
                        {
                            DocumentId = Guid.NewGuid().ToString(),
                        },
                        new EdinetXBRLData
                        {
                            DocumentId = Guid.NewGuid().ToString(),
                        },
                        new EdinetXBRLData
                        {
                            DocumentId = Guid.NewGuid().ToString(),
                        },
                    };
                    RegisterDownloadResult(expectedDownloadResult.ToAsyncEnumerable());
                    var interactor = CreateInteractor();

                    // act
                    await interactor
                        .Handle(DateTimeOffset.Now, DateTimeOffset.Now);

                    // assert
                    parser.Verify(x => x.Parse(It.IsAny<EdinetXBRLData>()),
                            Times.Exactly(expectedDownloadResult.Length));
                }
            }

            public sealed class 異常系 : HandleTests
            {
                [Fact]
                public async Task 引数のstartよりもendが前の場合例外を出す()
                {
                    // arrange
                    RegisterDownloadResult(AsyncEnumerable.Empty<EdinetXBRLData>());
                    var interactor = CreateInteractor();
                    var (start, end) = (new DateTimeOffset(2019, 11, 24, 14, 15, 1, TimeSpan.FromHours(9)),
                        new DateTimeOffset(2019, 11, 24, 14, 15, 0, TimeSpan.FromHours(9)));

                    // act & assert
                    await Assert.ThrowsAsync<ArgumentException>(()
                        => interactor.Handle(start, end));
                }
            }

            private DownloadFinancialReportsInteractor CreateInteractor()
            {
                return new DownloadFinancialReportsInteractor(
                    downloader.Object,
                    parser.Object);
            }

            private void RegisterDownloadResult(IAsyncEnumerable<EdinetXBRLData> reports)
            {
                downloader
                    .Setup(x => x.Download(
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<DateTimeOffset>()))
                    .Returns(reports);
            }
        }
    }
}

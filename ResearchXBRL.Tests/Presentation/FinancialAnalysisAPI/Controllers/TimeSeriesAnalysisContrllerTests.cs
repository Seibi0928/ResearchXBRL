using System;
using System.Collections.Generic;
using FinancialAnalysisAPI.Controllers;
using Microsoft.Extensions.Logging;
using Moq;
using ResearchXBRL.Application.ViewModel.FinancialAnalysis.TimeSeriesAnalysis;
using ResearchXBRL.Application.Usecase.FinancialAnalysis.TimeSeriesAnalysis;
using Xunit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ResearchXBRL.Tests.Presentation.FinancialAnalysisAPI.Controllers
{
    public sealed class TimeSeriesAnalysisControllerTests
    {
        public sealed class GetTimeSeriesAnalysisResultTests
        {
            private readonly Mock<ILogger<TimeSeriesAnalysisController>> logger;
            private readonly Mock<IPerformTimeSeriesAnalysisUsecase> usecase;

            public GetTimeSeriesAnalysisResultTests()
            {
                logger = new();
                usecase = new();
            }

            [Fact]
            public async Task 分析結果を返す()
            {
                // arrange
                var corporationId = "aaa";
                var accountItemName = "bbb";
                var expected = new TimeSeriesAnalysisViewModel
                {
                    AccountName = "test",
                    Unit = new UnitViewModel
                    {
                        Name = "testJPY",
                        Measure = "aaa"
                    },
                    ConsolidatedValues = new List<AccountValueViewModel>
                    {
                        new AccountValueViewModel
                        {
                            Amount = 1114
                        },
                        new AccountValueViewModel
                        {
                            Amount = 1114
                        }
                    }
                };
                usecase.Setup(x => x.Handle(It.Is<AnalyticalMaterials>(a =>
                    a.CorporationId == corporationId
                    && a.AccountItemName == accountItemName)))
                    .ReturnsAsync(expected);
                var controller = new TimeSeriesAnalysisController(
                    logger.Object,
                    usecase.Object);

                // act
                var response = await controller.GetTimeSeriesAnalysisResult(
                    corporationId,
                    accountItemName);

                // assert
                Assert.StrictEqual(expected, response.Value);
            }

            [Fact]
            public async Task UsecaseからArgumentExceptionが出力されたときBadRequestを返す()
            {
                // arrange
                usecase
                    .Setup(x => x.Handle(It.IsAny<AnalyticalMaterials>()))
                    .ThrowsAsync(new ArgumentException());
                var controller = new TimeSeriesAnalysisController(
                    logger.Object,
                    usecase.Object);

                // act
                var response = await controller.GetTimeSeriesAnalysisResult("", "");

                // assert
                Assert.IsType<BadRequestObjectResult>(response.Result);
            }
        }
    }
}

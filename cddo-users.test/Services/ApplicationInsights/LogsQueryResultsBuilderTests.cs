using AutoFixture.AutoMoq;
using AutoFixture;
using cddo_users.Services.ApplicationInsights;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Moq;
using cddo_users.Interface;

namespace cddo_users.test.Services.ApplicationInsights;

[TestFixture]
public class LogsQueryResultsBuilderTests
{
    #region BuildLogsQueryDataResultFromLogsQueryResult() Tests
    [Test]
    public void GivenANullLogsQueryResult_WhenIBuildLogsQueryDataResultFromLogsQueryResult_ThenAnArgumentNullExceptionIsThrown()
    {
        var testItems = CreateTestItems();

        Assert.That(() => testItems.LogsQueryResultsBuilder.BuildLogsQueryDataResultFromLogsQueryResultAsync(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("logsQueryResult"));
    }

    // TODO: Need to provide proxy round LogsQueryResult so that results can be mocked
    #endregion

    #region Test Item Creation
    private static TestItems CreateTestItems()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());

        var mockLogger = Mock.Get(fixture.Freeze<ILogger<LogsQueryResultsBuilder>>());
        var mockAnonymizedUserInformationPopulation = Mock.Get(fixture.Freeze<IAnonymizedUserInformationPopulation>());

        var logsQueryResultsBuilder = new LogsQueryResultsBuilder(
            mockLogger.Object,
            mockAnonymizedUserInformationPopulation.Object);

        return new TestItems(
            fixture,
            logsQueryResultsBuilder,
            mockLogger,
            mockAnonymizedUserInformationPopulation);
    }

    private class TestItems(
        IFixture fixture,
        ILogsQueryResultsBuilder logsQueryResultsBuilder,
        Mock<ILogger<LogsQueryResultsBuilder>> mockLogger,
        Mock<IAnonymizedUserInformationPopulation> mockAnonymizedUserInformationPopulation)
    {
        public IFixture Fixture { get; } = fixture;
        public ILogsQueryResultsBuilder LogsQueryResultsBuilder { get; } = logsQueryResultsBuilder;
        public Mock<ILogger<LogsQueryResultsBuilder>> MockLogger { get; } = mockLogger;
        public Mock<IAnonymizedUserInformationPopulation> MockAnonymizedUserInformationPopulation { get; } = mockAnonymizedUserInformationPopulation;
    }
    #endregion
}
using AutoFixture;
using AutoFixture.AutoMoq;
using cddo_users.Interface;
using cddo_users.Services.ApplicationInsights;
using Moq;
using NUnit.Framework;

namespace cddo_users.test.Services.ApplicationInsights;

[TestFixture]
public class LogsQueryBuilderTests
{
    #region BuildLogsQuery() Tests
    [Test]
    public void GivenANullLogsQueryUserFilter_WhenIBuildLogsQuery_ThenAnArgumentNullExceptionIsThrown()
    {
        var testItems = CreateTestItems();

        Assert.That(() => testItems.LogsQueryBuilder.BuildLogsQuery(
            It.IsAny<string?>(),
            testItems.Fixture.CreateMany<string>(),
            null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("logsQueryFilter"));
    }

    [Test]
    public void GivenATableName_WhenIBuildLogsQuery_ThenTheQueryIsComposedForThatTableName()
    {
        var testItems = CreateTestItems();

        var logsQuery = testItems.LogsQueryBuilder.BuildLogsQuery(
            "test table name",
            testItems.Fixture.CreateMany<string>(),
            testItems.Fixture.Create<ILogsQueryFilter>());

        Assert.That(logsQuery.StartsWith("test table name"));
    }

    [Test]
    public void GivenANullTableName_WhenIBuildLogsQuery_ThenTheQueryIsComposedForTheAppEventsTableName()
    {
        var testItems = CreateTestItems();

        var logsQuery = testItems.LogsQueryBuilder.BuildLogsQuery(
            null,
            testItems.Fixture.CreateMany<string>(),
            testItems.Fixture.Create<ILogsQueryFilter>());

        Assert.That(logsQuery.StartsWith("AppEvents"));
    }

    [Test]
    public void GivenSearchClauses_WhenIBuildLogsQuery_ThenTheQueryIsComposedForThoseSearchClauses()
    {
        var testItems = CreateTestItems();

        var testSearchClauses = new List<string>
        {
            "test clause 1",
            "  test clause 2",
            "test clause 3  ",
            "  test clause 4  "
        };

        var logsQuery = testItems.LogsQueryBuilder.BuildLogsQuery(
            It.IsAny<string?>(),
            testSearchClauses,
            testItems.Fixture.Create<ILogsQueryFilter>());

        Assert.Multiple(() =>
        {
            Assert.That(logsQuery.Contains("| test clause 1"));
            Assert.That(logsQuery.Contains("| test clause 2"));
            Assert.That(logsQuery.Contains("| test clause 3"));
            Assert.That(logsQuery.Contains("| test clause 4"));
        });
        
    }

    [Test]
    public void GivenSearchClauses_WhenIBuildLogsQuery_ThenTheSearchClausesAreComposedIntoTheQueryInTheGivenOrder()
    {
        var testItems = CreateTestItems();

        var testSearchClauses = new List<string>
        {
            "test clause 3",
            "test clause 1",
            "test clause 4",
            "test clause 2"
        };

        var logsQuery = testItems.LogsQueryBuilder.BuildLogsQuery(
            It.IsAny<string?>(),
            testSearchClauses,
            testItems.Fixture.Create<ILogsQueryFilter>());

        var indexOfFirstClause = logsQuery.IndexOf(testSearchClauses[0], StringComparison.InvariantCultureIgnoreCase);
        var indexOfSecondClause = logsQuery.IndexOf(testSearchClauses[1], StringComparison.InvariantCultureIgnoreCase);
        var indexOfThirdClause = logsQuery.IndexOf(testSearchClauses[2], StringComparison.InvariantCultureIgnoreCase);
        var indexOfFourthClause = logsQuery.IndexOf(testSearchClauses[3], StringComparison.InvariantCultureIgnoreCase);

        Assert.Multiple(() =>
        {
            Assert.That(indexOfFirstClause, Is.GreaterThan(0));
            Assert.That(indexOfSecondClause, Is.GreaterThan(indexOfFirstClause));
            Assert.That(indexOfThirdClause, Is.GreaterThan(indexOfSecondClause));
            Assert.That(indexOfFourthClause, Is.GreaterThan(indexOfThirdClause));
        });
    }

    [Test]
    [TestCaseSource(nameof(UserBasedFilteringTestCaseData))]
    public void GivenALogsQueryUserFilter_WhenIBuildLogsQuery_ThenAnOperatorShouldBeAddedForFilteringByTheGivenUserFilterProperties(
        bool filterByOrganisation,
        int organisationId,
        bool filterByDomain,
        int domainId,
        bool filterByUser,
        int userId)
    {
        var testItems = CreateTestItems();

        var testLogsQueryUserFilter = CreateTestLogsQueryUserFilter(
            filterByOrganisation: filterByOrganisation,
            organisationId: organisationId,
            filterByDomain: filterByDomain,
            domainId: domainId,
            filterByUser: filterByUser,
            userId: userId);

        var logsQuery = testItems.LogsQueryBuilder.BuildLogsQuery(
            It.IsAny<string?>(),
            testItems.Fixture.CreateMany<string>(),
            testLogsQueryUserFilter);

        Assert.Multiple(() =>
        {
            if (filterByOrganisation)
            {
                Assert.That(logsQuery.Contains($"Properties.OrganisationId == {organisationId}"));
            }
            else
            {
                Assert.That(logsQuery.Contains("Properties.OrganisationId"), Is.False);
            }

            if (filterByDomain)
            {
                Assert.That(logsQuery.Contains($"Properties.DomainId == {domainId}"));
            }
            else
            {
                Assert.That(logsQuery.Contains("Properties.DomainId"), Is.False);
            }

            if (filterByUser)
            {
                Assert.That(logsQuery.Contains($"Properties.UserId == {userId}"));
            }
            else
            {
                Assert.That(logsQuery.Contains("Properties.UserId"), Is.False);
            }
        });
    }

    [Test]
    public void GivenALogsQueryUserFilterAndNoSearchWhereClauses_WhenIBuildLogsQuery_ThenTheUserFilterOperatorShouldBeAddedToTheEndOfTheClauses()
    {
        var testItems = CreateTestItems();

        var testSearchClauses = new List<string>
        {
            "test clause 1",
            "test clause 2",
            "test clause 3"
        };

        var testLogsQueryUserFilter = CreateTestLogsQueryUserFilter(filterByOrganisation: true);

        var logsQuery = testItems.LogsQueryBuilder.BuildLogsQuery(
            It.IsAny<string?>(),
            testSearchClauses,
            testLogsQueryUserFilter);

        var indexOfFirstClause = logsQuery.IndexOf(testSearchClauses[0], StringComparison.InvariantCultureIgnoreCase);
        var indexOfSecondClause = logsQuery.IndexOf(testSearchClauses[1], StringComparison.InvariantCultureIgnoreCase);
        var indexOfThirdClause = logsQuery.IndexOf(testSearchClauses[2], StringComparison.InvariantCultureIgnoreCase);
        var indexOfUserFilterClause = logsQuery.IndexOf("Properties.Organisation", StringComparison.InvariantCultureIgnoreCase);

        Assert.Multiple(() =>
        {
            Assert.That(indexOfFirstClause, Is.GreaterThan(0));
            Assert.That(indexOfSecondClause, Is.GreaterThan(indexOfFirstClause));
            Assert.That(indexOfThirdClause, Is.GreaterThan(indexOfSecondClause));
            Assert.That(indexOfUserFilterClause, Is.GreaterThan(indexOfThirdClause));
        });
    }

    [Test]
    public void GivenALogsQueryUserFilterAndSearchWhereClauses_WhenIBuildLogsQuery_ThenTheUserFilterOperatorShouldBeAddedAfterTheFinalWhereClause()
    {
        var testItems = CreateTestItems();

        var testSearchClauses = new List<string>
        {
            "where test clause 1",
            "where test clause 2",
            "select test clause 3"
        };

        var testLogsQueryUserFilter = CreateTestLogsQueryUserFilter(filterByOrganisation: true);

        var logsQuery = testItems.LogsQueryBuilder.BuildLogsQuery(
            It.IsAny<string?>(),
            testSearchClauses,
            testLogsQueryUserFilter);

        var indexOfFirstClause = logsQuery.IndexOf(testSearchClauses[0], StringComparison.InvariantCultureIgnoreCase);
        var indexOfSecondClause = logsQuery.IndexOf(testSearchClauses[1], StringComparison.InvariantCultureIgnoreCase);
        var indexOfThirdClause = logsQuery.IndexOf(testSearchClauses[2], StringComparison.InvariantCultureIgnoreCase);
        var indexOfUserFilterClause = logsQuery.IndexOf("Properties.Organisation", StringComparison.InvariantCultureIgnoreCase);

        Assert.Multiple(() =>
        {
            Assert.That(indexOfFirstClause, Is.GreaterThan(0));
            Assert.That(indexOfSecondClause, Is.GreaterThan(indexOfFirstClause));

            Assert.That(indexOfUserFilterClause, Is.GreaterThan(indexOfSecondClause));

            Assert.That(indexOfThirdClause, Is.GreaterThan(indexOfUserFilterClause));
        });
    }

    [Test]
    public void GivenALogsQueryUserFilterAndEndsWithSearchWhereClauses_WhenIBuildLogsQuery_ThenTheUserFilterOperatorShouldBeAddedAfterTheFinalWhereClause()
    {
        var testItems = CreateTestItems();

        var testSearchClauses = new List<string>
        {
            "where test clause 1",
            "where test clause 2",
            "where test clause 3"
        };

        var testLogsQueryUserFilter = CreateTestLogsQueryUserFilter(filterByOrganisation: true);

        var logsQuery = testItems.LogsQueryBuilder.BuildLogsQuery(
            It.IsAny<string?>(),
            testSearchClauses,
            testLogsQueryUserFilter);

        var indexOfFirstClause = logsQuery.IndexOf(testSearchClauses[0], StringComparison.InvariantCultureIgnoreCase);
        var indexOfSecondClause = logsQuery.IndexOf(testSearchClauses[1], StringComparison.InvariantCultureIgnoreCase);
        var indexOfThirdClause = logsQuery.IndexOf(testSearchClauses[2], StringComparison.InvariantCultureIgnoreCase);
        var indexOfUserFilterClause = logsQuery.IndexOf("Properties.Organisation", StringComparison.InvariantCultureIgnoreCase);

        Assert.Multiple(() =>
        {
            Assert.That(indexOfFirstClause, Is.GreaterThan(0));
            Assert.That(indexOfSecondClause, Is.GreaterThan(indexOfFirstClause));
            Assert.That(indexOfThirdClause, Is.GreaterThan(indexOfSecondClause));
            Assert.That(indexOfUserFilterClause, Is.GreaterThan(indexOfThirdClause));
        });
    }

    private static IEnumerable<TestCaseData> UserBasedFilteringTestCaseData()
    {
        var fixture = new Fixture();

        var organisationId = fixture.Create<int>();
        var domainId = fixture.Create<int>();
        var userId = fixture.Create<int>();

        yield return CreateTestCase(false, false, false);
        yield return CreateTestCase(false, false, true);
        yield return CreateTestCase(false, true, false);
        yield return CreateTestCase(false, true, true);
        yield return CreateTestCase(true, false, false);
        yield return CreateTestCase(true, false, true);
        yield return CreateTestCase(true, true, false);
        yield return CreateTestCase(true, true, true);
        

        TestCaseData CreateTestCase(bool filterByOrganisationId, bool filterByDomainId, bool filterByUserId)
        {
            return new TestCaseData(
                filterByOrganisationId,
                filterByOrganisationId ? organisationId : It.IsAny<int>(),
                filterByDomainId,
                filterByDomainId ? domainId : It.IsAny<int>(),
                filterByUserId,
                filterByUserId ? userId : It.IsAny<int>());
        }
    }
    #endregion

    #region Test Data Creation
    private static ILogsQueryFilter CreateTestLogsQueryUserFilter(
        bool? filterByOrganisation = null,
        bool? filterByDomain = null,
        bool? filterByUser = null,
        int? organisationId = null,
        int? domainId = null,
        int? userId = null)
    {
        var mockLogsQueryUserFilter = new Mock<ILogsQueryFilter>();

        mockLogsQueryUserFilter.SetupGet(x => x.FilterByOrganisation).Returns(filterByOrganisation ?? false);
        mockLogsQueryUserFilter.SetupGet(x => x.FilterByDomain).Returns(filterByDomain ?? false);
        mockLogsQueryUserFilter.SetupGet(x => x.FilterByUser).Returns(filterByUser ?? false);

        mockLogsQueryUserFilter.SetupGet(x => x.OrganisationId ).Returns(organisationId ?? It.IsAny<int>());
        mockLogsQueryUserFilter.SetupGet(x => x.DomainId).Returns(domainId ?? It.IsAny<int>());
        mockLogsQueryUserFilter.SetupGet(x => x.UserId).Returns(userId ?? It.IsAny<int>());

        return mockLogsQueryUserFilter.Object;
    }
    #endregion

    #region Test Item Creation
    private static TestItems CreateTestItems()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());

        var logsQueryBuilder = new LogsQueryBuilder();

        return new TestItems(
            fixture,
            logsQueryBuilder);
    }

    private class TestItems(
        IFixture fixture,
        ILogsQueryBuilder logsQueryBuilder)
    {
        public IFixture Fixture { get; } = fixture;
        public ILogsQueryBuilder LogsQueryBuilder { get; } = logsQueryBuilder;
    }
    #endregion
}

using cddo_users.Interface;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using cddo_users.DTOs.EventLogs;
using cddo_users.DTOs;
using cddo_users.Repositories;

namespace cddo_users.Services.ApplicationInsights
{
    public class ApplicationInsightsService : IApplicationInsightsService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAzureLogsQueryClient _azureLogsQueryClient;
        private readonly ILogsQueryResultsBuilder _logsQueryResultsBuilder;
        private readonly IUserRepository _userRepository;
        private readonly ILogsQueryFilterProvision _logsQueryFilterProvision;
        private readonly ILogsQueryBuilder _logsQueryBuilder;

        public ApplicationInsightsService(
            IConfiguration configuration,
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IAzureLogsQueryClient azureLogsQueryClient,
            ILogsQueryResultsBuilder logsQueryResultsBuilder,
            ILogsQueryFilterProvision logsQueryFilterProvision,
            ILogsQueryBuilder logsQueryBuilder)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            _azureLogsQueryClient = azureLogsQueryClient;
            _logsQueryResultsBuilder = logsQueryResultsBuilder;
            _logsQueryFilterProvision = logsQueryFilterProvision;
            _logsQueryBuilder = logsQueryBuilder;
        }

        async Task<EventLogResponse> IApplicationInsightsService.GetEventLogsAsync(int pageSize, string searchQuery)
        {
            // Retrieve the user email from the HTTP context
            var userEmail = _httpContextAccessor.HttpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                throw new ArgumentNullException(userEmail, "User email is missing.");
            }

            // Fetch user profile using the email
            var userProfile = await _userRepository.GetUserByEmailAsync(userEmail);
            if (userProfile == null)
            {
                throw new ArgumentNullException(nameof(userProfile), "User profile could not be retrieved.");
            }

            var userId = userProfile.User.UserId;
            var userRoles = userProfile.Roles.Select(role => role.RoleName).ToList();
            var userOrg = userProfile.Organisation.OrganisationName;

            var apiKey = _configuration["ApplicationInsights:ApiKey"];
            var uri = _configuration["ApplicationInsights:Uri"];

            // Construct the base query
            var baseQuery = $@"customEvents
                   | where tostring(customDimensions.UserOrg) == '{userOrg}'
                   | where timestamp > ago(90d)";

            // Add role-based filters
            if (userRoles.Contains("Admin"))
            {
                // Admins can see all events
                baseQuery += "";
            }
            else
            {
                // Non-admin users can see only their own events
                baseQuery += $@" | where tostring(customDimensions.UserId) == '{userId}'";
            }

            var searchPart = !string.IsNullOrEmpty(searchQuery) ? $"| search '{searchQuery}'" : string.Empty;

            var additionalQuery = @"| extend EventTimestamp = timestamp, EventName = name, Properties = customDimensions
                        | project EventTimestamp, EventName, Properties
                        | order by EventTimestamp desc";

            var kqlQuery = $"{baseQuery}{searchPart}{additionalQuery}";

            var queryPayload = new
            {
                query = kqlQuery,
                timespan = TimeSpan.FromDays(90).ToString()  // Changed to 90 days as per your earlier mention of three months
            };

            // Serialize the payload to JSON
            var jsonPayload = JsonSerializer.Serialize(queryPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Configure the HTTP client
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

            // Send the POST request
            HttpResponseMessage response = await _httpClient.PostAsync(uri, content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                // Deserialize the API response
                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, options);

                var logs = new List<EventLog>();

                // Process each table and row in the response
                foreach (var table in apiResponse.Tables)
                {
                    foreach (var row in table.Rows)
                    {
                        var propertiesIndex = table.Columns.FindIndex(c => c.Name == "Properties");
                        var propertiesJson = row[propertiesIndex].ToString();
                        var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson);

                        var timestampIndex = table.Columns.FindIndex(c => c.Name == "EventTimestamp");
                        var eventNameIndex = table.Columns.FindIndex(c => c.Name == "EventName");

                        var log = new EventLog
                        {
                            EventTimestamp = DateTime.Parse(row[timestampIndex].ToString()),
                            EventName = row[eventNameIndex].ToString(),
                            Properties = properties
                        };
                        logs.Add(log);
                    }
                }

                // Return successful response mapped to DTO
                return new EventLogResponse
                {
                    Logs = logs,
                    TotalRecords = logs.Count
                };
            }
            else
            {
                // Handle failure
                throw new ArgumentNullException($"Failed to retrieve data: {response.StatusCode}");
            }
        }

        async Task<ILogsQueryDataResult> IApplicationInsightsService.GetEventLogsFromRawQueryAsync(
            string searchQuery,
            TimeSpan timeRange,
            UserProfile userProfile)
        {
            var logsQueryFilter = _logsQueryFilterProvision.ProvisionLogsQueryFilter(userProfile);

            var logsQuery = _logsQueryBuilder.ProvisionRawLogsQuery(searchQuery, logsQueryFilter);

            var logsQueryResult = await _azureLogsQueryClient.RunLogsQueryAsync(logsQuery, timeRange);

            var result = await _logsQueryResultsBuilder.BuildLogsQueryDataResultFromLogsQueryResultAsync(logsQueryResult);

            return result;
        }

        async Task<ILogsQueryDataResult> IApplicationInsightsService.GetEventLogsAsync(
            string? tableName,
            IEnumerable<string> searchClauses,
            TimeSpan timeRange,
            UserProfile userProfile)
        {
            var logsQueryFilter = _logsQueryFilterProvision.ProvisionLogsQueryFilter(userProfile);

            var logsQuery = _logsQueryBuilder.BuildLogsQuery(tableName, searchClauses, logsQueryFilter);

            var logsQueryResult = await _azureLogsQueryClient.RunLogsQueryAsync(logsQuery, timeRange);

            return await _logsQueryResultsBuilder.BuildLogsQueryDataResultFromLogsQueryResultAsync(logsQueryResult);
        }
    }

    // Supporting classes and structures
    public class ApiResponse
    {
        public List<ApiTable> Tables { get; set; }
    }

    public class ApiTable
    {
        public List<ApiColumn> Columns { get; set; }
        public List<List<object>> Rows { get; set; }
    }

    public class ApiColumn
    {
        public string Name { get; set; }
        public string Type { get; set; }  // This can be further used if needed
    }
}



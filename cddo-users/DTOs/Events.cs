namespace cddo_users.DTOs
{
    public class EventLog
    {
        public DateTime EventTimestamp { get; set; }
        public string EventName { get; set; }
        public IDictionary<string, object> Properties { get; set; }
        public int Count { get; set; }
    }

    public class EventLogResponse
    {
        public List<EventLog> Logs { get; set; }
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}

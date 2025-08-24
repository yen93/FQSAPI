namespace FQSAPI.Models
{
    public class ActiveQueueModel
    {
        public List<int> Queued { get; set; } = new List<int>();
        public List<int> InProgress { get; set; } = new List<int>();
        public List<int> Completed { get; set; } = new List<int>();
    }
}

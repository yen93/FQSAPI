namespace FQSAPI.Models
{
    public class ActiveOrdersModel
    {
        public int OrderId { get; set; }
        public List<OrderItemModel> Items { get; set; }
        public string Status { get; set; }
    }
}

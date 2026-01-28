namespace WMS.Backend.API.Dtos
{
    public class AllocationResult
    {
        public string OrderId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}

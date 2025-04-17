namespace APIAggregator.Models {
    public class AggregatorRequest 
    {

        public string? FilterBy { get; set; }
        public string? FilterValue { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }
}

namespace APIAggregator.Models {
    public class ApiConfig {
        public string Url { get; set; } = default!;
        public string ArrayPath { get; set; } = "items";
    }
}

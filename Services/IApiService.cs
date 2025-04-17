namespace APIAggregator.Services {
    public interface IApiService
    {
        Task<string> GetData(string apiUrl);
    }
}

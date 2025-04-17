namespace APIAggregator.Services {
    public interface IApiService
    {
        Task<string> GetRawJson(string apiUrl);
    }
}

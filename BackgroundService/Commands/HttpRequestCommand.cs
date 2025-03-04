namespace BackgroundService.Commands
{
    internal class HttpRequestCommand : IServiceCommand
    {
        private readonly string url;
        private readonly HttpClient httpClient;

        public HttpRequestCommand(string url)
        {
            this.url = url;
            this.httpClient = new HttpClient();
        }

        public async Task<string> ExecuteAsync()
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}

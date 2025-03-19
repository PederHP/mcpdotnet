namespace AspNetCoreSseServer
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string API_KEY_HEADER_NAME = "X-API-KEY";
        private readonly string _expectedApiKey;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            // Retrieve the expected API key from configuration
            _expectedApiKey = configuration["AppSettings:apikey"];
            if (string.IsNullOrWhiteSpace(_expectedApiKey))
            {
                throw new InvalidOperationException("apikey is not set in configuration. Please check your appsettings.json.");
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the API key header exists
            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out var extractedApiKey))
            {
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("API Key was not provided.");
                return;
            }

            // Validate the API key
            if (!string.Equals(extractedApiKey, _expectedApiKey))
            {
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}

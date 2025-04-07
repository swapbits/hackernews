using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Extensions.Http;
using Stories.Application.Clients;
using Stories.Application.Services;
using Stories.Infrastructure;

namespace Stories.Api;

public static class DependencyInjection
{
    public static WebApplicationBuilder ConfigureApplication(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        if (builder.Configuration.GetValue<bool>("HackerNews:UseCache"))
        {
            services.AddScoped<IBestStoriesService, BestStoriesCachingService>();
            services.AddMemoryCache();
            services.AddSingleton<ICache>(provider => new Cache(provider.GetRequiredService<IMemoryCache>(), TimeSpan.FromSeconds(30)));
        }
        else
        {
            services.AddScoped<IBestStoriesService, BestStoriesService>();  
        }
        
        services.AddScoped<IHackerNewsClient, HackerNewsClient>();
        
        var baseUrl = GetRequiredBaseUrl();
        services.AddHttpClient<IHackerNewsClient, HackerNewsClient>(client => client.BaseAddress = new Uri(baseUrl))
            .AddPolicyHandler(GetRetryPolicy());
        
        return builder;
        
        string GetRequiredBaseUrl()
        {
            var url = builder.Configuration["HackerNews:BaseUrl"];
            if (string.IsNullOrWhiteSpace(url))
            { 
                throw new ArgumentException("HackerNews:BaseUrl is required");
            }

            return url;
        }
    }
    
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }    
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace CachingProxy;

public class ProxyServer
{
    private readonly int _port;
    private readonly Uri _originUri;
    private readonly CacheManager _cacheManager;
    private readonly HttpClient _httpClient;

    public ProxyServer(int port, Uri originUri, CacheManager cacheManager)
    {
        _port = port;
        _originUri = originUri;
        _cacheManager = cacheManager;
        _httpClient = new HttpClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        
        
        builder.Services.AddSingleton(_cacheManager);
        builder.Services.AddSingleton(_httpClient);
        
       
        builder.WebHost.UseUrls($"http://localhost:{_port}");
        
        var app = builder.Build();
        
        
        app.Use(async (context, next) =>
        {
            await HandleRequestAsync(context);
        });
        
        await app.RunAsync(cancellationToken);
    }

    private async Task HandleRequestAsync(HttpContext context)
    {
        try
        {
            var request = context.Request;
            var cacheKey = GenerateCacheKey(request);
            
           
            var cachedResponse = _cacheManager.GetCachedResponse(cacheKey);
            if (cachedResponse != null)
            {
                await SendCachedResponse(context, cachedResponse);
                return;
            }
            
            
            var originResponse = await ForwardRequestToOrigin(request);
            
            
            var responseData = new CachedResponse
            {
                StatusCode = (int)originResponse.StatusCode,
                Headers = originResponse.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray()),
                ContentHeaders = originResponse.Content.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray()),
                Body = await originResponse.Content.ReadAsByteArrayAsync()
            };
            
            _cacheManager.CacheResponse(cacheKey, responseData);
            
           
            await SendOriginResponse(context, originResponse, responseData);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Proxy Error: {ex.Message}");
        }
    }

    private string GenerateCacheKey(HttpRequest request)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(request.Method);
        keyBuilder.Append("|");
        keyBuilder.Append(request.Path);
        keyBuilder.Append(request.QueryString);
        
        
        var relevantHeaders = new[] { "Accept", "Accept-Language", "Authorization" };
        foreach (var header in relevantHeaders)
        {
            if (request.Headers.ContainsKey(header))
            {
                keyBuilder.Append("|");
                keyBuilder.Append(header);
                keyBuilder.Append(":");
                keyBuilder.Append(string.Join(",", request.Headers[header]));
            }
        }
        
        return keyBuilder.ToString();
    }

    private async Task<HttpResponseMessage> ForwardRequestToOrigin(HttpRequest request)
    {
        var targetUri = new Uri(_originUri, request.Path + request.QueryString);
        var requestMessage = new HttpRequestMessage(new HttpMethod(request.Method), targetUri);
        
        
        foreach (var header in request.Headers)
        {
            if (!IsRestrictedHeader(header.Key))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
        
        
        if (request.ContentLength > 0)
        {
            var bodyContent = new byte[request.ContentLength.Value];
            await request.Body.ReadAsync(bodyContent, 0, bodyContent.Length);
            requestMessage.Content = new ByteArrayContent(bodyContent);
            
           
            if (request.ContentType != null)
            {
                requestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);
            }
        }
        
        return await _httpClient.SendAsync(requestMessage);
    }

    private static bool IsRestrictedHeader(string headerName)
    {
        // Headers that shouldn't be copied to avoid conflicts
        var restrictedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Host", "Connection", "Content-Length"
        };
        
        return restrictedHeaders.Contains(headerName);
    }

    private async Task SendCachedResponse(HttpContext context, CachedResponse cachedResponse)
    {
        var response = context.Response;
        response.StatusCode = cachedResponse.StatusCode;
        
        response.Headers["X-Cache"] = "HIT";
        
       
        foreach (var header in cachedResponse.Headers)
        {
            if (!IsResponseRestrictedHeader(header.Key))
            {
                response.Headers[header.Key] = header.Value;
            }
        }
        
      
        foreach (var header in cachedResponse.ContentHeaders)
        {
            if (!IsResponseRestrictedHeader(header.Key))
            {
                response.Headers[header.Key] = header.Value;
            }
        }
        
  
        await response.Body.WriteAsync(cachedResponse.Body, 0, cachedResponse.Body.Length);
    }

    private async Task SendOriginResponse(HttpContext context, HttpResponseMessage originResponse, CachedResponse responseData)
    {
        var response = context.Response;
        response.StatusCode = (int)originResponse.StatusCode;
        
       
        response.Headers["X-Cache"] = "MISS";
        
       
        foreach (var header in originResponse.Headers)
        {
            if (!IsResponseRestrictedHeader(header.Key))
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }
        }
        
        
        foreach (var header in originResponse.Content.Headers)
        {
            if (!IsResponseRestrictedHeader(header.Key))
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }
        }
        
       
        await response.Body.WriteAsync(responseData.Body, 0, responseData.Body.Length);
    }

    private static bool IsResponseRestrictedHeader(string headerName)
    {
        var restrictedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Transfer-Encoding", "Connection"
        };
        
        return restrictedHeaders.Contains(headerName);
    }
}
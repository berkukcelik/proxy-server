using System.CommandLine;
using CachingProxy;

var portOption = new Option<int>(
    name: "--port");
 

var originOption = new Option<string>(
    name: "--origin"

);
var clearCacheOption = new Option<bool>(
    name: "--clear-cache"
);

var rootCommand = new RootCommand("Caching Proxy Server - A CLI tool that starts a caching proxy server");
rootCommand.AddOption(portOption);
rootCommand.AddOption(originOption);
rootCommand.AddOption(clearCacheOption);

rootCommand.SetHandler(async (int port, string origin, bool clearCache) =>
{
    var cacheManager = new CacheManager();
    
    if (clearCache)
    {
        cacheManager.ClearCache();
        Console.WriteLine("Cache cleared successfully!");
        return;
    }
    
    if (port == 0 || string.IsNullOrEmpty(origin))
    {
        Console.WriteLine("Error: Both --port and --origin are required when starting the server.");
        Console.WriteLine("Usage: caching-proxy --port <number> --origin <url>");
        Console.WriteLine("       caching-proxy --clear-cache");
        return;
    }
    
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
    {
        Console.WriteLine($"Error: Invalid origin URL: {origin}");
        return;
    }
    
    var proxyServer = new ProxyServer(port, originUri, cacheManager);
    
    Console.WriteLine($"Starting caching proxy server on port {port}");
    Console.WriteLine($"Forwarding requests to: {origin}");
    Console.WriteLine("Press Ctrl+C to stop the server...");
    
    var cancellationTokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => {
        e.Cancel = true;
        cancellationTokenSource.Cancel();
    };
    
    try
    {
        await proxyServer.StartAsync(cancellationTokenSource.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\nServer stopped.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error starting server: {ex.Message}");
    }
}, portOption, originOption, clearCacheOption);

return await rootCommand.InvokeAsync(args);
using System.CommandLine;

public class AddingCommands
{
    public static Option<int> GetPortOption()
    {
        Option<int> portNumberOption = new("--port")
        {
            Description = "which the caching proxy server will run.",
            Required = true
        };
        return portNumberOption;       
    }
    public static Option<string> GetUrlOption()
    {
        Option<string> urlAdress = new("--url")
        {
            Description = "URL of the server to which the requests will be forwarded.",
            Required = true
        };
        return urlAdress;
    }
    public static RootCommand ConfigureRootCommand(string[] args)
    {
        var rootCommand = new RootCommand("--port for port number , --url for url to request");
        var urlOption = GetUrlOption();
        var portOption = GetPortOption();
        rootCommand.Options.Add(urlOption);
        rootCommand.Options.Add(portOption);
        ParseResult parseResult = rootCommand.Parse(args);
        int? port = parseResult.GetValue(portOption);
        string? url = parseResult.GetValue(urlOption);
        Console.WriteLine(port);
        Console.WriteLine(url);
        if (parseResult.Errors != null)
        {
            foreach (var error in parseResult.Errors)
            {
                Console.WriteLine(error);
            }
        }
        return rootCommand;
        

    }
}
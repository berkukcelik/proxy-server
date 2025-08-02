using System.CommandLine;
using System.CommandLine.Parsing;

class Program
{
    static void Main(string[] args)
    {
        var rootCommand = AddingCommands.ConfigureRootCommand(args);
        
    }
}

 
        
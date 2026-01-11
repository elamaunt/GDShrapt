using System;
using System.Threading;
using System.Threading.Tasks;

namespace GDShrapt.LSP;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Parse arguments
        var useStdio = true;
        var port = 0;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--stdio":
                    useStdio = true;
                    break;
                case "--socket":
                case "--port":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out port))
                    {
                        useStdio = false;
                        i++;
                    }
                    break;
                case "--version":
                    Console.WriteLine("GDShrapt LSP Server 1.0.0");
                    return 0;
                case "--help":
                case "-h":
                    PrintHelp();
                    return 0;
            }
        }

        // Create transport
        IGDJsonRpcTransport transport;
        if (useStdio)
        {
            var serializer = new GDSystemTextJsonSerializer();
            transport = new GDStdioJsonRpcTransport(serializer);
        }
        else
        {
            // TODO: Implement socket transport
            Console.Error.WriteLine("Socket transport not yet implemented");
            return 1;
        }

        // Create and run server
        await using var server = new GDLanguageServer();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await server.InitializeAsync(transport, cts.Token);
            await server.RunAsync(cts.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("GDShrapt Language Server");
        Console.WriteLine();
        Console.WriteLine("Usage: GDShrapt.LSP [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --stdio       Use stdio for communication (default)");
        Console.WriteLine("  --port <n>    Use TCP socket on specified port");
        Console.WriteLine("  --version     Print version and exit");
        Console.WriteLine("  --help, -h    Print this help message");
    }
}

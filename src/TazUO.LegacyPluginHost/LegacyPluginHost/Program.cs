using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace TazUO.LegacyPluginHost;

internal class Program
{
    private static void Main(string[] args)
    {
        string? pluginPath = args.Length > 0 ? args[0] : null;
        string? pipeName = args.Length > 1 ? args[1] : null;
        Console.WriteLine($"Loading plugin: {pluginPath}");

        if (string.IsNullOrEmpty(pluginPath) || !File.Exists(pluginPath))
        {
            Console.Error.WriteLine($"Plugin does not exist: {pluginPath}");
            return;
        }

        if (string.IsNullOrEmpty(pipeName))
        {
            Console.Error.WriteLine("No pipe name provided.");
            return;
        }

        Console.WriteLine($"Starting plugin host with pipe name {pipeName}");
        using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        Console.WriteLine($"Waiting for client...");
        if (!server.WaitForConnectionAsync().Wait(timeout: TimeSpan.FromSeconds(10)))
        {
            Console.Error.WriteLine($"Connection timed out. Aborting.");
            return;
        }

        using var reader = new StreamReader(server);
        using var writer = new StreamWriter(server) { AutoFlush = true };
        //int namedPipeDefaultBufferSize = 4096;
        //using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: namedPipeDefaultBufferSize, leaveOpen: true);
        //using var writer = new StreamWriter(server, Encoding.UTF8, bufferSize: namedPipeDefaultBufferSize, leaveOpen: true) { AutoFlush = true };

        Console.WriteLine($"Client connected. Plugin host ready.");
        /*
        try
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine($"Received: {line}");
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Client disconnected or pipe error. This is generally normal during shutdown: {ex.Message}");
        }
        */

        while (server.CanRead)
        {
            Thread.Sleep(100);
        }

        Console.WriteLine($"Plugin Host shutting down.");
    }
}
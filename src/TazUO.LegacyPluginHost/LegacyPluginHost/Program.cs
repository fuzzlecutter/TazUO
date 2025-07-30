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

        string pluginName = Path.GetFileNameWithoutExtension(pluginPath);
        Console.WriteLine($"[{pluginName}] Starting plugin host with pipe name {pipeName}");

        using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        Console.WriteLine($"[{pluginName}] Waiting for client...");
        DateTime timeout = DateTime.Now + TimeSpan.FromSeconds(10);
        while (!server.IsConnected)
        {
            if (DateTime.Now > timeout)
            {
                Console.Error.WriteLine($"[{pluginName}] Connection timed out. Aborting.");
                return;
            }
            Thread.Sleep(100);
        }

        Console.WriteLine($"[{pluginName}] Client connected.");

        using var reader = new StreamReader(server);
        using var writer = new StreamWriter(server) { AutoFlush = true };
        //int namedPipeDefaultBufferSize = 4096;
        //using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: namedPipeDefaultBufferSize, leaveOpen: true);
        //using var writer = new StreamWriter(server, Encoding.UTF8, bufferSize: namedPipeDefaultBufferSize, leaveOpen: true) { AutoFlush = true };

        try
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine($"[{pluginName}] Received: {line}");

                if (line == "shutdown-request")
                {
                    Console.WriteLine($"[{pluginName}] Shutdown request received. Sending ack...");
                    writer.WriteLine("shutdown-ack");
                    break;
                }
                else
                {
                    Console.WriteLine($"[{pluginName}] Echo from plugin host: {line}");
                    writer.WriteLine($"[{pluginName}] Echo from plugin host: {line}");
                }

            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[{pluginName}] Client disconnected or pipe error. This is generally normal during shutdown: {ex.Message}");
        }

        Console.WriteLine($"[{pluginName}] Plugin Host shutting down.");
    }
}
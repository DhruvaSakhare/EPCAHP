using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Program
{
    static async Task Main(string[] args)
    {
        //var inputString = "Hello, world!";

        using (var client = new TcpClient())
        {
            await client.ConnectAsync("localhost", 7913);

            var stream = client.GetStream();

            // Wait for the server to send the "Hello" message back
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            var helloMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            Console.WriteLine(helloMessage);

            if (helloMessage != "Hello\n")
            {
                Console.WriteLine($"Error: Invalid response from server - {helloMessage}");
                return;
            }

            var inputString = Console.ReadLine();

            // Send the input string to the server
            var inputBytes = Encoding.ASCII.GetBytes(inputString + "\n\n");
            await stream.WriteAsync(inputBytes, 0, inputBytes.Length);

            // Wait for the server to send the UUID back
            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            var uuid = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            Console.WriteLine($"UUID: {uuid}");

            // Close the connection
            client.Close();
        }
    }

}

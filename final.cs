using System;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace FinalTest
{
    class Program
    {
        private static int uploadCount = 0;

        static void Main(string[] args)
        {
            // Start a thread to print upload count every 10 seconds
            Thread countThread = new Thread(PrintUploadCount);
            countThread.Start();

            // Set up web server
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://localhost:5000") // or any other URL
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        // Check if request is for /api/strings and is a POST
                        if (context.Request.Path == "/api/strings" && context.Request.Method == HttpMethods.Post)
                        {
                            // Read string from request body
                            using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                            {
                                string content = await reader.ReadToEndAsync();

                                // Connect to StupidFileStore server and send string
                                using (TcpClient client = new TcpClient("localhost", 7913))
                                using (NetworkStream stream = client.GetStream())
                                {
                                    byte[] hello = Encoding.ASCII.GetBytes("Hello\n");
                                    await stream.WriteAsync(hello, 0, hello.Length);

                                    

                                    // Read response from server
                                    byte[] responseBytes = new byte[hello.Length];
                                    await stream.ReadAsync(responseBytes, 0, responseBytes.Length);

                                    // Check if response is "Hello"
                                    string response = Encoding.ASCII.GetString(responseBytes);
                                    if (response != "Hello\n")
                                    {
                                        throw new Exception("Did not receive 'Hello' response from server.");
                                    }

                                    byte[] stringBytes = Encoding.ASCII.GetBytes(content + "\n\n");
                                    await stream.WriteAsync(stringBytes, 0, stringBytes.Length);

                                    // Read UUID from response
                                    byte[] uuidBytes = new byte[36];
                                    await stream.ReadAsync(uuidBytes, 0, uuidBytes.Length);

                                    string uuid = Encoding.ASCII.GetString(uuidBytes).Trim();

                                    // Return UUID to client
                                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                                    context.Response.ContentType = "text/plain";
                                    await context.Response.WriteAsync(uuid);
                                }

                                // Increment upload count
                                Interlocked.Increment(ref uploadCount);
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        }
                    });
                })
                .Build();

            host.Run();
        }

        private static void PrintUploadCount()
        {
            while (true)
            {
                Thread.Sleep(10000); // wait 10 seconds
                Console.WriteLine($"Uploaded {uploadCount} strings since last printout.");
                Interlocked.Exchange(ref uploadCount, 0);
            }
        }
    }
}

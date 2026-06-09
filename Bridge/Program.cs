using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Bridge
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Bridge COM/HTTP forwarder...");

            string targetHttpUrl = "http://localhost:5100/api/telemetry";
            using HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("X-Api-Key", "ZleHaslo");

            try
            {
                using TcpClient tcpClient = new TcpClient("127.0.0.1", 4000);
                using NetworkStream stream = tcpClient.GetStream();
                using StreamReader reader = new StreamReader(stream);

                Console.WriteLine("Connected to Wokwi on port 4000. Listening for sensor data...");

                while (true)
                {
                    string jsonPayload = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(jsonPayload)) continue;

                    Console.WriteLine($"[Bridge] Received: {jsonPayload}");

                    if (jsonPayload.Contains("status")) continue;

                    try
                    {
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await httpClient.PostAsync(targetHttpUrl, content);
                        Console.WriteLine($"[Bridge] Forwarded to Client. Status: {response.StatusCode}");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("[Bridge] Failed to send HTTP request. Client may not be running.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}. Ensure Wokwi is running.");
            }
        }
    }
}
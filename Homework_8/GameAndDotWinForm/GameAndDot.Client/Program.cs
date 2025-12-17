using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Models;
using System.Net.Sockets;
using System.Text.Json;

namespace GameAndDot.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var config = SettingsManager.GetInstance();
            using TcpClient client = new TcpClient();
            Console.Write("Введите имя: ");
            string? userName = Console.ReadLine();
            Console.WriteLine($"Добро пожаловать, {userName}");
            StreamReader? Reader = null;
            StreamWriter? Writer = null;

            try
            {
                client.Connect(config.HostAddress, config.PortNumber);
                Reader = new StreamReader(client.GetStream());
                Writer = new StreamWriter(client.GetStream());
                if (Writer is null || Reader is null) return;


                Task.Run(() => ReceiveMessageAsync(Reader));
                await SendMessageAsync(Writer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Writer?.Close();
            Reader?.Close();

            async Task SendMessageAsync(StreamWriter writer)
            {
                var connectMessege = new EventMessege
                {
                    Type = Shared.Enums.EventType.PlayerConnected,
                    Username = userName
                };
                await writer.WriteLineAsync(JsonSerializer.Serialize(connectMessege));
                await writer.FlushAsync();

                while (true)
                {
                    string? input = Console.ReadLine();
                    if (input.StartsWith("point "))
                    {
                        var part
                            = input.Split(' ');
                        var pointMes = new EventMessege
                        {
                            Type = EventType.PointPlaced,
                            Username = userName,
                            X = int.Parse(part[1]),
                            Y = int.Parse(part[2])
                        };
                        await writer.WriteLineAsync(JsonSerializer.Serialize(pointMes));
                        await writer.FlushAsync();
                    }
                }
            }
            async Task ReceiveMessageAsync(StreamReader reader)
            {
                while (true)
                {
                    string? json = await reader.ReadLineAsync();
                    var mes = JsonSerializer.Deserialize<EventMessege>(json);

                    switch(mes.Type)
                    {
                        case EventType.PlayerConnected:
                            Console.WriteLine($"{mes.Username} подключился");
                            break; 
                        case EventType.PointPlaced:
                            Console.WriteLine($"{mes.Username}: ({mes.X}, {mes.Y})");
                            break;
                    }
                }
            }

            // чтобы сообщение не накладывалось на новое
            void Print(string message)
            {
                if (OperatingSystem.IsWindows())
                {
                    var pos = Console.GetCursorPosition(); // получаем позицию курсора
                    int left = pos.Left;
                    int top = pos.Top;     
                    Console.MoveBufferArea(0, top, left, 1, 0, top + 1);
                    // стаивм курсор в начало строки
                    Console.SetCursorPosition(0, top);
                    Console.WriteLine(message);
                    Console.SetCursorPosition(left, top + 1);
                }
                else Console.WriteLine(message);
            }
        }
    }
}

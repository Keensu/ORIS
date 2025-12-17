using GameAndDot.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameAndDot.Shared.Models
{
    public class ClientObject
    {
        protected internal string Id { get; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = String.Empty;
        protected internal StreamWriter Writer { get; }
        protected internal StreamReader Reader { get; }
        public string Color { get; set; } = string.Empty;

        TcpClient client;
        ServerObject server; // объект сервера

        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            client = tcpClient;
            server = serverObject;
            // получаем NetworkStream
            var stream = client.GetStream();
            // создаем StreamReader
            Reader = new StreamReader(stream);
            // создаем StreamWriter
            Writer = new StreamWriter(stream);
            Color = GenerateRandomColor();
        }
        private string GenerateRandomColor()
        {
            Random random = new Random();
            return $"#{random.Next(0x1000000):X6}";
        }
        public async Task ProcessAsync()
        {
            try
            {
                while (true)
                {
                    var jsonRequest = await Reader.ReadLineAsync();
                    Console.WriteLine($"Сервер получил: {jsonRequest}");

                    var mesRequest = JsonSerializer.Deserialize<EventMessege>(jsonRequest);
                    if (mesRequest == null) continue;

                    switch (mesRequest.Type)
                    {
                        case Enums.EventType.PlayerConnected:

                            Username = mesRequest.Username;
                            var playerColors = new Dictionary<string,string>();
                            foreach (var client in server.Clients)
                            {
                                if (!string.IsNullOrEmpty(client.Color))
                                {
                                    playerColors[client.Username] = client.Color;
                                }
                            }

                            if (!string.IsNullOrEmpty(mesRequest.Color))
                                Color = mesRequest.Color;

                            Console.WriteLine($"{Username} вошел в чат, цвет: {Color}");

                            var mesResponse = new EventMessege()
                            {
                                Type = EventType.PlayerConnected,
                                Username = Username,
                                Id = Id,
                                Players = server.Clients.Select(c => c.Username).ToList(),
                                Points = server.points.Select(p => new PointData
                                {
                                    Username = p.Username,
                                    X = p.X,
                                    Y = p.Y,
                                    Color = p.Color
                                }).ToList(),
                                PlayerColors = playerColors
                            };
                            string jsonResponse = JsonSerializer.Serialize(mesResponse);
                            Console.WriteLine($"Сервер отправляет: {jsonResponse}");
                            await server.BroadcastMessageAllAsync(jsonResponse);
                            break;

                        case Enums.EventType.PointPlaced:
                            Console.WriteLine($"Сервер получил точку: {mesRequest.Username}, ({mesRequest.X},{mesRequest.Y}), цвет: {mesRequest.Color}");

                            server.points.Add(new PointData
                            {
                                Username = Username, 
                                X = mesRequest.X,
                                Y = mesRequest.Y,
                                Color = mesRequest.Color
                            });

                            Console.WriteLine($"Сервер пересылает точку всем...");
                            await server.BroadcastMessageAsync(jsonRequest, Id);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка: {e.Message}");
                Console.WriteLine($"Stack trace: {e.StackTrace}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(Username))
                {
                    var disconnectMes = new EventMessege
                    {
                        Type = EventType.PlayerDisconected,
                        Username = Username
                    };
                    string json = JsonSerializer.Serialize(disconnectMes);
                    await server.BroadcastMessageAsync(json, Id);
                }


                server.RemoveConnection(Id);
            }
        }

        protected internal void Close()
        {
            Writer.Close();
            Reader.Close();
            client.Close();
        }
    }
}

using GameAndDot.Shared;

namespace GameAndDot.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Запускаем сервер...");


            ServerObject server = new ServerObject();// создание
            await server.ListenAsync(); // запуск
        }
    }  
}

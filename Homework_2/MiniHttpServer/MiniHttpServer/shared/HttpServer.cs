using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.shared
{
    public class HttpServer
    {

        private HttpListener _listener = new();
        private SettingsModel _config;
        private bool _isRunning = false;

        public HttpServer(SettingsModel config) { _config = config; }


        public void Start()
        {

            _listener.Prefixes.Add($"http://{_config.Domain}:{_config.Port}/");
            _listener.Start();
            _isRunning = true;
            Console.WriteLine("Сервер запущен");
            Receive();
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            Console.WriteLine("Сервер остановил работу");
        }

        private void Receive()
        {
            if (_isRunning)
            {
                try
                {
                    _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
                }

                catch (Exception ex) 
                { 
                    Console.WriteLine(ex.Message);
                }
            }
            else {return; }
        }

        private async void ListenerCallback(IAsyncResult result)
        {
            if (_listener.IsListening)
            {
                if(!_isRunning) {return; }

                string responseText = string.Empty;


                try
                {
                    responseText = File.ReadAllText($"{_config.PublicDirectoryPath}/index.html");
                }

                catch (DirectoryNotFoundException dirEx)
                {
                    Console.WriteLine("Директория не найдена: " + dirEx.Message);
                }

                catch (FileNotFoundException filnfEx)
                {
                    Console.WriteLine("Файл не найден: " + filnfEx.Message);
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                
                var context = _listener.EndGetContext(result);
                var request = context.Request;
                var response = context.Response;

                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                // получаем поток ответа и пишем в него ответ
                response.ContentLength64 = buffer.Length;
                using Stream output = response.OutputStream;

                // отправляем данные
                await output.WriteAsync(buffer);
                await output.FlushAsync();
                Console.WriteLine("Запрос обработан");
                Receive();
            }
        }
    }
}

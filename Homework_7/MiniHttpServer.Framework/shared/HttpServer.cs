using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Handlers;
using MiniHttpServer.FrameWork.Core.Handlers;
using MiniHttpServer.Settings;
using System.Net;
using System.Net.Mime;

namespace MiniHttpServer.shared
{
    public class HttpServer
    {

        private HttpListener _listener = new();
        private SettingsModel _config;
        private bool _isRunning = false;

        public HttpServer(SettingsModel config) {
            _config = config;
            Config.Initialize(config);
        }

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

                var context = _listener.EndGetContext(result);

                Handler staticFilesHandler = new StaticFilesHandler();
                Handler endPointsHandler = new EndPointsHandler();
                staticFilesHandler.Successor = endPointsHandler;
                staticFilesHandler.HandleRequest(context);


                Console.WriteLine("Запрос обработан");
                Receive();
            }
        }
    }
}

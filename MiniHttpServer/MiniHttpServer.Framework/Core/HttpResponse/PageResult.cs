using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Core.HttpResponse
{
    public class PageResult : IHttpResult
    {
        private readonly string _pathTemplate;
        private readonly object _data;
        public PageResult(string pathTemplate, object data)
        {
            _pathTemplate = pathTemplate;
            _data = data;
        }
        public string Execute(HttpListenerContext context) 
        {
            //Todo
            // вызов с шаблонизатора
            // Json result
            // tests framework.unittetst
            // tests http server
            // logika endpointhandler
            return String.Empty;
        }
    }
}

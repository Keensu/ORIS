using MiniHttpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MiniTemplateEngine;

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
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.StatusCode = 200;
            var templateRenderer = new HtmlTemplateRenderer();

            return templateRenderer.RenderFromFile(_pathTemplate, _data);


            //Todo
            // tests framework.unittetst
            // tests http server
        }
    }
}

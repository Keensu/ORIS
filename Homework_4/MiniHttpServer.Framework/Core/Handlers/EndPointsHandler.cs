using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.Handlers
{
    class EndPointsHandler : Handler
    {
        public override async void HandleRequest(HttpListenerContext context)
        {

            if (true)
            {
                var request = context.Request;
                var endpointName = request.Url?.AbsolutePath.Split('/')
                    .Where(p => p != string.Empty && p != null).First();

                var assembly = Assembly.GetEntryAssembly();
                var endpoint = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<Endpoint>() != null)
                    .FirstOrDefault(end => IsCheckedNameEndpoint(end.Name, endpointName));

                if (endpoint == null) return; //hw

                var method = endpoint.GetMethods().Where(t => t.GetCustomAttributes(true)
                    .Any(attr => attr.GetType().Name.Equals($"Http{context.Request.HttpMethod}", StringComparison.OrdinalIgnoreCase))).FirstOrDefault();

                if (method == null) return; //hw

                var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();

                var postParams = new Dictionary<string, string>();
                foreach (var pair in body.Split('&'))
                {
                    var kv = pair.Split('=');
                    postParams[WebUtility.UrlDecode(kv[0])] = WebUtility.UrlDecode(kv[1]);
                }

                var parameters = method.GetParameters()
                                         .Select(p => postParams.ContainsKey(p.Name) ? postParams[p.Name] : null)
                                         .ToArray();

                bool isBaseEndpoint = endpoint.Assembly.GetTypes()
                                      .Any(t => typeof(EndpointBase)
                                      .IsAssignableFrom(t) && !t.IsAbstract);

                var instanceEndpoint = Activator.CreateInstance(endpoint);

                if (isBaseEndpoint)
                {
                    (instanceEndpoint as EndpointBase).SetContext(context);
                }

                var result = method.Invoke(Activator.CreateInstance(endpoint), parameters);

                if(result is string stringContent)
                {
                    await WriteResponseAsync(context.Response, stringContent);
                }

                else
                {
                    //hw
                    await WriteResponseAsync(context.Response, result.ToString() ?? "Sending data. Status(OK)");
                }
            }

            // передача запроса дальше по цепи при наличии в ней обработчиков

            else if (Successor != null)
            {
                Successor.HandleRequest(context);
            }
        }

        private bool IsCheckedNameEndpoint(string endpointName, string className) =>
            endpointName.Equals(className, StringComparison.OrdinalIgnoreCase) ||
            endpointName.Equals($"{className}Endpoint", StringComparison.OrdinalIgnoreCase);

        private static async Task WriteResponseAsync(HttpListenerResponse response, string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            // получаем поток ответа и пишем в него ответ
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            // отправляем данные
            await output.WriteAsync(buffer);
            await output.FlushAsync();

        }
   }
}

using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Attributes;
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
        public override void HandleRequest(HttpListenerContext context)
        {
            // некоторая обработка запроса

            if (true)
            {
                var request = context.Request;
                var endpointName = request.Url?.AbsolutePath.Split('/')
                    .Where(p => p != string.Empty || p != null).First();

                var assembly = Assembly.GetExecutingAssembly();
                var endpoint = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<Endpoint>() != null)
                    .FirstOrDefault(end => IsCheckedNameEndpoint(end.Name, endpointName));

                if (endpoint == null) return; //hw

                var method = endpoint.GetMethods().Where(t => t.GetCustomAttributes(true)
                    .Any(attr => attr.GetType().Name.Equals($"Http{context.Request.HttpMethod}", StringComparison.OrdinalIgnoreCase))).FirstOrDefault();

                if (method == null) return; //hw

                var ret = method.Invoke(Activator.CreateInstance(endpoint), null);

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
   }
}

using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.HttpResponse;
using MiniHttpServer.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.FrameWork.Core.Handlers
{
    class EndPointsHandler : Handler
    {
        public override async void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var endpointName = request.Url.AbsolutePath.Split('/')[^2];

            var assembly = Assembly.GetEntryAssembly();
            var endpoint = assembly.GetTypes()
                                   .FirstOrDefault(t => t.GetCustomAttribute<EndpointAttribute>() != null &&
                                                        IsCheckedNameEndpoint(t.Name, endpointName));
            if (endpoint == null) return;

            var method = endpoint.GetMethods()
                                 .FirstOrDefault(m => m.GetCustomAttributes(true)
                                                        .Any(attr => attr.GetType().Name.StartsWith($"Http{request.HttpMethod}", StringComparison.OrdinalIgnoreCase)) &&
                                                    IsMethodForCurrentPath(m, request.Url.AbsolutePath));

            if (method == null)
            {
                method = endpoint.GetMethods()
                                 .FirstOrDefault(m => m.GetCustomAttributes(true)
                                                        .Any(attr => attr.GetType().Name.Equals($"Http{request.HttpMethod}", StringComparison.OrdinalIgnoreCase)));
            }

            if (method == null) return;

            string body;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }

            var postParams = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(body))
            {
                foreach (var pair in body.Split('&'))
                {
                    if (string.IsNullOrEmpty(pair)) continue;
                    var kv = pair.Split('=', 2);
                    if (kv.Length == 2)
                        postParams[WebUtility.UrlDecode(kv[0])] = WebUtility.UrlDecode(kv[1]);
                }
            }

            var parameters = method.GetParameters()
                                   .Select(p => postParams.ContainsKey(p.Name) ? postParams[p.Name] : null)
                                   .ToArray();

            var instance = Activator.CreateInstance(endpoint);
            if (typeof(EndpointBase).IsAssignableFrom(endpoint))
            {
                (instance as EndpointBase)?.SetContext(context);
            }

            var ret = method.Invoke(instance, parameters);

            if (ret is IHttpResult httpResult)
            {
                await WriteResponseAsync(context.Response, httpResult.Execute(context));
            }
            else if (ret is string strResult)
            {
                await WriteResponseAsync(context.Response, strResult);
            }
            else if (ret != null)
            {
                await WriteResponseAsync(context.Response, ret.ToString());
            }
            else
            {
                context.Response.StatusCode = 200;
                await WriteResponseAsync(context.Response, "OK");
            }
        }

        private bool IsCheckedNameEndpoint(string endpointName, string className) =>
            endpointName.Equals(className, StringComparison.OrdinalIgnoreCase) ||
            endpointName.Equals($"{className}EndPoint", StringComparison.OrdinalIgnoreCase);

        private bool IsMethodForCurrentPath(MethodInfo method, string currentPath)
        {
            var segments = currentPath.Split('/');
            var httpAttr = method.GetCustomAttributes().FirstOrDefault(attr => attr.GetType().Name.StartsWith("Http"));
            if (httpAttr == null) return false;

            var routeProp = httpAttr.GetType().GetProperty("Route");
            if (routeProp != null)
            {
                var route = routeProp.GetValue(httpAttr) as string;
                if (!string.IsNullOrEmpty(route))
                    return segments[^1].Equals(route, StringComparison.OrdinalIgnoreCase);
            }

            var methodName = method.Name.ToLower();
            var expected = methodName.Replace("get", "").Replace("post", "");
            return segments[^1].Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task WriteResponseAsync(HttpListenerResponse response, string content)
        {
            var buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            await using var output = response.OutputStream;
            await output.WriteAsync(buffer);
            await output.FlushAsync();
        }
    }
}

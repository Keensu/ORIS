using MiniHttpServer.Core.Abstracts;
using MiniHttpServer.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.Handlers
{
    class StaticFilesHandler : Handler
    {
        public async override void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var isGetMethod = context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase);
            var isStaticFile = request.Url.AbsolutePath.Split('/').Any(x=> x.Contains("."));

            if (isGetMethod && isStaticFile)
            {
                
                var response = context.Response;
                byte[] responseFile = null;
                try
                {
                    string filePath = $"{Config.Instance.Settings.PublicDirectoryPath}{request.Url.AbsolutePath}";
                    if (request.Url.AbsolutePath[^1] == '/')
                    {
                        filePath += "index.html";
                    }


                    responseFile = File.ReadAllBytes(filePath);
                    response.ContentType = GetContentType.Invoke(filePath);
                    response.ContentLength64 = responseFile.Length;

                }

                catch (DirectoryNotFoundException dirEx)
                {
                    Console.WriteLine("Директория не найдена: " + dirEx.Message);
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }

                catch (FileNotFoundException filnfEx)
                {
                    Console.WriteLine("Файл не найден: " + filnfEx.Message);

                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }


                using Stream output = response.OutputStream;

                // отправляем данные
                await output.WriteAsync(responseFile);
                await output.FlushAsync();
            }
            // передача запроса дальше по цепи при наличии в ней обработчиков
            else if (Successor != null)
            {
                Successor.HandleRequest(context);
            }
        }
    }
}

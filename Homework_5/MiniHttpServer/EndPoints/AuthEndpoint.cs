using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Services;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.HttpResponse;

namespace MiniHttpServer.EndPoints
{
    [Endpoint]
    internal class AuthEndpoint : EndpointBase
    {
        private readonly EmailService emailService = new EmailService();
        //get /auth/
        [HttpGet]
        public string LoginPage()
        {
            Context.Response.Cookies.Clear();


            return "hui";
        }
        //post /auth/
        [HttpPost ("/auth")]
        public async Task Login(string email, string password) 
        {
            Console.WriteLine("PIVO");
            await emailService.SendEmailAsync(email, "Успешная авторизация", password);
        }

        // post /auth/sendEmail
        [HttpPost("sendEmail")]
        public void SendEmail(string to, string title, string message)
        {
            
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Services;

namespace MiniHttpServer.EndPoints
{
    [Endpoint]
    internal class AuthEndpoint
    {
        //get /auth/
        [HttpGet]
        public string LoginPage()
        {
            return "index.html";
        }
        //post /auth/
        [HttpPost]
        public void Login(string email, string password) {
            //send email and password
            //EmailService.SendEmail(email, title, message);
        }

        // post /auth/sendEmail
        [HttpPost("sendEmail")]
        public void SendEmail(string to, string title, string message)
        {
            
        }
    }
}

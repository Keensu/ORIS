using MiniHttpServer.Settings.EmailSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Services
{
    public class EmailService
    {
        private readonly List<SmtpSettings> _smtpList;

        public EmailService()
        {
            _smtpList = new List<SmtpSettings>
            {
                new SmtpSettings
                {
                    Name = "Gmail",
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    Username = "work3do1send@gmail.com",
                    Password = "ehnw tfdv ygtf ukel"
                },

                new SmtpSettings
                {
                    Name = "Mail",
                    Host = "smtp.mail.ru",
                    Port = 587,
                    EnableSsl = true,
                    Username = "work2do1send@mail.ru",
                    Password = "mcs33i2Hhosd5i4VFNh1"
                }


            };
        }

        public async Task SendEmailAsync(string to, string title, string password)
        {
            foreach (var smtpSettings in _smtpList)
            {
                try
                {
                    MailAddress from = new MailAddress(smtpSettings.Username, "Плотников Ярослав Алексеевич 11-409");
                    MailAddress recepient = new MailAddress(to);

                    using (MailMessage mes = new MailMessage(from, recepient))
                    {
                        mes.Subject = title;
                        mes.Body = $@"
                        <html>
                            <body style='font-family: Arial, sans-serif; color: #333;'>
                                <h2 style='color:#2e6c80;'>Здравствуйте!</h2>
                                <p>Авторизация выполнена.</p>
                                <p>Ваши данные для входа:</p>
                                <ul>
                                    <li><b>Логин:</b> {to}</li>
                                    <li><b>Пароль:</b> {password}</li>
                                </ul>
                            </body>
                        </html>";

                        mes.IsBodyHtml = true;

                        var path = Path.Combine(Directory.GetCurrentDirectory(), "MiniHttpServer.zip");
                        mes.Attachments.Add(new Attachment(path));

                        SmtpClient smtp = new SmtpClient(smtpSettings.Host, smtpSettings.Port);
                            smtp.Credentials = new NetworkCredential(smtpSettings.Username, smtpSettings.Password);
                            smtp.EnableSsl = smtpSettings.EnableSsl;

                            await smtp.SendMailAsync(mes);
                            Console.WriteLine($" Письмо отправлено через {smtpSettings.Name}");
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"s Ошибка: {smtpSettings.Name}: {ex.Message}");
                }
            }
        }
    }
}

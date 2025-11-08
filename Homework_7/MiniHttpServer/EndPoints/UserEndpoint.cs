using MiniHttpServer.Core.Attributes;
using MiniHttpServer.Framework.Core;
using MiniHttpServer.Framework.Core.Attributes;
using MiniHttpServer.Framework.Core.HttpResponse;
using MyORMLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MiniHttpServer.Settings;

namespace MiniHttpServer.EndPoints
{
    internal class UserEndpoint : EndpointBase
    {
        private readonly string _connectionString;

        public UserEndpoint(string connectionString)
        {
            _connectionString = connectionString;
        }

        [HttpGet("users")]
        public IHttpResult GetUsers()
        {
            ORMContext context = new ORMContext(_connectionString);
            string sqlExpression = "SELECT * FROM users";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows) // если есть данные
                {
                    // выводим названия столбцов
                    Console.WriteLine("{0}\t{1}\t{2}\t{3}", reader.GetName(0), reader.GetName(1), reader.GetName(2), reader.GetName(3));

                    while (reader.Read()) // построчно считываем данные
                    {
                        object id = reader.GetValue(0);
                        object username = reader.GetValue(1);
                        object email = reader.GetValue(2);
                        object password = reader.GetValue(3);

                        Console.WriteLine("{0} \t{1} \t{2}\t{3}", id, username, email, password);
                    }
                }

                reader.Close();
            }

            Console.Read();

            return null;
        }

        [HttpPost("users")]
        public IHttpResult PostUsers(User user)
        {
            ORMContext context = new ORMContext(_connectionString);

            context.Create(user);

            return null; 
        }

        [HttpDelete("users")]
        public IHttpResult DeleteUsers(int id)
        {
            ORMContext context = new ORMContext(_connectionString);

            context.Delete(id);

            return null; 
        }

        [HttpPut("users")]
        public IHttpResult UpdateUsers(User user, string newPassword)
        {
            ORMContext context = new ORMContext(_connectionString);

            context.Update(user.Id, newPassword);

            return null; 
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}

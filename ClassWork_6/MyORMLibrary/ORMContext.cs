using System.Data.SqlClient;

namespace MyORMLibrary
{
    public class ORMContext
    {
        private readonly string _connectionString;

        public ORMContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Create<T>(T entity) where T : class
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // рефлексия
                var properties = typeof(T).GetProperties();
                var columnNames = string.Join(", ", properties.Select(p => p.Name));
                var paramNames = string.Join(", ", properties.Select(p => "@" + p.Name));

                string sql = $"INSERT INTO users ({columnNames}) VALUES ({paramNames})";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(entity) ?? DBNull.Value;
                        command.Parameters.AddWithValue("@" + prop.Name, value);
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        public T ReadById<T>(int id) where T : class, new() 
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string sql = $"SELECT * FROM users WHERE Id = @id";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            T entity = new T();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var prop = typeof(T).GetProperty(reader.GetName(i));
                                if (prop != null && !reader.IsDBNull(i))
                                {
                                    prop.SetValue(entity, reader.GetValue(i));
                                }
                            }
                            return entity;
                        }
                    }
                }
            }
            return null;
        }

        public List<T> ReadByAll<T>() where T : class, new()
        {
            var list = new List<T>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string sql = $"SELECT * FROM users";
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        T entity = new T();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var prop = typeof(T).GetProperty(reader.GetName(i));
                            if (prop != null && !reader.IsDBNull(i))
                            {
                                prop.SetValue(entity, reader.GetValue(i));
                            }
                        }
                        list.Add(entity);
                    }
                }
            }
            return list;
        }

        public void Update<T>(int id, T entity) where T : class
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var properties = typeof(T).GetProperties().Where(p => p.Name.ToLower() != "id").ToList();

                var setClause = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));

                string sql = $"UPDATE users SET {setClause} WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(entity) ?? DBNull.Value;
                        command.Parameters.AddWithValue("@" + prop.Name, value);
                    }

                    command.Parameters.AddWithValue("@Id", id);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = $"DELETE FROM users WHERE Id = @id";
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);


                command.ExecuteNonQuery();
            }
        }
    }
}

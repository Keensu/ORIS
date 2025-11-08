using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Reflection.PortableExecutable;

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
        public IEnumerable<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            var (sql, parameters) = BuildSqlQuery(predicate, false);
            return ExecuteQueryMultiple<T>(sql, parameters);
        }

        public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class, new()
        {
            var (sql, parameters) = BuildSqlQuery(predicate, true);
            return ExecuteQuerySingle<T>(sql, parameters);
        }

        private (string Sql, List<SqlParameter> Parameters) BuildSqlQuery<T>(
            Expression<Func<T, bool>> predicate, bool singleResult)
        {
            var parameters = new List<SqlParameter>();
            string tableName = "users";
            string whereClause = ParseExpression(predicate.Body, parameters);
            string limit = singleResult ? "LIMIT 1" : "";
            string sql = $"SELECT * FROM \"{tableName}\" WHERE {whereClause} {limit}".Trim();
            return (sql, parameters);
        }

        private string ParseExpression(Expression expression, List<SqlParameter> parameters)
        {
            switch (expression)
            {
                case BinaryExpression binary:
                    string left = ParseExpression(binary.Left, parameters);
                    string right = ParseExpression(binary.Right, parameters);

                    if (right == "NULL" && binary.NodeType == ExpressionType.Equal)
                        return $"({left} IS NULL)";
                    if (right == "NULL" && binary.NodeType == ExpressionType.NotEqual)
                        return $"({left} IS NOT NULL)";

                    string op = GetSqlOperator(binary.NodeType);
                    return $"({left} {op} {right})";

                case MemberExpression member when member.Expression is ParameterExpression:
                    // Это поле сущности: x => x.Name
                    return $"\"{member.Member.Name.ToLowerInvariant()}\"";

                case MemberExpression member:
                    // Это захваченная переменная или константа — вычисляем её значение
                    object? value = EvaluateExpression(member);
                    var param = CreateParameter(value, parameters);
                    return param.ParameterName;

                case ConstantExpression constant:
                    var p = CreateParameter(constant.Value, parameters);
                    return p.ParameterName;

                case UnaryExpression unary when unary.NodeType == ExpressionType.Not:
                    return $"(NOT {ParseExpression(unary.Operand, parameters)})";

                case MethodCallExpression method:
                    return ParseMethodCall(method, parameters);

                default:
                    throw new NotSupportedException($"Unsupported expression: {expression.NodeType}");
            }
        }

        private string ParseMethodCall(MethodCallExpression method, List<SqlParameter> parameters)
        {
            if (method.Method.DeclaringType == typeof(string))
            {
                string member = ParseExpression(method.Object!, parameters);
                string argument = ParseExpression(method.Arguments[0], parameters);

                return method.Method.Name switch
                {
                    nameof(string.Contains) => $"({member} ILIKE '%' || {argument} || '%')",
                    nameof(string.StartsWith) => $"({member} ILIKE {argument} || '%')",
                    nameof(string.EndsWith) => $"({member} ILIKE '%' || {argument})",
                    _ => throw new NotSupportedException($"Unsupported string method: {method.Method.Name}")
                };
            }

            // Enumerable.Contains
            if (method.Method.Name == "Contains" &&
                method.Method.DeclaringType != typeof(string) &&
                method.Arguments.Count == 1 &&
                method.Object != null) // x.Contains(item
            {
                var collection = EvaluateExpression(method.Object);
                var itemExpr = method.Arguments[0];
                string column = ParseExpression(itemExpr, parameters);

                var values = new List<string>();
                foreach (var item in (System.Collections.IEnumerable)collection)
                {
                    var p = CreateParameter(item, parameters);
                    values.Add(p.ParameterName);
                }

                string valueList = string.Join(", ", values);
                return $"({column} IN ({valueList}))";
            }

            // Enumerable.Contains (item.Contains(collection))
            if (method.Method.Name == "Contains" &&
                method.Method.DeclaringType != typeof(string) &&
                method.Arguments.Count == 2) // static method call with 2 arguments
            {
                var collection = EvaluateExpression(method.Arguments[0]);
                var itemExpr = method.Arguments[1];
                string column = ParseExpression(itemExpr, parameters);

                var values = new List<string>();
                foreach (var item in (System.Collections.IEnumerable)collection)
                {
                    var p = CreateParameter(item, parameters);
                    values.Add(p.ParameterName);
                }

                string valueList = string.Join(", ", values);
                return $"({column} IN ({valueList}))";
            }

            throw new NotSupportedException($"Unsupported method: {method.Method.Name}");
        }

        // Безопасная оценка любого подвыражения (включая замыкания)
        private object? EvaluateExpression(Expression expr)
        {
            var objectExpr = Expression.Convert(expr, typeof(object));
            var lambda = Expression.Lambda<Func<object>>(objectExpr);
            var func = lambda.Compile();
            return func();
        }

        private SqlParameter CreateParameter(object? value, List<SqlParameter> parameters)
        {
            string paramName = $"@p{parameters.Count}";
            var param = new SqlParameter(paramName, value ?? DBNull.Value);
            parameters.Add(param);
            return param;
        }

        private string GetSqlOperator(ExpressionType nodeType) => nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Unsupported operator: {nodeType}")
        };

        private T ExecuteQuerySingle<T>(string sql, List<SqlParameter> parameters) where T : class, new()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return MapToEntity<T>(reader);
            return null;
        }

        private IEnumerable<T> ExecuteQueryMultiple<T>(string sql, List<SqlParameter> parameters) where T : class, new()
        {
            var list = new List<T>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapToEntity<T>(reader));
            return list;
        }

        private T MapToEntity<T>(SqlDataReader reader) where T : new ()
        {
            var entity = new T();
        var props = typeof(T).GetProperties()
            .ToDictionary(p => p.Name.ToLowerInvariant(), p => p);

            for (int i = 0; i<reader.FieldCount; i++)
            {
                string colName = reader.GetName(i).ToLowerInvariant();
                if (!props.TryGetValue(colName, out var prop))
                    continue;

                if (reader.IsDBNull(i))
                {
                    if (prop.PropertyType.IsClass || Nullable.GetUnderlyingType(prop.PropertyType) != null)
                        prop.SetValue(entity, null);
                }
                else
                {
                    prop.SetValue(entity, Convert.ChangeType(reader.GetValue(i), Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType));

                }
            }

            return entity;
        }

    }
}

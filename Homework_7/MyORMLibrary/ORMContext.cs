using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Npgsql;

public class ORMContext
{
    private readonly string _connectionString;

    public ORMContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    private string GetTableName<T>() => $"\"{typeof(T).Name.ToLower()}s\"";

    public void Create<T>(T entity) where T : class
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        var properties = typeof(T).GetProperties().Where(p => p.Name.ToLower() != "id").ToList();
        var columnNames = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
        var paramNames = string.Join(", ", properties.Select((p, i) => $"@p{i}"));

        string sql = $"INSERT INTO {GetTableName<T>()} ({columnNames}) VALUES ({paramNames})";

        using var cmd = new NpgsqlCommand(sql, conn);
        for (int i = 0; i < properties.Count; i++)
        {
            var value = properties[i].GetValue(entity) ?? DBNull.Value;
            cmd.Parameters.AddWithValue($"@p{i}", value);
        }

        cmd.ExecuteNonQuery();
    }

    public T? ReadById<T>(int id) where T : class, new()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string sql = $"SELECT * FROM {GetTableName<T>()} WHERE \"id\" = @id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return MapToEntity<T>(reader);

        return null;
    }

    public List<T> ReadByAll<T>() where T : class, new()
    {
        var list = new List<T>();
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string sql = $"SELECT * FROM {GetTableName<T>()}";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(MapToEntity<T>(reader));

        return list;
    }

    public void Update<T>(int id, T entity) where T : class
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        var properties = typeof(T).GetProperties().Where(p => p.Name.ToLower() != "id").ToList();
        var setClause = string.Join(", ", properties.Select((p, i) => $"\"{p.Name}\" = @p{i}"));

        string sql = $"UPDATE {GetTableName<T>()} SET {setClause} WHERE \"id\" = @id";
        using var cmd = new NpgsqlCommand(sql, conn);

        for (int i = 0; i < properties.Count; i++)
        {
            var value = properties[i].GetValue(entity) ?? DBNull.Value;
            cmd.Parameters.AddWithValue($"@p{i}", value);
        }

        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void Delete<T>(int id)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();

        string sql = $"DELETE FROM {GetTableName<T>()} WHERE \"id\" = @id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
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

    private (string Sql, List<NpgsqlParameter> Parameters) BuildSqlQuery<T>(
        Expression<Func<T, bool>> predicate, bool singleResult)
    {
        var parameters = new List<NpgsqlParameter>();
        string whereClause = ParseExpression(predicate.Body, parameters);
        string limit = singleResult ? "LIMIT 1" : "";
        string sql = $"SELECT * FROM {GetTableName<T>()} WHERE {whereClause} {limit}".Trim();
        return (sql, parameters);
    }

    private string ParseExpression(Expression expression, List<NpgsqlParameter> parameters)
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
                return $"\"{member.Member.Name}\"";

            case MemberExpression member:
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

    private string ParseMethodCall(MethodCallExpression method, List<NpgsqlParameter> parameters)
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

        if (method.Method.Name == "Contains")
        {
            var collection = EvaluateExpression(method.Object ?? method.Arguments[0]);
            var itemExpr = method.Object != null ? method.Arguments[0] : method.Arguments[1];
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

    private object? EvaluateExpression(Expression expr)
    {
        var objectExpr = Expression.Convert(expr, typeof(object));
        var lambda = Expression.Lambda<Func<object>>(objectExpr);
        return lambda.Compile()();
    }

    private NpgsqlParameter CreateParameter(object? value, List<NpgsqlParameter> parameters)
    {
        string paramName = $"@p{parameters.Count}";
        var param = new NpgsqlParameter(paramName, value ?? DBNull.Value);
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

    private T? ExecuteQuerySingle<T>(string sql, List<NpgsqlParameter> parameters) where T : class, new()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters.ToArray());
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return MapToEntity<T>(reader);
        return null;
    }

    private IEnumerable<T> ExecuteQueryMultiple<T>(string sql, List<NpgsqlParameter> parameters) where T : class, new()
    {
        var list = new List<T>();
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters.ToArray());
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(MapToEntity<T>(reader));
        return list;
    }

    private T MapToEntity<T>(NpgsqlDataReader reader) where T : new()
    {
        var entity = new T();
        var props = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p);

        for (int i = 0; i < reader.FieldCount; i++)
        {
            string colName = reader.GetName(i);
            if (!props.TryGetValue(colName, out var prop))
                continue;

            if (reader.IsDBNull(i))
                prop.SetValue(entity, null);
            else
                prop.SetValue(entity, Convert.ChangeType(reader.GetValue(i), Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType));
        }

        return entity;
    }
}


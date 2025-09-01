using Dapper;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DiscountServer.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    /// <summary>
    /// Attempts to insert a collection of codes and returns the number of rows actually inserted.
    /// Uses "INSERT IGNORE" to automatically skip any codes that already exist in the database.
    /// </summary>
    /// <param name="codes">The collection of codes to insert.</param>
    /// <returns>The count of newly inserted codes.</returns>
    public async Task<int> InsertNewCodesAsync(IEnumerable<string> codes)
    {
        using var connection = CreateConnection();
        const string sql = "INSERT IGNORE INTO DiscountCodes (Code, IsUsed) VALUES (@Code, FALSE);";
        var codesToInsert = codes.Select(c => new { Code = c });

        // Dapper's ExecuteAsync returns the number of rows affected, which is exactly what we need.
        return await connection.ExecuteAsync(sql, codesToInsert);
    }

    public async Task<(bool Exists, bool IsUsed)> GetCodeStatusAsync(string code)
    {
        using var connection = CreateConnection();
        const string sql = "SELECT IsUsed FROM DiscountCodes WHERE Code = @Code";
        var result = await connection.QuerySingleOrDefaultAsync<bool?>(sql, new { Code = code });

        if (result == null)
        {
            return (false, false); // Doesn't exist
        }
        return (true, result.Value); // Exists, and its IsUsed status
    }

    public async Task<bool> MarkCodeAsUsedAsync(string code)
    {
        using var connection = CreateConnection();
        const string sql = "UPDATE DiscountCodes SET IsUsed = TRUE WHERE Code = @Code AND IsUsed = FALSE";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Code = code });
        return rowsAffected > 0;
    }
}

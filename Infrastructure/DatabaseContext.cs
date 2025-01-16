using System;
using System.Collections.Generic;
using System.Data.OleDb;
using NLPv2.Models;

namespace NLPv2.Infrastructure
{
    public interface IDatabaseContext
    {
        List<SwiftData> GetAllSwiftData(string tableName = "SwiftData");
    }

    public class DatabaseContext : IDatabaseContext
    {
        private readonly string _connectionString;

        public DatabaseContext(string databasePath)
        {
            _connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};Persist Security Info=False;";
        }

        public List<SwiftData> GetAllSwiftData(string tableName = "SwiftData")
        {
            var result = new List<SwiftData>();

            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();
                string query = $"SELECT SWIFT, Category, Language FROM {tableName}";

                using (var command = new OleDbCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = new SwiftData
                        {
                            SWIFT = reader["SWIFT"].ToString(),
                            Category = Convert.ToInt32(reader["Category"]),
                            Language = Convert.ToInt32(reader["Language"])
                        };
                        result.Add(data);
                    }
                }
            }

            return result;
        }
    }
}

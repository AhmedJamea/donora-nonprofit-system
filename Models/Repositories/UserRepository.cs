using Donora.Models.Entities;
using Microsoft.Data.SqlClient;

namespace Donora.Models.Repositories
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public AppUser? Authenticate(string email, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(SqlQueries.GetUserByEmail, conn);
            cmd.Parameters.AddWithValue("@email", email);

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                string storedHash = reader["password_hash"].ToString()!;

                // Note: Always use a proper hashing library (like BCrypt) in production!
                if (password == storedHash)
                {
                    return MapUser(reader);
                }
            }
            return null;
        }

        public bool Register(AppUser user)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(SqlQueries.InsertUser, conn);

            cmd.Parameters.AddWithValue("@name", user.FullName);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@pass", user.PasswordHash);
            cmd.Parameters.AddWithValue("@role", user.Role);

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        #region Helpers

        private AppUser MapUser(SqlDataReader r) => new AppUser
        {
            UserId = (int)r["user_id"],
            FullName = r["full_name"].ToString()!,
            Role = r["role"].ToString()!
        };

        #endregion

        private static class SqlQueries
        {
            public const string GetUserByEmail = "SELECT user_id, full_name, password_hash, role FROM AppUser WHERE email = @email";

            public const string InsertUser = "INSERT INTO AppUser (full_name, email, password_hash, role) VALUES (@name, @email, @pass, @role)";
        }
    }
}
using Donora.Models.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Donora.Models.Repositories
{
    public class ContributionRepository
    {
        private readonly string _connectionString;

        public ContributionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool AddContribution(Contribution contribution)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Transaction ensures the record is logged AND the initiative total is updated
            using var trans = conn.BeginTransaction();
            try
            {
                // 1. Insert Contribution
                using (var cmd = new SqlCommand(SqlQueries.InsertContribution, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@uid", contribution.SupporterId);
                    cmd.Parameters.AddWithValue("@iid", contribution.InitiativeId);
                    cmd.Parameters.AddWithValue("@amount", contribution.Amount);
                    cmd.ExecuteNonQuery();
                }

                // 2. Update Initiative Total
                using (var cmd = new SqlCommand(SqlQueries.UpdateInitiativeTotal, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@amount", contribution.Amount);
                    cmd.Parameters.AddWithValue("@iid", contribution.InitiativeId);
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
                return true;
            }
            catch
            {
                trans.Rollback();
                return false;
            }
        }

        public List<dynamic> GetHistoryBySupporter(int userId)
        {
            // Specify <dynamic> here so the helper creates a List<dynamic>
            return QueryList<dynamic>(SqlQueries.GetHistory, r => new
            {
                Amount = (decimal)r["amount"],
                Date = (DateTime)r["timestamp"],
                ProjectName = r["initiative_name"].ToString()
            }, p => p.AddWithValue("@uid", userId));
        }

        #region Helpers

        private List<T> QueryList<T>(string sql, Func<SqlDataReader, T> mapper, Action<SqlParameterCollection>? addParams = null)
        {
            var list = new List<T>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            addParams?.Invoke(cmd.Parameters);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(mapper(reader));

            return list;
        }

        #endregion

        private static class SqlQueries
        {
            public const string InsertContribution = "INSERT INTO Contribution (user_id, initiative_id, amount) VALUES (@uid, @iid, @amount)";

            public const string UpdateInitiativeTotal = "UPDATE Initiative SET current_raised = current_raised + @amount WHERE initiative_id = @iid";

            public const string GetHistory = @"
                SELECT c.amount, c.timestamp, i.initiative_name 
                FROM Contribution c
                JOIN Initiative i ON c.initiative_id = i.initiative_id
                WHERE c.user_id = @uid
                ORDER BY c.timestamp DESC";
        }
    }
}
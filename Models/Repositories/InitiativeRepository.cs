using Donora.Models.Entities;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Donora.Models.Repositories
{
    public class InitiativeRepository
    {
        private readonly string _connectionString;

        public InitiativeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Public CRUD Methods

        public List<Sector> GetAllSectors()
        {
            return QueryList(SqlQueries.GetAllSectors, r => new Sector
            {
                SectorId = (int)r["sector_id"],
                Name = r["name"].ToString()!
            });
        }

        public bool CreateInitiative(Initiative initiative)
        {
            return ExecuteCommand(SqlQueries.CreateInitiative, p => {
                p.AddWithValue("@sid", initiative.SectorId);
                p.AddWithValue("@uid", initiative.CreatedByUserId);
                p.AddWithValue("@name", initiative.InitiativeName);
                p.AddWithValue("@obj", (object)initiative.Objective ?? DBNull.Value);
                p.AddWithValue("@target", initiative.FundingTarget);
                p.AddWithValue("@start", initiative.StartDate);
                p.AddWithValue("@end", initiative.EndDate);
            }) > 0;
        }

        public bool UpdateInitiative(Initiative initiative)
        {
            return ExecuteCommand(SqlQueries.UpdateInitiative, p => {
                p.AddWithValue("@id", initiative.InitiativeId);
                p.AddWithValue("@sid", initiative.SectorId);
                p.AddWithValue("@name", initiative.InitiativeName);
                p.AddWithValue("@obj", (object)initiative.Objective ?? DBNull.Value);
                p.AddWithValue("@target", initiative.FundingTarget);
                p.AddWithValue("@start", initiative.StartDate);
                p.AddWithValue("@end", initiative.EndDate);
            }) > 0;
        }

        public Initiative? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(SqlQueries.GetById, conn);
            cmd.Parameters.AddWithValue("@id", id);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapInitiative(reader) : null;
        }

        public List<Initiative> GetByCreatorId(int userId)
        {
            return QueryList(SqlQueries.GetByCreator, r => MapInitiative(r), p => p.AddWithValue("@uid", userId));
        }

        public bool DeleteInitiative(int initiativeId, int userId)
        {
            return ExecuteCommand(SqlQueries.DeleteInitiative, p => {
                p.AddWithValue("@id", initiativeId);
                p.AddWithValue("@uid", userId);
            }) > 0;
        }

        public bool LogExpenditure(Expenditure exp)
        {
            return ExecuteCommand(SqlQueries.LogExpenditure, p => {
                p.AddWithValue("@iid", exp.InitiativeId);
                p.AddWithValue("@amount", exp.AmountSpent);
                p.AddWithValue("@vendor", exp.VendorName);
                p.AddWithValue("@date", exp.DateSpent);
            }) > 0;
        }

        public decimal GetTotalSpent(int initiativeId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(SqlQueries.GetTotalSpent, conn);
            cmd.Parameters.AddWithValue("@iid", initiativeId);
            conn.Open();
            return (decimal)(cmd.ExecuteScalar() ?? 0m);
        }

        public List<Initiative> GetAllInitiativesWithProgress()
        {
            return QueryList(SqlQueries.GetAllWithProgress, r => MapInitiative(r));
        }

        #endregion

        #region Analytics & Dashboard

        // REFACTOR: Method now accepts adminId to filter data
        public dynamic GetDashboardStats(int adminId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(SqlQueries.DashboardStats, conn);
            // REFACTOR: Passing parameter to the SQL Command
            cmd.Parameters.AddWithValue("@adminId", adminId);
            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new
                {
                    TotalFunds = (decimal)reader["TotalFunds"],
                    ActiveCount = (int)reader["ActiveCount"],
                    LowFundingCount = (int)reader["LowFundingCount"]
                };
            }
            return new { TotalFunds = 0m, ActiveCount = 0, LowFundingCount = 0 };
        }

        public dynamic GetGlobalReports()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            return new
            {
                TopSector = ExecuteScalar(SqlQueries.ReportTopSector, conn)?.ToString() ?? "No Data",
                SlowInitiatives = QueryList(SqlQueries.ReportSlowInitiatives, r => r[0].ToString()!, null, conn),
                TopSupporter = ExecuteScalar(SqlQueries.ReportTopSupporter, conn)?.ToString() ?? "No Data",
                NoSpendInitiatives = QueryList(SqlQueries.ReportNoSpend, r => r[0].ToString()!, null, conn),
                ActiveLastMonth = QueryList(SqlQueries.ReportActiveLastMonth, r => new { Sector = r["Sector"].ToString(), Name = r["initiative_name"].ToString() }, null, conn),
                SupporterStats = QueryList(SqlQueries.ReportSupporterStats, r => new { User = r["full_name"].ToString(), Count = (int)r["ProjectsCount"] }, null, conn)
            };
        }

        #endregion

        #region Private Helpers

        private Initiative MapInitiative(SqlDataReader r) => new Initiative
        {
            InitiativeId = (int)r["initiative_id"],
            SectorId = (int)r["sector_id"],
            InitiativeName = r["initiative_name"].ToString()!,
            Objective = r["objective"]?.ToString(),
            FundingTarget = (decimal)r["funding_target"],
            StartDate = (DateTime)r["start_date"],
            EndDate = (DateTime)r["end_date"],
            CurrentRaised = r["current_raised"] != DBNull.Value ? Convert.ToDecimal(r["current_raised"]) : 0m
        };

        private int ExecuteCommand(string sql, Action<SqlParameterCollection> addParams)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            addParams(cmd.Parameters);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        private object? ExecuteScalar(string sql, SqlConnection conn)
        {
            using var cmd = new SqlCommand(sql, conn);
            return cmd.ExecuteScalar();
        }

        private List<T> QueryList<T>(string sql, Func<SqlDataReader, T> mapper, Action<SqlParameterCollection>? addParams = null, SqlConnection? existingConn = null)
        {
            var list = new List<T>();
            var conn = existingConn ?? new SqlConnection(_connectionString);
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                using var cmd = new SqlCommand(sql, conn);
                addParams?.Invoke(cmd.Parameters);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add(mapper(reader));
            }
            finally { if (existingConn == null) conn.Dispose(); }
            return list;
        }

        #endregion

        private static class SqlQueries
        {
            public const string GetAllSectors = "SELECT sector_id, name FROM Sector";
            public const string GetById = "SELECT * FROM Initiative WHERE initiative_id = @id";
            public const string GetByCreator = "SELECT * FROM Initiative WHERE created_by_user_id = @uid";

            public const string CreateInitiative = @"INSERT INTO Initiative (sector_id, created_by_user_id, initiative_name, objective, funding_target, start_date, end_date) 
                                                     VALUES (@sid, @uid, @name, @obj, @target, @start, @end)";

            public const string UpdateInitiative = @"UPDATE Initiative SET sector_id = @sid, initiative_name = @name, objective = @obj, 
                                                     funding_target = @target, start_date = @start, end_date = @end WHERE initiative_id = @id";

            // REFACTOR: Added WHERE created_by_user_id = @adminId to filter personal stats
            public const string DashboardStats = @"
                SELECT 
                    (SELECT ISNULL(SUM(current_raised), 0) FROM Initiative WHERE created_by_user_id = @adminId) as TotalFunds,
                    (SELECT COUNT(*) FROM Initiative WHERE created_by_user_id = @adminId AND GETDATE() BETWEEN start_date AND end_date) as ActiveCount,
                    (SELECT COUNT(*) FROM Initiative WHERE created_by_user_id = @adminId AND current_raised <= 0) as LowFundingCount";

            // Reports Queries
            public const string ReportTopSector = @"SELECT TOP 1 s.name FROM Sector s JOIN Initiative i ON s.sector_id = i.sector_id JOIN Contribution c ON i.initiative_id = c.initiative_id GROUP BY s.name ORDER BY COUNT(*) DESC";
            public const string ReportSlowInitiatives = @"SELECT initiative_name FROM Initiative WHERE start_date <= EOMONTH(DATEADD(m, -1, GETDATE())) AND initiative_id NOT IN (SELECT initiative_id FROM Contribution WHERE MONTH(timestamp) = MONTH(DATEADD(m, -1, GETDATE())) AND YEAR(timestamp) = YEAR(DATEADD(m, -1, GETDATE())))";
            public const string ReportTopSupporter = @"SELECT TOP 1 u.full_name FROM AppUser u JOIN Contribution c ON u.user_id = c.user_id WHERE MONTH(c.timestamp) = MONTH(DATEADD(m, -1, GETDATE())) GROUP BY u.full_name ORDER BY SUM(c.amount) DESC";
            public const string ReportNoSpend = @"SELECT initiative_name FROM Initiative WHERE start_date <= EOMONTH(DATEADD(m, -1, GETDATE())) AND initiative_id NOT IN (SELECT initiative_id FROM Expenditure WHERE MONTH(date_spent) = MONTH(DATEADD(m, -1, GETDATE())))";
            public const string ReportActiveLastMonth = @"SELECT s.name as Sector, i.initiative_name FROM Initiative i JOIN Sector s ON i.sector_id = s.sector_id WHERE i.start_date <= EOMONTH(DATEADD(m, -1, GETDATE())) AND i.end_date >= DATEFROMPARTS(YEAR(DATEADD(m,-1,GETDATE())), MONTH(DATEADD(m,-1,GETDATE())), 1)";
            public const string ReportSupporterStats = @"SELECT u.full_name, COUNT(DISTINCT c.initiative_id) as ProjectsCount FROM AppUser u LEFT JOIN Contribution c ON u.user_id = c.user_id WHERE u.role = 'Supporter' GROUP BY u.full_name";

            public const string DeleteInitiative = "DELETE FROM Initiative WHERE initiative_id = @id AND created_by_user_id = @uid";
            public const string LogExpenditure = @"INSERT INTO Expenditure (initiative_id, amount_spent, vendor_name, date_spent) 
                                           VALUES (@iid, @amount, @vendor, @date)";
            public const string GetTotalSpent = "SELECT ISNULL(SUM(amount_spent), 0) FROM Expenditure WHERE initiative_id = @iid";
            public const string GetAllWithProgress = "SELECT * FROM Initiative";
        }
    }
}
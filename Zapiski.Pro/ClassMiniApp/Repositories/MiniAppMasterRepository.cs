using System.Data;
using Zapisi.Pro;
using Zapiski.Pro.MiniApp.Models;

namespace Zapiski.Pro.MiniApp.Repositories
{
    public class MiniAppMasterRepository
    {
        private readonly DbHelper db;

        public MiniAppMasterRepository(DbHelper db)
        {
            this.db = db;
        }

        public MiniAppMasterProfileDto? GetMasterByKey(string key)
        {
            var safeKey = key.Replace("'", "''");

            var dt = db.ExecuteQuery($@"
                SELECT
                    m.""idMaster"",
                    m.""Key"",
                    u.""TelegrammId"",
                    u.""UserName""
                FROM ""Masters"" m
                JOIN ""Users"" u ON u.""idUser"" = m.""UserId""
                WHERE m.""Key"" = '{safeKey}'
                LIMIT 1
            ");

            if (dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];

            return new MiniAppMasterProfileDto
            {
                Id = Convert.ToInt32(row["idMaster"]),
                Key = row["Key"].ToString(),
                TelegramId = Convert.ToInt64(row["TelegrammId"]),
                Username = row["UserName"]?.ToString()
            };
        }
    }
}
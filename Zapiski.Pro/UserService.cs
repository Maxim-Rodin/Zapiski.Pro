using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zapisi.Pro
{
    internal class UserService
    {
        private readonly DbHelper db;

        public UserService()
        {
            var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            EnvConfig.Load(envPath);
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            var user = Environment.GetEnvironmentVariable("DB_USER");
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");
            db = new DbHelper($"Host={host};Port=5432;Username={user};Password={pass};Database=Zapisi.Pro;SSL Mode=Disable;Trust Server Certificate=true;");
        }

      
        public bool ExistsByTelegramId(long telegramId)
        {
            string sql = $@"
            SELECT 1 FROM public.""Users""
            WHERE ""TelegrammId"" = {telegramId}
            LIMIT 1";

            var dt = db.ExecuteQuery(sql);

            return dt.Rows.Count > 0;
        }

       
        public DataRow GetByTelegramId(long telegramId)
        {
            string sql = $@"
            SELECT * FROM public.""Users""
            WHERE ""TelegrammId"" = {telegramId}";

            var dt = db.ExecuteQuery(sql);

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }
        public void CreateUser(long telegramId, string username)
        {
            string sql = $@"
        INSERT INTO public.""Users"" (""TelegrammId"", ""UserName"")
        VALUES ({telegramId}, '{username}')
        ON CONFLICT (""TelegrammId"") DO NOTHING";

            db.ExecuteNonQuery(sql);
        }
        
    }
}

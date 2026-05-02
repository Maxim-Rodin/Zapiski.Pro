using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zapisi.Pro.State
{
    internal class StateService
    {
        private readonly DbHelper db;

        public StateService()
        {
            var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            DotNetEnv.Env.Load(envPath);
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            db = new DbHelper($"Host={host};Port=5432;Username=postgres;Password=admin;Database=Zapisi.Pro");
        }

        public void SetState(long telegramId, string state)
        {
            string sql = $@"
        INSERT INTO public.""UserStates""(""TelegramId"", ""State"")
        VALUES ({telegramId}, '{state}')
        ON CONFLICT (""TelegramId"")
        DO UPDATE SET ""State"" = EXCLUDED.""State""";

            db.ExecuteNonQuery(sql);
        }

        public string GetState(long telegramId)
        {
            string sql = $@"
        SELECT ""State"" FROM public.""UserStates""
        WHERE ""TelegramId"" = {telegramId}";

            var dt = db.ExecuteQuery(sql);

            if (dt.Rows.Count == 0)
                return null;

            return dt.Rows[0]["State"].ToString();
        }

        public void ClearState(long telegramId)
        {
            string sql = $@"
        DELETE FROM public.""UserStates""
        WHERE ""TelegramId"" = {telegramId}";

            db.ExecuteNonQuery(sql);
        }
        public void SetData(long telegramId, string data)
        {
            string sql = $@"
        UPDATE public.""UserStates""
        SET ""Data"" = '{data}'
        WHERE ""TelegramId"" = {telegramId}";

            db.ExecuteNonQuery(sql);
        }

        public string GetData(long telegramId)
        {
            string sql = $@"
        SELECT ""Data"" FROM public.""UserStates""
        WHERE ""TelegramId"" = {telegramId}";

            var dt = db.ExecuteQuery(sql);

            if (dt.Rows.Count == 0)
                return null;

            return dt.Rows[0]["Data"].ToString();
        }
    }
}

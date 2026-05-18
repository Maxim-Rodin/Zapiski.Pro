using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Zapisi.Pro.CallBacks
{
    internal class ServiceHandler 
    {


        private readonly DbHelper db;

        public ServiceHandler()
        {
            var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            DotNetEnv.Env.Load(envPath);
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            var user = Environment.GetEnvironmentVariable("DB_USER");
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");

            db = new DbHelper($"Host={host};Port=5432;Username={user};Password={pass};Database=Zapisi.Pro;SSL Mode=Disable;Trust Server Certificate=true;");
        }
        public void AddService(string key, string title, int price, int duration)
        {
            string sql = $@"
        INSERT INTO ""Services"" (""MasterId"", ""Name"", ""Price"", ""Duration"")
        VALUES (
            (SELECT ""idMaster"" FROM ""Masters"" WHERE ""Key"" = '{key}'),
            '{title}',
            {price},
            {duration}
        );
    ";
            db.ExecuteNonQuery(sql);
        }

        public List<DataRow> GetServices(string key)
        {
            string sql = $@"
        SELECT s.*
        FROM ""Services"" s
        JOIN ""Masters"" m ON s.""MasterId"" = m.""idMaster""
        WHERE m.""Key"" = '{key}';
        ";

            return db.ExecuteQuery(sql).AsEnumerable().ToList();
        }

        public void DeleteService(int id)
        {
            string sql = $@"
            DELETE FROM ""Services""
            WHERE ""idService"" = {id};
        ";

            db.ExecuteNonQuery(sql);
        }
    }
}

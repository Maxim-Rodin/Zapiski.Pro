using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;


namespace Zapisi.Pro
{
    public class DbHelper
    {

        public string connString { get; set; }
        public DbHelper(string connString)
        {
            this.connString = connString;
        }
        public NpgsqlConnection GetConnection() // метод получения соединения 
        {
            
            return new NpgsqlConnection(connString);
        }
        public DataTable ExecuteQuery(string sql, params NpgsqlParameter[] parametrs) // выполнение select комманд 
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    if (parametrs != null)
                    {
                        cmd.Parameters.AddRange(parametrs);

                    }

                    using (var adapter = new NpgsqlDataAdapter(cmd))
                    {
                        var table = new DataTable();
                        adapter.Fill(table);
                        return table;
                    }
                }
            }
        }

        public int ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public object ExecuteScalar(string sql, params NpgsqlParameter[] parameters)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    return cmd.ExecuteScalar();
                }

            }
        }
        public string GetMasterKeyByTelegramId(long telegramId)
        {
            var table = ExecuteQuery($@"
                    SELECT m.""Key""
                    FROM ""Masters"" m
                    JOIN ""Users"" u ON u.""idUser"" = m.""UserId""
                    WHERE u.""TelegrammId"" = {telegramId}
                ");

            if (table.Rows.Count == 0)
                return null;

            return table.Rows[0]["Key"].ToString();
        }

        public void CreateDefaultSchedule(int masterId)//создаем дефолтный график для мастера если его нету
        {
            for (int day = 1; day <= 7; day++)
            {
                bool isWorkDay = day <= 5; // Пн–Пт

                ExecuteNonQuery($@"
            INSERT INTO ""MasterSchedule""
            (""MasterId"", ""DayOfWeek"", ""StartTime"", ""EndTime"", ""IsActive"")
            VALUES
            ({masterId}, {day},
            '09:00', '18:00',
            {isWorkDay})
        ");
            }
        }

       

             public List<DataRow> GetByMasterKey(string key)
        {
            string sql = $@"
                SELECT s.*
                FROM ""Services"" s
                JOIN ""Masters"" m ON m.""idMaster"" = s.""MasterId""
                WHERE m.""Key"" = '{key}'
                ORDER BY s.""idService""";

            return ExecuteQuery(sql).AsEnumerable().ToList();
        }

        public int GetMasterIdByKey(string key)//получаем id мастера по его ключу
        {
            var table = ExecuteQuery($@"
                    SELECT ""idMaster""
                    FROM ""Masters""
                    WHERE ""Key"" = '{key}'
                    LIMIT 1
                ");

            return Convert.ToInt32(table.Rows[0]["idMaster"]);
        }
        public long GetMasterTelegramId(int masterId)
        {
            var table = ExecuteQuery($@"
        SELECT ""TelegrammId""
        FROM ""Masters""
        WHERE ""idMaster"" = {masterId}
        LIMIT 1
    ");

            return Convert.ToInt64(table.Rows[0]["TelegrammId"]);
        }
    }
}

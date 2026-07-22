using System.Data;
using Zapisi.Pro;
using Zapiski.Pro.MiniApp.Models;

namespace Zapiski.Pro.MiniApp.Repositories
{
    public class MiniAppAdminRepository
    {
        private readonly DbHelper db;

        public MiniAppAdminRepository(DbHelper db)
        {
            this.db = db;
        }

        public MiniAppAdminStatsDto GetStats()
        {
            var masters = Convert.ToInt32(db.ExecuteScalar(@"SELECT COUNT(*) FROM ""Masters"""));
            var landingMasters = Convert.ToInt32(db.ExecuteScalar(@"
                SELECT COUNT(*)
                FROM ""Masters""
                WHERE ""RegistrationSource"" = 'landing'
            "));

            return new MiniAppAdminStatsDto
            {
                Users = Convert.ToInt32(db.ExecuteScalar(@"SELECT COUNT(*) FROM ""Users""")),
                Masters = masters,
                Bookings = Convert.ToInt32(db.ExecuteScalar(@"SELECT COUNT(*) FROM ""Bookings""")),
                Payments = Convert.ToInt32(db.ExecuteScalar(@"
                    SELECT COUNT(*) 
                    FROM ""Bookings""
                    WHERE ""Status"" = 'waiting_payment_confirm'
                ")),
                LandingMasters = landingMasters,
                DirectMasters = masters - landingMasters,
                RegistrationsLast30Days = Convert.ToInt32(db.ExecuteScalar(@"
                    SELECT COUNT(*)
                    FROM ""Masters""
                    WHERE ""RegisteredAt"" >= NOW() - INTERVAL '30 days'
                ")),
                LandingSharePercent = masters == 0
                    ? 0
                    : Math.Round(landingMasters * 100m / masters, 1)
            };
        }

        public List<MiniAppMasterDto> GetMasters()
        {
            var dt = db.ExecuteQuery(@"
                SELECT 
                    m.""idMaster"",
                    m.""Key"",
                    COALESCE(m.""AvatarUrl"", '') AS ""AvatarUrl"",
                    COALESCE(m.""IsFounder"", false) AS ""IsFounder"",
                    m.""TrialEndsAt"",
                    m.""SubscriptionEndsAt"",
                    COALESCE(m.""SubscriptionPlan"", '') AS ""SubscriptionPlan"",
                    u.""TelegrammId"",
                    u.""UserName""
                FROM ""Masters"" m
                JOIN ""Users"" u ON u.""idUser"" = m.""UserId""
                ORDER BY m.""idMaster"" DESC
            ");

            var masters = new List<MiniAppMasterDto>();

            foreach (DataRow row in dt.Rows)
            {
                masters.Add(new MiniAppMasterDto
                {
                    Id = Convert.ToInt32(row["idMaster"]),
                    Key = row["Key"].ToString(),
                    TelegramId = Convert.ToInt64(row["TelegrammId"]),
                    Username = row["UserName"]?.ToString(),
                    AvatarUrl = row["AvatarUrl"]?.ToString() ?? string.Empty,
                    IsFounder = Convert.ToBoolean(row["IsFounder"]),
                    TrialEndsAt = ReadNullableDateTime(row["TrialEndsAt"]),
                    SubscriptionEndsAt = ReadNullableDateTime(row["SubscriptionEndsAt"]),
                    SubscriptionPlan = row["SubscriptionPlan"]?.ToString() ?? string.Empty
                });

                FillAccess(masters[^1]);
            }

            return masters;
        }

        public List<MiniAppUserDto> GetUsers()
        {
            var dt = db.ExecuteQuery(@"
                SELECT 
                    u.""idUser"",
                    u.""TelegrammId"",
                    u.""UserName"",
                    COUNT(b.""idBooking"") AS ""BookingsCount""
                FROM ""Users"" u
                LEFT JOIN ""Bookings"" b ON b.""UserId"" = u.""idUser""
                GROUP BY u.""idUser"", u.""TelegrammId"", u.""UserName""
                ORDER BY u.""idUser"" DESC
            ");

            var users = new List<MiniAppUserDto>();

            foreach (DataRow row in dt.Rows)
            {
                users.Add(new MiniAppUserDto
                {
                    Id = Convert.ToInt32(row["idUser"]),
                    TelegramId = Convert.ToInt64(row["TelegrammId"]),
                    Username = row["UserName"]?.ToString(),
                    BookingsCount = Convert.ToInt32(row["BookingsCount"])
                });
            }

            return users;
        }

        public bool UserExistsByTelegramId(long telegramId)
        {
            var dt = db.ExecuteQuery($@"
                SELECT 1
                FROM ""Users""
                WHERE ""TelegrammId"" = {telegramId}
                LIMIT 1
            ");

            return dt.Rows.Count > 0;
        }

        public bool MasterKeyExists(string key)
        {
            var safeKey = key.Replace("'", "''");

            var dt = db.ExecuteQuery($@"
                SELECT 1
                FROM ""Masters""
                WHERE ""Key"" = '{safeKey}'
                LIMIT 1
            ");

            return dt.Rows.Count > 0;
        }

        public int GetMasterIdByKey(string key)
        {
            var safeKey = key.Replace("'", "''");

            var result = db.ExecuteScalar($@"
                SELECT ""idMaster""
                FROM ""Masters""
                WHERE ""Key"" = '{safeKey}'
            ");

            return Convert.ToInt32(result);
        }

        public MiniAppMasterDto? GetMasterById(int masterId)
        {
            var dt = db.ExecuteQuery($@"
                SELECT 
                    m.""idMaster"",
                    m.""Key"",
                    COALESCE(m.""AvatarUrl"", '') AS ""AvatarUrl"",
                    COALESCE(m.""IsFounder"", false) AS ""IsFounder"",
                    m.""TrialEndsAt"",
                    m.""SubscriptionEndsAt"",
                    COALESCE(m.""SubscriptionPlan"", '') AS ""SubscriptionPlan"",
                    u.""TelegrammId"",
                    u.""UserName""
                FROM ""Masters"" m
                JOIN ""Users"" u ON u.""idUser"" = m.""UserId""
                WHERE m.""idMaster"" = {masterId}
            ");

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];

            return new MiniAppMasterDto
            {
                Id = Convert.ToInt32(row["idMaster"]),
                Key = row["Key"].ToString(),
                TelegramId = Convert.ToInt64(row["TelegrammId"]),
                Username = row["UserName"]?.ToString(),
                AvatarUrl = row["AvatarUrl"]?.ToString() ?? string.Empty,
                IsFounder = Convert.ToBoolean(row["IsFounder"]),
                TrialEndsAt = ReadNullableDateTime(row["TrialEndsAt"]),
                SubscriptionEndsAt = ReadNullableDateTime(row["SubscriptionEndsAt"]),
                SubscriptionPlan = row["SubscriptionPlan"]?.ToString() ?? string.Empty
            };
        }

        public void CreateMaster(long telegramId, string key, bool isFounder, int subscriptionMonths)
        {
            var safeKey = key.Replace("'", "''");
            var safePlan = subscriptionMonths switch
            {
                1 => "month",
                3 => "quarter",
                12 => "year",
                _ => string.Empty
            };

            db.ExecuteNonQuery($@"
                INSERT INTO public.""Masters""
                    (""UserId"", ""Key"", ""IsFounder"", ""TrialStartedAt"", ""TrialEndsAt"", ""SubscriptionEndsAt"", ""SubscriptionPlan"")
                VALUES (
                    (SELECT ""idUser"" FROM public.""Users"" WHERE ""TelegrammId"" = {telegramId}),
                    '{safeKey}',
                    {isFounder.ToString().ToLowerInvariant()},
                    CASE WHEN {isFounder.ToString().ToLowerInvariant()} THEN NULL ELSE NOW() END,
                    CASE WHEN {isFounder.ToString().ToLowerInvariant()} THEN NULL ELSE NOW() + INTERVAL '30 days' END,
                    CASE WHEN {subscriptionMonths} > 0 THEN NOW() + INTERVAL '{subscriptionMonths} months' ELSE NULL END,
                    {(string.IsNullOrWhiteSpace(safePlan) ? "NULL" : $"'{safePlan}'")}
                );

                UPDATE public.""Users""
                SET ""Role"" = 'master'
                WHERE ""TelegrammId"" = {telegramId};
            ");
        }

        public void UpdateMasterSubscription(int masterId, bool? isFounder, int subscriptionMonths)
        {
            var safePlan = subscriptionMonths switch
            {
                1 => "month",
                3 => "quarter",
                12 => "year",
                _ => string.Empty
            };

            db.ExecuteNonQuery($@"
                UPDATE ""Masters""
                SET
                    ""IsFounder"" = CASE
                        WHEN {isFounder.HasValue.ToString().ToLowerInvariant()} THEN {isFounder.GetValueOrDefault().ToString().ToLowerInvariant()}
                        ELSE ""IsFounder""
                    END,
                    ""SubscriptionEndsAt"" = CASE
                        WHEN {subscriptionMonths} > 0 THEN
                            GREATEST(
                                COALESCE(""SubscriptionEndsAt"", NOW()),
                                COALESCE(""TrialEndsAt"", NOW()),
                                NOW()
                            ) + INTERVAL '{subscriptionMonths} months'
                        ELSE ""SubscriptionEndsAt""
                    END,
                    ""SubscriptionPlan"" = CASE
                        WHEN {subscriptionMonths} > 0 THEN '{safePlan}'
                        ELSE ""SubscriptionPlan""
                    END
                WHERE ""idMaster"" = {masterId}
            ");
        }

        public void DeleteMaster(int masterId, long telegramId)
        {
            db.ExecuteNonQuery($@"
                DELETE FROM ""Masters""
                WHERE ""idMaster"" = {masterId};

                UPDATE ""Users""
                SET ""Role"" = 'client'
                WHERE ""TelegrammId"" = {telegramId};
            ");
        }

        public void CreateDefaultSchedule(int masterId)
        {
            for (int day = 1; day <= 7; day++)
            {
                db.ExecuteNonQuery($@"
            INSERT INTO ""MasterSchedule""
            (
                ""MasterId"",
                ""DayOfWeek"",
                ""StartTime"",
                ""EndTime"",
                ""IsActive""
            )
            VALUES
            (
                {masterId},
                {day},
                '09:00:00',
                '18:00:00',
                false
            )
        ");
            }
        }

        public bool IsAdmin(long telegramId)
        {
            var result = db.ExecuteScalar($@"
        SELECT ""Role""
        FROM ""Users""
        WHERE ""TelegrammId"" = {telegramId}
        LIMIT 1
    ");

            return result != null && result.ToString() == "admin";
        }

        private static DateTime? ReadNullableDateTime(object value)
        {
            if (value == DBNull.Value || value == null)
                return null;

            return Convert.ToDateTime(value);
        }

        private static void FillAccess(MiniAppMasterDto master)
        {
            var now = DateTime.UtcNow;

            if (master.IsFounder)
            {
                master.HasAccess = true;
                master.AccessType = "founder";
                master.DaysLeft = 9999;
                return;
            }

            if (master.SubscriptionEndsAt.HasValue && master.SubscriptionEndsAt.Value.ToUniversalTime() > now)
            {
                master.HasAccess = true;
                master.AccessType = "paid";
                master.DaysLeft = Math.Max(0, (int)Math.Ceiling((master.SubscriptionEndsAt.Value.ToUniversalTime() - now).TotalDays));
                return;
            }

            if (master.TrialEndsAt.HasValue && master.TrialEndsAt.Value.ToUniversalTime() > now)
            {
                master.HasAccess = true;
                master.AccessType = "trial";
                master.DaysLeft = Math.Max(0, (int)Math.Ceiling((master.TrialEndsAt.Value.ToUniversalTime() - now).TotalDays));
                return;
            }

            master.HasAccess = false;
            master.AccessType = "expired";
            master.DaysLeft = 0;
        }
    }
}

using DotNetEnv;
using Hangfire; //бибилиотеки для чтого чтоб реализовать напоминания
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zapisi.Pro.CallBacks;
using Zapisi.Pro.State;
using Zapiski.Pro.BasedClasses;
using Zapiski.Pro.ClassMiniApp.Repositories;
using Zapiski.Pro.ClassMiniApp.Services;
using Zapiski.Pro.MiniApp.Endpoints;
using Zapiski.Pro.MiniApp.Repositories;
using Zapiski.Pro.MiniApp.Services;
using Zapiski.Pro.Services;

namespace Zapisi.Pro
{
    internal class Program
    {


       
        private static ITelegramBotClient botClient; // клиент для взаимодействия с Telegram Bot API

            private static ReceiverOptions receiverOptions; // настройки для получения обновлений от Telegram
            private static Dictionary<string, byte[]> photoCache = new Dictionary<string, byte[]>(); // кэш для хранения фотографий

        private static DbHelper db ;

        static async Task Main(string[] args)
            {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MiniApp", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            var app = builder.Build();
           
            app.UseCors("MiniApp");

            var envPath = Path.Combine(AppContext.BaseDirectory, ".env");

            

            EnvConfig.Load(envPath);

            // загрузка переменных окружения из .env файла
            var token = Environment.GetEnvironmentVariable("BOT_TOKEN"); // получение токена бота из переменных окружения  

            var host = Environment.GetEnvironmentVariable("DB_HOST");
            Console.WriteLine("DB_HOST = " + Environment.GetEnvironmentVariable("DB_HOST"));
            var user = Environment.GetEnvironmentVariable("DB_USER");
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");
            db = new DbHelper($"Host={host};Port=5432;Username={user};Password={pass};Database=Zapisi.Pro");
            Console.WriteLine("===== ENV DEBUG =====");
            Console.WriteLine($"DB_HOST = {Environment.GetEnvironmentVariable("DB_HOST")}");
            Console.WriteLine($"DB_USER = {Environment.GetEnvironmentVariable("DB_USER")}");
            Console.WriteLine($"DB_PASSWORD = {(Environment.GetEnvironmentVariable("DB_PASSWORD") != null ? "***" : "NULL")}");
            Console.WriteLine("=====================");
            GlobalConfiguration.Configuration .UsePostgreSqlStorage(c =>  c.UseNpgsqlConnection($"Host={host};Port=5432;Username={user};Password={pass};Database=Zapisi.Pro")
                );
            using var hangfireServer = new BackgroundJobServer();

            Console.WriteLine("Hangfire + Bot запущены");
           





            botClient = new TelegramBotClient(token); // инициализация клиента с токеном бота
            ReminderService.BotClient = botClient;
            BookingJobs.BotClient = botClient;
            BookingJobs.Db = db;
            var miniAppAdminRepository = new MiniAppAdminRepository(db);
            var miniAppAdminService = new MiniAppAdminService(miniAppAdminRepository, botClient);

            var miniAppMasterRepository = new MiniAppMasterRepository(db);
            var cloudinaryImageService = new CloudinaryImageService();
            var miniAppMasterService = new MiniAppMasterService(miniAppMasterRepository, cloudinaryImageService);

            var miniAppUserRepository = new MiniAppUserRepository(db);
            var miniAppUserService = new MiniAppUserService(miniAppUserRepository);

            app.MapMiniAppAdminEndpoints(miniAppAdminService);
            app.MapMiniAppMasterEndpoints(miniAppMasterService);
            app.MapMiniAppUserEndpoints(miniAppUserService);
   
            app.RunAsync();

            //BookingJobs.RestoreReminders();

            receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[] // тут указваем тип полуаемх обновлений
                    {
                    UpdateType.Message,  //сообщение 
                    UpdateType.CallbackQuery, // колбек 
                },

                    DropPendingUpdates = true // выбрасывать исключение при получении неразрешенных обновлений
                };

                using (var cts = new CancellationTokenSource())
                { // токен для отмены получения обновлений


                    botClient.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions, cancellationToken: cts.Token); // запуск получения обновлений
                  await Task.Delay(-1);
                }

                try
                {
                    var me = botClient.GetMe().Result;
                    Console.WriteLine($"{me.FirstName} запущен ");
                    await Task.Delay(-1); // задержка для предотвращения завершения программы

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

               
                

            }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                var stateService = new StateService();
                var dbHelper = db;
                var scheduleService = new ScheduleService(botClient, dbHelper, stateService);
                var adminService = new AdminHandler(botClient);
                var masterService = new MasterHandler(botClient, scheduleService);
                var userservice = new UserHandler(botClient);
                var router = new CallBackRouter(new List<ICallbackHandler>
                    {
                       adminService, masterService ,userservice
                    });
                
                
                
                var stateRouter = new StateRouter(adminService, masterService, stateService ,botClient, scheduleService,userservice);


                if (update.Type == UpdateType.CallbackQuery)
                {
                    Console.WriteLine("CALLBACK ПРИШЁЛ: " + update.CallbackQuery.Data);
                    await router.Route(update.CallbackQuery);
                    return;
                }
                if (update.Type == UpdateType.Message && update.Message?.Text != null)
                {
                    var message = update.Message;
                    var chat = message.Chat;
                    var userService = new UserService();

                    var telegramId = message.From.Id;
                    var username = message.From.Username ?? "unknown";

                    
                    if (!userService.ExistsByTelegramId(telegramId)) // проверяем, существует ли пользователь с данным Telegram ID в базе данных
                    {
                        userService.CreateUser(telegramId, username);// если пользователь не существует, создаем его в базе данных
                    }

                    var state = stateService.GetState(chat.Id);

                    if (state != null)
                    {
                        await stateRouter.Handle(state, message);
                        return;
                    }
                    if (message.Text.StartsWith("/start"))
                    {
                        
                        var parts = message.Text.Split(' ');
                        if (parts.Length > 1) 
                        {
                            var key = parts[1];
                            await masterService.ShowProfileFromStart( chat.Id, message.From.Id, key );
                            return;
                        }


                        // создаём пользователя если его нет
                        if (!userService.ExistsByTelegramId(telegramId))
                        {
                            userService.CreateUser(telegramId, username);
                        }

                        var user = userService.GetByTelegramId(telegramId);
                        var role = user["Role"].ToString();

                        var miniAppUrl = Environment.GetEnvironmentVariable("MINIAPP_URL") ?? "https://app-zapisi-pro.site";

                        var keyboard = new List<InlineKeyboardButton[]>
                                {
                                    new[]
                                    {
                                        InlineKeyboardButton.WithWebApp(
                                            "📱 Мини-приложение",
                                            new WebAppInfo($"{miniAppUrl.TrimEnd('/')}/user/{telegramId}")
                                        )
                                    },
                                    new[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("📅 Записаться", "client:book")
                                    },
                                    new[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("📖 Мои записи", "client:my_records")
                                    }
                                };

                        // 👇 добавляем роли как доп. кнопки
                        if (role == "master")
                        {
                            var key = db.GetMasterKeyByTelegramId(telegramId);
                            keyboard.Add(new[]
                            {
                                    InlineKeyboardButton.WithCallbackData("💅 Личный кабинет мастера", $"master:master_panel:{key}")
                                });
                        }

                        if (role == "admin")
                        {
                            keyboard.Add(new[]
                            {
                                    InlineKeyboardButton.WithCallbackData("🛠 Админ панель", "admin:menu")
                                });
                        }

                        var path = Path.Combine(AppContext.BaseDirectory, "source", "helloPicture.png");

                        using (var stream = new FileStream(path, FileMode.Open))
                        {
                            await botClient.SendPhoto(
                                chatId: message.Chat.Id,
                                photo: InputFile.FromStream(stream),
                                replyMarkup: new InlineKeyboardMarkup(keyboard)
                            );
                        }

                        return;
                    }
                    if (message.Text == "/admin")// команда для отображения админского меню, доступна только пользователям с ролью "admin"
                    {
                        

                        var telegrammId = message.From.Id;
                        var user = userService.GetByTelegramId(telegrammId);


                        if (user == null) // проверка существования пользователя в базе данных, если пользователь не найден, отправляем сообщение об ошибке и прекращаем выполнение метода
                        {
                            await botClient.SendMessage(chat.Id, "❌ Пользователь не найден");
                            return;
                        }

                        var role = user["Role"].ToString();
                        if (role == "admin")// проверка рели пользователя 
                        {
                            await adminService.ShowMenu(chat.Id);// если роль пользователя "admin", отображаем ему админское меню
                        }
                        else
                        {
                            await botClient.SendMessage(chat.Id, "Недостаточно прав ❌");
                        }

                       





                    }
                   
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при обработке обновления:");
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken) // метод для обработки ошибок при получении обновлений
            {
                string errorMessage;
                switch (error)
                {
                    case ApiRequestException apiRequestException:
                        errorMessage = $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}";
                        break;
                    default:
                        errorMessage = error.ToString();
                        break;

                }
                ;
                Console.WriteLine(errorMessage);
                await Task.CompletedTask;
            }
     
      
    }
}

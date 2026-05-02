using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zapisi.Pro.State
{
    internal class UserState //класс состояния пользоателя
    {
        public long TelegramId { get; set; }
        public string State { get; set; }
        public string Data { get; set; }

        public UserState(long telegramId, string state, string data)
        {
            TelegramId = telegramId;
            State = state;
            Data = data;
        }
    }
}

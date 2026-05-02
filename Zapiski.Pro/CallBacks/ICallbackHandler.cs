using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Zapisi.Pro.CallBacks;

namespace Zapisi.Pro
{
    internal interface ICallbackHandler
    {
        string Entity {  get;  }
        Task Handle(CallBackData data, CallbackQuery query);
    }
}

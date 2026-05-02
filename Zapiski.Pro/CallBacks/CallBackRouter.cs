using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Zapisi.Pro.CallBacks
{
    internal class CallBackRouter
    {
        private readonly List<ICallbackHandler> handlers; // список наших колбеков 

        public CallBackRouter(List<ICallbackHandler> handlers) // конструктор нашего роутера 
        {
            this.handlers = handlers;
        }
        public async Task Route(CallbackQuery query) 
        {
            var data = CallBackParser.Parse(query.Data);

            var hendler = handlers.FirstOrDefault(x => x.Entity == data.Entity);
            if (hendler == null) { return; }
            await hendler.Handle(data, query);


        }
    }
}

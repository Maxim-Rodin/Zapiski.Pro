using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zapisi.Pro.CallBacks
{
    internal static class CallBackParser 
    {
        public static CallBackData Parse(string data) //метод парса для колбеков 
        {
            var parts = data.Split(':');
            return new CallBackData
            {
                Entity = parts.Length > 0 ? parts[0] : null,
                Action = parts.Length > 1 ? parts[1] : null,
                Id = parts.Length > 2 ? parts[2] : null,
                SubAction = parts.Length > 3 ? parts[3] : null,
                Extra = parts.Length > 4 ? parts[4] : null,
                Ultra = parts.Length > 5 ? parts[5] : null
            };


        }
    }
}

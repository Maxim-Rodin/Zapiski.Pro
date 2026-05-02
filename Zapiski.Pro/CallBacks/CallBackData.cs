using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zapisi.Pro.CallBacks
{
    internal class CallBackData //основной класс описывающий колбек 
    {
        public string Entity { get; set; }      
            
        public string Action { get; set; }
        public string Id { get; set; }  
        public string SubAction { get; set; } 
        public string Extra { get; set; }
        public string Ultra { get; set; }


    }
}

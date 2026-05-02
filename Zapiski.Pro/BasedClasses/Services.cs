using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zapisi.Pro.BasedClasses
{
    internal class Services
    {
        public int idService {  get; set; }

        public int MasterId { get; set; }

        public string Name {  get; set; }

        public int Price { get; set; }  

        public int Duration { get; set; }

        public Services(int idService, int masterId, string name, int price, int duratione)        {
            this.idService = idService;
            MasterId = masterId;
            Name = name;
            Price = price;
            Duration = duratione;
        }

    }
}

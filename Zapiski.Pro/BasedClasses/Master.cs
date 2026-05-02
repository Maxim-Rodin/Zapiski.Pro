using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zapisi.Pro
{
    internal class Master //класс мастера 
    {
        public int idMaster { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }

        public string PhotoPath { get; set; }

        public int UserId { get; set; }
        public string Description { get; set; }

        public Master(int idMaster, string Name, string Key, string PhotoPath, int UserId, string description) //конструктор для создания нового мастера
        {
            this.idMaster = idMaster;
            this.Name = Name;
            this.Key = Key;
            this.PhotoPath = PhotoPath;
            this.UserId = UserId;
            Description = description;
        }


    }
}

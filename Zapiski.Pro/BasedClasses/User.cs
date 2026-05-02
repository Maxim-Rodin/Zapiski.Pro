using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Zapisi.Pro
{
    internal class User //класс нашего пользователя 
    {
        public int idUser {get;set;}
        public BigInteger TelegrammId { get;set;}
        public string UserName {get;set;}
        public string Role {get;set;}

        public User(int idUser, BigInteger TelegrammId, string UserName, string Role) //конструктор для создания нового пользователя
        {
            this.idUser = idUser;
            this.TelegrammId = TelegrammId;
            this.UserName = UserName;
            this.Role = Role;
        }

        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ResolvePay_WebAPI.Entities
{
    public class LoginRequest_PGSQL_D
    {

        public string email { get; set; }
        public string password { get; set; }
        //Salt
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
    }

    public class GetTestDetails_PGSQL_D
    {
        public int empmasterid { get; set; }
        public string empnumber { get; set; }
        public string empfirstname { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public DateTime doj { get; set; }
        public string passwordsalt { get; set; }

    }
}

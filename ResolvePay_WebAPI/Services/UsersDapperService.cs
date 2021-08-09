using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Devart.Data.PostgreSql;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using ResolvePay_WebAPI.Model;
using static ResolvePay_WebAPI.Startup;
using System.Threading.Tasks;
using ResolvePay_WebAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using ResolvePay_WebAPI.Entities;
using ResolvePay_WebAPI.Model.V1;
using ResolvePay_WebAPI.Services.V1;

namespace ResolvePay_WebAPI.Services
{
    public interface IUsersDapperService
    {
        public Task<bool> IsValidUserCredentials_PGSQL(string userName, string password);
        public Task<bool> UpdatePassword_PGSQL(string userName, string password, string passwordSalt);
        public Task<GetTestDetails_PGSQL_D> GetUserDetails_PGSQL(string userName);

        string GetUserRole(string userName);
    }

    public class UsersDapperService : IUsersDapperService
    {
        private readonly ILogger<UsersDapperService> _logger;

        MySqlConnection con;
        private IDictionary<string, string> _users = new Dictionary<string, string>();

        private GetTestDetails_PGSQL _userddata = new GetTestDetails_PGSQL();
        private readonly DBDapperContext _context;

        private GetDetails _userdata = new GetDetails();
        private ConnectionStringsConfig _connectionStrings;

        public UsersDapperService(ILogger<UsersDapperService> logger, IOptionsSnapshot<ConnectionStringsConfig> connectionStrings, DBDapperContext context)
        {
            _logger = logger;
            _connectionStrings = connectionStrings?.Value ?? throw new ArgumentNullException(nameof(connectionStrings));
            _context = context;
        }

        public async Task<bool> IsValidUserCredentials_PGSQL(string userName, string password)
        {
            _logger.LogInformation($"Validating user [{userName}]");
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    return false;
                }

                var query = "SELECT * FROM empmaster";
                using (var connection = _context.CreateConnection())
                {
                    var companies = await connection.QueryAsync<GetTestDetails_PGSQL>(query);
                    if (companies.Count() != 0)
                    {
                        foreach (var item in companies)
                        {
                            string optionKey = item.email.ToString();
                            if (!_users.ContainsKey(optionKey))
                                _users.Add(optionKey, item.password.ToString());
                        }
                    }
                    else
                    {
                    }
                }

                return _users.TryGetValue(userName, out var p) && p == password;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error at IsValidUserCredentials_PGSQL : [{ex.Message}], [{userName}]");
               throw;
            }
        }

        public async Task<GetTestDetails_PGSQL_D> GetUserDetails_PGSQL(string userName)
        {
            var query = "SELECT * FROM empmaster as e where e.email='" + userName+"'";
            using (var connection = _context.CreateConnection())
            {
                var companies = await connection.QueryAsync<GetTestDetails_PGSQL_D>(query);
                
                return companies.FirstOrDefault();
            }
        }
        
        public async Task<bool> UpdatePassword_PGSQL(string userName, string password, string passwordSalt)
        {
            var query = "UPDATE empmaster SET password = '" + password + "',passwordsalt='"+passwordSalt+"' WHERE email = '" + userName + "'"; 

            using (var connection = _context.CreateConnection())
            {
                var companies = await connection.QueryAsync<GetTestDetails_PGSQL_D>(query);
                return true;
            }
        }

       /* public GetTestDetails_PGSQL GetUserDetails_PGSQL(string userName)
        {
            _logger.LogInformation($"Getting User Details [{userName}]");
            try
            {
                foreach (var item in _context.empmaster)
                {
                    if (item.email == userName)
                    {
                        _userddata.empfirstname = item.empfirstname;
                        _userddata.empnumber = item.empnumber;
                        _userddata.dateofjoining = item.dateofjoining;
                        _userddata.email = item.email;
                    }
                }

                return _userddata;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error at GetUserDetails_PGSQL : [{ex.Message}], [{userName}]");
                throw;
            }
        }*/

        //Methods with MySQL Connection
        public bool IsValidUserCredentials(string userName, string password)
        {
            _logger.LogInformation($"Validating user [{userName}]");
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    return false;
                }

                con = new MySqlConnection(_connectionStrings.DefaultConnection);
                //con = new MySqlConnection("server=localhost;user=root;password=;database=customerdb_terex;AllowZeroDateTime=True");
                con.Open();

                MySqlDataAdapter sql = new MySqlDataAdapter("SELECT * FROM empmaster", con);
                //MySqlDataAdapter sql = new MySqlDataAdapter("SELECT * FROM empmaster e WHERE e.Email= '" + userName + "' AND e.Password= '" + encodedPassword + "' LIMIT 1;", con);
                DataTable data = new DataTable();
                sql.Fill(data);

                if (data.Rows.Count > 0)
                {

                    foreach (DataRow item in data.Rows)
                    {
                        string optionKey = item["Email"].ToString();
                        if (!_users.ContainsKey(optionKey))
                            _users.Add(optionKey, item["Password"].ToString());
                    }
                }
                else
                {
                }

                con.Close();

                return _users.TryGetValue(userName, out var p) && p == password;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error at IsValidUserCredentials : [{ex.Message}], [{userName}]");
                throw;
            }
        }

        public GetDetails GetUserDetails(string userName)
        {
            _logger.LogInformation($"Getting User Details [{userName}]");
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return null;
                }

                con = new MySqlConnection(_connectionStrings.DefaultConnection);
                con.Open();

                MySqlDataAdapter sql = new MySqlDataAdapter("SELECT * FROM empmaster e WHERE e.Email= '" + userName + "'", con);
                DataTable data = new DataTable();
                sql.Fill(data);

                if (data.Rows.Count > 0)
                {
                    foreach (DataRow item in data.Rows)
                    {
                        _userdata.Employeenumber = item["EmpNumber"].ToString();
                        _userdata.Employeename = item["EmpFirstName"].ToString() + item["EmpMiddleName"].ToString() + item["EmpLastName"].ToString();
                        _userdata.roleid = item["RoleID"].ToString();
                        _userdata.DOJ = item["DateOfJoining"].ToString();
                    }
                }
                else
                {
                }

                con.Close();

                return _userdata;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error at GetUserDetails : [{ex.Message}], [{userName}]");
                throw;
            }
        }

        public bool IsAnExistingUser(string userName)
        {
            return _users.ContainsKey(userName);
        }


        public string GetUserRole(string userName)
        {
            if (!IsAnExistingUser(userName))
            {
                return string.Empty;
            }

            if (userName == "srikanth.dg@resolveindia.com")
            {
                return UserRoles.Admin;
            }

            return UserRoles.BasicUser;
        }

        public string GetMd5Hash(string input)
        {
            using MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    } 
}

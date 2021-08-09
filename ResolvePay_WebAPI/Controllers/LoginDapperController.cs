using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResolvePay_WebAPI.Entities;
using ResolvePay_WebAPI.Infrastructure;
using ResolvePay_WebAPI.Model;
using ResolvePay_WebAPI.Model.V2;
using ResolvePay_WebAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ResolvePay_WebAPI.Controllers
{
    //Controller with Dapper ORM
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class LoginDapperController : Controller
    {
        private readonly ILogger<LoginDapperController> _logger;
        private IUsersDapperService _usersService;
        private readonly IJwtAuthManager _jwtAuthManager;

        public LoginDapperController(ILogger<LoginDapperController> logger, IUsersDapperService usersService, IJwtAuthManager jwtAuthManager)
        {
            _logger = logger;
            _usersService = usersService;
            _jwtAuthManager = jwtAuthManager;
        }

        //Login API
        [AllowAnonymous]
        [HttpPost("loginpgsql")]
        public async Task<IActionResult> Login_pgsql([FromBody] LoginRequest_PGSQL request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest();
                }

                var data = await _usersService.GetUserDetails_PGSQL(request.email);
                bool isAuth = VerifyPassword(request.password, data.password);

                if (!isAuth)
                {
                    return StatusCode(500, "Incorrect Password");
                }

                var role = _usersService.GetUserRole(request.email);  //To check whether the user id is admin or basic user
                var claims = new[]
                {
                new Claim(ClaimTypes.Name,request.email),
                new Claim(ClaimTypes.Role, role),

            };

                var jwtResult = _jwtAuthManager.GenerateTokens(request.email, claims, DateTime.Now);
                _logger.LogInformation($"User [{request.email}] logged in the system.");
                return Ok(new LoginResult
                {
                    result = "Logged in sucessfully!",
                    Role = role,
                    token = jwtResult.AccessToken,
                    refreshtoken = jwtResult.RefreshToken.TokenString
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error at Login_pgsql : [{ex.Message}], [{request.email}]");
                return Ok(new LoginResponse()
                {
                    result = "Failed",
                    message = ex.Message,
                    token = ""
                });
            }
        }

        //Fetching details based on bearer token's username
        [Authorize]
        [HttpGet("getdatapgsql")]
        public async Task<IActionResult> GetData_PGSQL()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest();
                }

                if (_usersService.GetUserDetails_PGSQL(User.Identity?.Name) == null)
                {
                    return StatusCode(500, "No Data Found!");
                }

                var data = await _usersService.GetUserDetails_PGSQL(User.Identity?.Name);

                _logger.LogInformation($"User [{User.Identity?.Name}] logged in the system.");
                List<GetTestDetails_PGSQL_D> res = new List<GetTestDetails_PGSQL_D>();
                return Ok(new ResultTestDetails_PGSQL
                {
                    result = "Success",
                    message = "Details fetched successfully!",
                    empfirstname = data.empfirstname,
                    empnumber = data.empnumber,
                    dateofjoining = GetDateTimeFrmt(data.doj),
                    email = data.email,
                    token = ""
                });

                return Ok(res);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error at GetData_PGSQL : [{ex.Message}], [{User.Identity?.Name}]");
                return Ok(new LoginResponse()
                {
                    result = "Failed",
                    message = ex.Message,
                    token = ""
                });
            }
        }

        //Registering or Updating new password 
        [AllowAnonymous]
        [HttpPost("registerpwd")]
        public async Task<IActionResult> Register([FromBody] LoginRequest_PGSQL_D user)
        {
            try
            {
                byte[] passwordHash = null;
                string passwordSalt = "";
                string password = CreatePasswordHash(user.password, passwordHash, false);
                if (password != "")
                {
                    passwordSalt = password.Split(":").LastOrDefault();
                }

                await _usersService.UpdatePassword_PGSQL(user.Username, password, passwordSalt);

                return Ok(new LoginResponse()
                {
                    result = "Success",
                    message = "Password registered successfully!",
                    token = ""
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error at GetMd5Hash_SALT : [{ex.Message}], [{User.Identity?.Name}]");

                return Ok(new LoginResponse()
                {
                    result = "Failed",
                    message = ex.Message,
                    token = ""
                });
            }
        }

        //Creating hash password with salt
        public string CreatePasswordHash(string password, byte[] salt = null, bool needsOnlyHash = false)
        {
            try
            {
                if (salt == null || salt.Length != 16)
                {
                    // generate a 128-bit salt using a secure PRNG
                    salt = new byte[128 / 8];
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(salt);
                    }
                }

                string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 256 / 8));

                if (needsOnlyHash) return hashed;
                // password will be concatenated with salt using ':'
                return $"{hashed}:{Convert.ToBase64String(salt)}";
            }
            catch
            {
                return "";
            }
        }

        //Verifying user password with newly created hash password with salt
        public bool VerifyPassword(string passwordToCheck, string hashedPasswordWithSalt)
        {
            try
            {
                // retrieve both salt and password from 'hashedPasswordWithSalt'
                var passwordAndHash = hashedPasswordWithSalt.Split(':');
                if (passwordAndHash == null || passwordAndHash.Length != 2)
                    return false;
                var salt = Convert.FromBase64String(passwordAndHash[1]);
                if (salt == null)
                    return false;
                // hash the given password
                var hashOfpasswordToCheck = CreatePasswordHash(passwordToCheck, salt, true);
                if (hashOfpasswordToCheck != "")
                {
                    // compare both hashes
                    if (String.Compare(passwordAndHash[0], hashOfpasswordToCheck) == 0)
                    {
                        return true;
                    }
                }

            }
            catch
            {
                return false;
            }
            return false;
        }

        private string GetDateTimeFrmt(DateTime doj)
        {
            string dt = "";
            try
            {
                dt = doj.ToString("MM-dd-yyyy");
            }
            catch { }

            return dt;
        }

    }
}

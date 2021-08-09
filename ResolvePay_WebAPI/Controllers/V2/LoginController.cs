using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResolvePay_WebAPI.Entities;
using ResolvePay_WebAPI.Infrastructure;
using ResolvePay_WebAPI.Model.V2;
using ResolvePay_WebAPI.Services.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ResolvePay_WebAPI.Controllers.V2
{
    //Controller with EF Core ORM
    [ApiVersion("2")]
    [ApiController]
    [Authorize]
    [Route("api/v{version:apiVersion}")]
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private IUsersService _usersService;
        private readonly IJwtAuthManager _jwtAuthManager;

        public LoginController(ILogger<LoginController> logger, IUsersService usersService, IJwtAuthManager jwtAuthManager)
        {
            _logger = logger;
            _usersService = usersService;
            _jwtAuthManager = jwtAuthManager;
        }

        //Login API
        [AllowAnonymous]
        [HttpPost("employee/login")]
        public async Task<IActionResult> Login_pgsql([FromBody] LoginRequest_PGSQL request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new LoginResponse()
                    {
                        result = Convert.ToInt32(HttpStatusCode.BadRequest).ToString() +" " + "Bad Request",
                        message = "",
                        token = ""
                    });
                }

                var data = await _usersService.GetUserDetails_PGSQL(request.email);

                bool isAuth = VerifyPassword(request.password, data.password); // , data.passwordsalt);

                if (!isAuth)
                {
                    return Unauthorized(new LoginResponse()
                    {
                        result = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString() + " " + "UnAuthorized",
                       statuscode = Convert.ToInt32(HttpStatusCode.OK).ToString(),
                        message = "Invalid credentials",
                        token = ""
                    });
                }

                var role = _usersService.GetUserRole(request.email); //To check whether the user id is admin or basic user

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name,request.email),
                    new Claim(ClaimTypes.Role, role)
                };

                var jwtResult = _jwtAuthManager.GenerateTokens(request.email, claims, DateTime.Now);
                _logger.LogInformation($"User [{request.email}] logged in the system."); //Temp log

                return Ok(new LoginResult
                {
                    result =  "Success",
                    statuscode = Convert.ToInt32(HttpStatusCode.OK).ToString(),
                    message = "Logged in successfully!",
                    UserName = request.email,
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
                        statuscode = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                    message = ex.Message,
                    token = ""
                });
            }
        }

        //Fetching details based on bearer token's username
        [Authorize]
        [HttpGet("employees")]
        public async Task<IActionResult> GetData_PGSQL()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new LoginResponse()
                    {
                        result = "Bad Request",
                        statuscode = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "",
                        token = ""
                    });
                }

                if (await _usersService.GetAllDetails_PGSQL(User.Identity?.Name) == null)
                {
                    return Unauthorized(new LoginResponse()
                    {
                        result =  "UnAuthorized",
                        statuscode = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString(),
                        message = "No data found for " + User.Identity?.Name,
                        token = ""
                    });
                }

                var data = await _usersService.GetAllDetails_PGSQL(User.Identity?.Name);
                _logger.LogInformation($"User [{User.Identity?.Name}] logged in the system.");

                return Ok(new Details_PGSQL
                {
                    result = "Success",
                    statuscode = Convert.ToInt32(HttpStatusCode.OK).ToString(),
                    message = "Details fetched successfully!",
                    empfirstname = data.empfirstname,
                    empnumber = data.empnumber,
                    doj = data.doj,
                    email = data.email,
                   personaldetails  = data.personaldetails,
                    token = ""
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error at GetData_PGSQL : [{ex.Message}], [{User.Identity?.Name}]");

                return NotFound(new LoginResponse()
                {
                    result = "Failed",
                    statuscode = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                    message = ex.Message,
                    token = ""
                });
            }
        }

        //Registering or Updating new password 
        [AllowAnonymous]
        [HttpPost("employee/change-password")]
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
                    result =  "Success",
                    statuscode = Convert.ToInt32(HttpStatusCode.OK).ToString(),
                    message = "Password registered successfully!",
                    token = ""
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error at GetData_PGSQL : [{ex.Message}], [{User.Identity?.Name}]");

                return NotFound(new LoginResponse()
                {
                    result = "Failed",
                    statuscode = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
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
            catch(Exception ex)
            {
                return ex.Message;
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
                {  // compare both hashes
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

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ResolvePay_WebAPI.Model.V1
{
    //Model for EF Core ORM
    public class LoginRequest_PGSQL
    {
        [Required(ErrorMessage = "Email field is required")]
        [JsonPropertyName("username")]
        public string email { get; set; }

        [Required(ErrorMessage = "Password field is required")]
        [JsonPropertyName("password")]
        public string password { get; set; }
    }

    public class GetTestDetails_PGSQL
    {
        [Key]
        [JsonPropertyName("empid")]
        public int empmasterid { get; set; }

        [JsonPropertyName("empname")]
        public string empnumber { get; set; }

        [JsonPropertyName("employeenumber")]
        public string empfirstname { get; set; }

        [JsonPropertyName("password")]
        public string password { get; set; }

        [JsonPropertyName("userid")]
        public string email { get; set; }
        [JsonPropertyName("doj")]
        public DateTime doj { get; set; }

        [JsonPropertyName("passwordSalt")]
        public string passwordsalt { get; set; }

    }

    public class ResultTestDetails_PGSQL
    {
        [JsonPropertyName("result")]
        public string result { get; set; }
        
        [JsonPropertyName("statuscode")]
        public string statuscode { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }

        [JsonPropertyName("employeenumber")]
        public string empnumber { get; set; }

        [JsonPropertyName("empname")]
        public string empfirstname { get; set; }

        [JsonPropertyName("userid")]
        public string email { get; set; }

        [JsonPropertyName("dateofjoining")]
        public string dateofjoining { get; set; }

        [JsonPropertyName("token")]
        public string token { get; set; }
    }
    //Classes for MySQL Connection
    public class LoginRequest
    {
        [Required]
        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }

    public class LoginResult
    {
        [JsonPropertyName("result")]
        public string result { get; set; }
        
        [JsonPropertyName("statuscode")]
        public string statuscode { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }

        [JsonPropertyName("username")]
        public string UserName { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("refreshToken")]
        public string refreshtoken { get; set; }

        [JsonPropertyName("token")]
        public string token { get; set; }
    }

    public class GetDetails
    {
        [JsonPropertyName("empid")]
        public string UserName { get; set; }

        [JsonPropertyName("empname")]
        public string Employeename { get; set; }

        [JsonPropertyName("employeenumber")]
        public string Employeenumber { get; set; }

        [JsonPropertyName("roleid")]
        public string roleid { get; set; }

        [JsonPropertyName("doj")]
        public string DOJ { get; set; }

        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }
    }
    public class GetTestDetails
    {
        [Key]
        [JsonPropertyName("empid")]
        public int ID { get; set; }

        [JsonPropertyName("empname")]
        public string EmpNumber { get; set; }

        [JsonPropertyName("photo")]
        public string Photo { get; set; }

        [JsonPropertyName("aadhaarnum")]
        public string AADHARNumber { get; set; }

        [JsonPropertyName("createdon")]
        public string CreatedOn { get; set; }

        [JsonPropertyName("modifiedon")]
        public string ModifiedOn { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

    }

    public class RefreshTokenRequest
    {
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; }
    }

    public class LoginResponse
    {
        public string result { get; set; }
        public string statuscode { get; set; }
        public string message { get; set; }
        public string token { get; set; }
    }
}
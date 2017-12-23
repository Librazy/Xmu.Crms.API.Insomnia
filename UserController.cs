﻿using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xmu.Crms.Shared.Exceptions;
using Xmu.Crms.Shared.Models;
using Xmu.Crms.Shared.Service;

namespace Xmu.Crms.Insomnia
{
    [Route("")]
    [Produces("application/json")]
    public class UserController : Controller
    {
        private readonly JwtHeader _header;
        private readonly ILoginService _loginService;
        private readonly IUserService _userService;

        public UserController(JwtHeader header, ILoginService loginService, IUserService userService)
        {
            _header = header;
            _loginService = loginService;
            _userService = userService;
        }

        [Authorize]
        [HttpGet("/me")]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var user = _userService.GetUserByUserId(User.Id());
                return Json(user, Utils.Ignoring("City", "Province", "Password"));
            }
            catch (UserNotFoundException)
            {
                return StatusCode(404, new {msg = "用户不存在"});
            }
        }

        [HttpPut("/me")]
        public IActionResult UpdateCurrentUser([FromBody] UserInfo updated)
        {
            try
            {
                _userService.UpdateUserByUserId(User.Id(), updated);
                return NoContent();
            }
            catch (UserNotFoundException)
            {
                return StatusCode(404, new {msg = "用户不存在"});
            }
        }

        [HttpGet("/signin")]
        public IActionResult SigninWechat([FromQuery] string code, [FromQuery] string state,
            [FromQuery(Name = "success_url")] string successUrl) => throw new NotSupportedException();

        [HttpPost("/signin")]
        public IActionResult SigninPassword([FromBody] UsernameAndPassword uap)
        {
            try
            {
                var user = _loginService.SignInPhone(new UserInfo {Phone = uap.Phone, Password = uap.Password});
                return Json(CreateSigninResult(user));
            }
            catch (PasswordErrorException)
            {
                return StatusCode(401, new {msg = "用户名或密码错误"});
            }
            catch (UserNotFoundException)
            {
                return StatusCode(404, new {msg = "用户不存在"});
            }
        }

        [HttpPost("/register")]
        public IActionResult RegisterPassword([FromBody] UsernameAndPassword uap)
        {
            try
            {
                var user = _loginService.SignUpPhone(new UserInfo {Phone = uap.Phone, Password = uap.Password});
                return Json(CreateSigninResult(user));
            }
            catch (PhoneAlreadyExistsException)
            {
                return StatusCode(409, new {msg = "手机已注册"});
            }
        }

        private SigninResult CreateSigninResult(UserInfo user) => new SigninResult
        {
            Id = user.Id,
            Name = user.Name,
            Type = user.Type.ToString().ToLower(),
            Jwt = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(_header,
                new JwtPayload(
                    null,
                    null,
                    new[]
                    {
                        new Claim("id", user.Id.ToString()),
                        new Claim("type", user.Type.ToString().ToLower())
                    },
                    null,
                    DateTime.Now.AddDays(7)
                )))
        };

        [HttpPost("/upload/avatar")]
        public IActionResult UploadAvatar(IFormFile file) =>
            Created("/upload/avatar.png", new {url = "/upload/avatar.png"});

        public class UsernameAndPassword
        {
            public string Phone { get; set; }
            public string Password { get; set; }
        }

        public class SigninResult
        {
            public long Id { get; set; }

            public string Name { get; set; }

            public string Type { get; set; }

            public string Jwt { get; set; }
        }
    }
}
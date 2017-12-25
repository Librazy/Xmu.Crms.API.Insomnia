using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xmu.Crms.Shared.Models;
using Xmu.Crms.Shared.Service;
using System.Linq;
using Xmu.Crms.Shared.Exceptions;
using Type = Xmu.Crms.Shared.Models.Type;

namespace Xmu.Crms.Insomnia
{
    [Route("")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SchoolController : Controller
    {
        private readonly ISchoolService _schoolService;
        private readonly IUserService _userService;
        [HttpGet("/school")]
        public IActionResult GetSchools([FromQuery] string city)
        {
            var schools = _schoolService.ListSchoolByCity(city);
            return Json(schools.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                province = t.Province,
                city = t.City,

            }));
        }

        [HttpGet("/school/{schoolId:long}")]
        public IActionResult GetSchoolById([FromRoute] long schoolId)
        {
            try
            {
                var schoolinfo = _schoolService.GetSchoolBySchoolId(schoolId);
                return Json(new { name = schoolinfo.Name, province = schoolinfo.Province, city = schoolinfo.City });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "学校ID输入格式有误" });
            }
        }



        /*
         * 这里school的查找有问题
         */
        [HttpPost("/school")]
        public IActionResult CreateSchool([FromBody] School newSchool)
        {
            var userlogin = _userService.GetUserByUserId(User.Id());
            if (userlogin.Type == Shared.Models.Type.Teacher)
            {
                var schoolId = _schoolService.InsertSchool(new School);
                return Created("/school/" + schoolId, newSchool);
            }
            return StatusCode(403, new { msg = "权限不足" });
        }
    }
}
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

namespace Xmu.Crms.Insomnia
{
    [Route("")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ClassController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly IClassService _classService;
        private readonly IUserService _userService;
        private readonly IFixGroupService _fixGroupService;
        private readonly ISeminarGroupService _seminarGroupService;
        
        private readonly JwtHeader _header;

        public ClassController(ICourseService courseService, IClassService classService, 
            IUserService userService, IFixGroupService fixGroupService, 
            ISeminarGroupService seminarGroupService, JwtHeader header)
        {
            _courseService = courseService;
            _classService = classService;
            _userService = userService;
            _fixGroupService = fixGroupService;
            _seminarGroupService = seminarGroupService;
            _header = header;
        }

        [HttpGet("/class")]
        public IActionResult GetUserClasses()
        {
            //List<ClassInfo> classes = new List<ClassInfo>();
            try
            {
                var classes = _classService.ListClassByUserId(User.Id());
                return Json(classes.Select(c => new {name = c.Name, site = c.Site}));
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "用户ID输入格式错误"});
            }
            
        }

        [HttpPost("/class")]
        public IActionResult CreateClass([FromBody] ClassInfo newClass)
        {
            var userlogin = _userService.GetUserByUserId(User.Id());
            if (userlogin.Type == Shared.Models.Type.Teacher)
            {
                var classId = _courseService.InsertClassById(newClass.Course.Id, newClass);
                return Created($"/class/{classId}", newClass);
            }
            return StatusCode(403, new {msg = "权限不足"});
        }

        [HttpGet("/class/{classId:long}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult GetClassById([FromRoute] long classId)
        {
            try
            {
                var classinfo = _classService.GetClassByClassId(classId);
                return Json(new {name = classinfo.Name, site = classinfo.Site});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "班级ID输入格式有误"});
            }
        }

        [HttpDelete("/class/{classId:long}")]
        public IActionResult DeleteClassById([FromRoute] long classId)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                if (userlogin.Type == Shared.Models.Type.Teacher)
                {
                    _classService.DeleteClassByClassId(classId);
                    return NoContent();
                }
                return StatusCode(403, new {msg = "权限不足"});
            }
            catch (ClassNotFoundException)
            {
                return StatusCode(404, new {msg = "班级不存在"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "班级ID输入格式有误"});
            }
            
        }

        [HttpPut("/class/{classId:long}")]
        public IActionResult UpdateClassById([FromRoute] long classId, [FromBody] ClassInfo updated)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                if (userlogin.Type == Shared.Models.Type.Teacher)
                {
                    _classService.UpdateClassByClassId(classId, updated);
                    return NoContent();
                }
                return StatusCode(403, new {msg = "权限不足"});
            }
            catch (ClassNotFoundException)
            {
                return StatusCode(404, new {msg = "班级不存在"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "班级ID输入格式有误"});
            }
            
        }

        [HttpGet("/class/{classId:long}/student")]
        public IActionResult GetStudentsByClassId([FromRoute] long classId,[FromQuery] string numBeginWith, string nameBeginWith)
        {
            try
            {
                var users = _userService.ListUserByClassId(classId, numBeginWith, nameBeginWith);
                return Json(users.Select(u => new {id = u.Id, name = u.Name, number = u.Number}));
            }
            catch (ClassNotFoundException)
            {
                return StatusCode(404, new {msg = "班级格式输入有误"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "班级ID输入格式有误"});
            }
            //return Json(new List<ClassInfo>());
        }

        [HttpPost("/class/{classId:long}/student")]
        public IActionResult SelectClass([FromRoute] long classId, [FromBody] UserInfo student)
        {
            try
            {
                var user = student;
                var userlogin = _userService.GetUserByUserId(User.Id());
                if (userlogin.Type == Shared.Models.Type.Student)
                {
                    if (User.Id() == student.Id)
                    {
                        _classService.InsertCourseSelectionById(student.Id, classId);
                        return Created($"/class/{classId}/student/{student.Id}",
                            new Dictionary<string, string> {["url"] = $"/class/{classId}/student/{student.Id}"});
                    }
                    return StatusCode(403, new {msg = "学生无法为他人选课"});
                }
                return StatusCode(403, new {msg = "权限不足"});
            }
            catch (ClassNotFoundException)
            {
                return StatusCode(404, new {msg = "班级不存在"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "班级ID输入格式有误"});
            }
        }

        [HttpDelete("/class/{classId:long}/student/{studentId:long}")]
        public IActionResult DeselectClass([FromRoute] long classId, [FromRoute] long studentId)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                if (userlogin.Type == Shared.Models.Type.Student)
                {
                    if (studentId == User.Id())
                    {
                        _classService.DeleteCourseSelectionById(studentId, classId);
                        return NoContent();
                    }
                    return StatusCode(403, new {msg = "用户无法为他人退课"});
                }
                return StatusCode(403, new {msg = "权限不足"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "错误的ID格式"});
            }
           
        }

        /*
         * 这两部分移除，放在SeminarController里面
         */
        //[HttpGet("/class/{classId:long}/attendance")]
        //public IActionResult GetAttendanceByClassId([FromRoute] long classId)
        //{
            
        //    return Json(new List<ClassInfo>());
        //}

        //[HttpPut("/class/{classId:long}/attendance/{studentId:long}")]
        //public IActionResult UpdateAttendanceByClassId([FromRoute] long classId, [FromRoute] long studentId,
        //    [FromBody] Location loc)
        //{
        //    return NoContent();
        //}

        [HttpGet("/class/{classId}/classgroup")]
        public IActionResult GetUserClassGroupByClassId([FromRoute] long classId)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                if (userlogin.Type == Shared.Models.Type.Student)
                {
                    var group = _fixGroupService.GetFixedGroupById(User.Id(), classId);
                    var leader = group.Leader;
                    var _members = _fixGroupService.ListFixGroupMemberByGroupId(group.Id);
                    //members.Add(group.Leader);
                    var result = Json(
                        new
                        {
                            leader = new
                            {
                                id = leader.Id,
                                name = leader.Name,
                                number = leader.Number
                            },
                            members = _members.Select(m => new
                            {
                                id = m.Id,
                                name = m.Name,
                                number = m.Number
                            })
                        });
                    return result;
                }
                return StatusCode(403, new {msg = "权限不足"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "课程ID格式错误"});
            }
        }

        /*
         * 这一部分删去，增加了新的方法
         */
        //[HttpPut("/class/{classId}/classgroup")]
        //public IActionResult UpdateUserClassGroupByClassId([FromRoute] long classId, [FromBody] FixGroup updated)
        //{
        //    try
        //    {
                
        //        return NoContent();
        //    }
        //    catch (ClassNotFoundException)
        //    {
        //        return StatusCode(404, new {msg = "不存在当前班级"});
        //    }
        //    catch (ArgumentException)
        //    {
        //        return StatusCode(400, new {msg = "班级ID格式有误"});
        //    }
        //}

        /*
         * 以下的四个controller为新添加的controller
         * 前两个方法模块组的同学说不会被调用，先不写
         */
        [HttpPut("/class/{classId}/classgroup/resign")]
        public IActionResult GroupLeaderResignByClassId([FromRoute] long classId, [FromBody] UserInfo student)
        {
            try
            {
                //var groupId = _fixGroupService.GetFixedGroupById()
                //_seminarGroupService.ResignLeaderById
                return NoContent();
            }
            catch (ClassNotFoundException)
            {
                return StatusCode(404, new {msg = "不存在当前班级"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "班级ID格式错误"});
            }
        }

        [HttpPut("/class/{classId}/classgroup/assign")]
        public IActionResult GroupLeaderAssignByClassId([FromRoute] long classId, [FromBody] UserInfo student)
        {
            try
            {
                //var groupId = _fixGroupService.GetFixedGroupById()
                //_seminarGroupService.ResignLeaderById
                return NoContent();
            }
            catch (ClassNotFoundException)
            {
                return StatusCode(404, new { msg = "不存在当前班级" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "班级ID格式错误" });
            }
        }

        [HttpPut("/class/{classId}/classgroup/add")]
        public IActionResult AddGroupMemberByClassId([FromRoute] long classId, [FromBody] UserInfo student)
        {
            try
            {
                var group = _fixGroupService.GetFixedGroupById(User.Id(), classId);
                _fixGroupService.InsertStudentIntoGroup(student.Id, group.Id);
                return NoContent();
            }
            catch (ClassNotFoundException)
            {
                return StatusCode(404, new { msg = "不存在当前班级" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "班级ID格式错误" });
            }
        }

        [HttpPut("/class/{classId}/classgroup/remove")]
        public IActionResult RemoveGroupMemberByClassId([FromRoute] long classId, [FromBody] UserInfo student)
        {
            try
            {
                var group = _fixGroupService.GetFixedGroupById(User.Id(), classId);
                _fixGroupService.DeleteFixGroupUserById(group.Id, student.Id);
                return NoContent();
            }
            catch (ClassNotFoundException)
            {
                return StatusCode(404, new { msg = "不存在当前班级" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "班级ID格式错误" });
            }
        }

        public class Attendance
        {
            public int NumPresent { get; set; }
            public List<UserInfo> Present { get; set; }
            public List<UserInfo> Late { get; set; }
            public List<UserInfo> Absent { get; set; }
        }


        public struct Location
        {
            public double Longitude { get; set; }
            public double Latitude { get; set; }
            public double Elevation { get; set; }
        }

        public class GroupUser
        {
            public bool IsLeader { get; set; }
            public long Id { get; set; }
            public string Name { get; set; }
            public string Number { get; set; }
        }
    }
}
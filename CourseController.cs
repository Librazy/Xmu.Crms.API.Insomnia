using System;
using System.Collections.Generic;
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
    public class CourseController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly IClassService _classService;
        private readonly IUserService _userService;
        private readonly ISeminarService _seminarService;

        public CourseController(ICourseService courseService, IClassService classService,
            IUserService userService, IFixGroupService fixGroupService,
            ISeminarGroupService seminarGroupService, 
            ISeminarService seminarService)
        {
            _courseService = courseService;
            _classService = classService;
            _userService = userService;
            _seminarService = seminarService;
        }

        /*
         * 无法计算每个课程里面学生的人数，需要多表联合查询，查询难度非常大
         */
        [HttpGet("/course")]
        public IActionResult GetUserCourses()
        {
            var userlogin = _userService.GetUserByUserId(User.Id());
            if (userlogin.Type == Type.Student)
            {
                var courses = _courseService.ListCourseByUserId(User.Id());
                foreach (var cs in courses)
                {
                    var numStuCount = 0;
                    var classinfo = _classService.ListClassByCourseId(cs.Id);
                    var numClassCount = classinfo.Count;
                    foreach (var cl in classinfo)
                    {
                        numStuCount += _userService.ListUserByClassId(cl.Id, "", "").Count;
                    }
                    //TODO
                }
            }
            return StatusCode(403, new {msg = "权限不足"});
        }

        [HttpPost("/course")]
        public IActionResult CreateCourse([FromBody] Course newCourse)
        {
            var userlogin = _userService.GetUserByUserId(User.Id());
            if (userlogin.Type != Type.Teacher)
            {
                return StatusCode(403, new {msg = "权限不足"});
            }
            _courseService.InsertCourseByUserId(User.Id(), newCourse);
            return Created($"/course/{newCourse.Id}", new {id = newCourse.Id});

        }

        [HttpGet("/course/{courseId:long}")]
        public IActionResult GetCourseById([FromRoute] long courseId)
        {
            try
            {
                var course = _courseService.GetCourseByCourseId(courseId);
                var result = Json(new
                {
                    id = course.Id,
                    name = course.Name,
                    description = course.Description,
                    teacherName = course.Teacher.Name,
                    teacherEmail = course.Teacher.Email
                });
                return result;
            }
            catch (CourseNotFoundException)
            {
                return StatusCode(404, new {msg = "未找到课程"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "错误的ID格式"});
            }
        }

        [HttpDelete("/course/{courseId:long}")]
        public IActionResult DeleteCourseById([FromRoute] long courseId)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                if (userlogin.Type == Type.Teacher)
                {
                    _courseService.DeleteCourseByCourseId(courseId);
                    return NoContent();
                }
                return StatusCode(403, new {msg = "权限不足"});
            }
            catch (CourseNotFoundException)
            {
                return StatusCode(404, new {msg = "未找到课程"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "课程ID格式错误"});
            }
        }

        [HttpPut("/course/{courseId:long}")]
        public IActionResult UpdateCourseById([FromRoute] long courseId, [FromBody] Course updated)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                if (userlogin.Type == Type.Teacher)
                {
                    _courseService.UpdateCourseByCourseId(courseId, updated);
                    return NoContent();
                }
                return StatusCode(403, new { msg = "权限不足" });
            }
            catch (CourseNotFoundException)
            {
                return StatusCode(404, new { msg = "未找到课程" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "课程ID格式错误" });
            }
        }

        [HttpGet("/course/{courseId:long}/class")]
        public IActionResult GetClassesByCourseId([FromRoute] long courseId)
        {
            try
            {
                var classes = _classService.ListClassByCourseId(courseId);
                return Json(classes.Select(c => new
                {
                    id = c.Id,
                    name = c.Name
                }));
            }
            catch (CourseNotFoundException)
            {
                return StatusCode(404, new { msg = "未找到课程" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "课程ID格式错误" });
            }
        }

        [HttpPost("/course/{courseId:long}/class")]
        public IActionResult CreateClassByCourseId([FromRoute] long courseId, [FromBody] ClassInfo newClass)
        {
            var userlogin = _userService.GetUserByUserId(User.Id());
            if (userlogin.Type != Type.Teacher)
            {
                return StatusCode(403, new {msg = "权限不足"});
            }

            var classId = _courseService.InsertClassById(courseId, newClass);
            return Created($"/class/{classId}", new {id = classId});
        }

        /*
         * 这里的embededGrade不知道应该用哪种方式展示
         * 成绩部分，个人认为不存在查询成绩的问题，不知道前端会用什么方法进行Json的接收
         */
        [HttpGet("/course/{courseId:long}/seminar")]
        public IActionResult GetSeminarsByCourseId([FromRoute] long courseId)
        {
            try
            {
                var seminars = _seminarService.ListSeminarByCourseId(courseId);
                return Json(seminars.Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    description = s.Description,
                    groupingMethod = (s.IsFixed==true)?"fixed":"random",
                    startTime = s.StartTime,
                    endTime = s.EndTime
                    //个人认为这里不存在查询成绩的问题
                }));
            }
            catch (CourseNotFoundException)
            {
                return StatusCode(404, new { msg = "未找到课程" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "课程ID格式错误" });
            }
        }

        [HttpPost("/course/{courseId:long}/seminar")]
        public IActionResult CreateSeminarByCourseId([FromRoute] long courseId, [FromBody] Seminar newSeminar)
        {
            var userlogin = _userService.GetUserByUserId(User.Id());
            if (userlogin.Type == Type.Teacher)
            {
                var seminarId = _seminarService.InsertSeminarByCourseId(courseId, newSeminar);
                return Created($"/seminar/{seminarId}", new {id = seminarId});
            }
            return StatusCode(403, new {msg = "权限不足"});
        }

        /*
         * 这一步找不到对应的和courseId相关的方法
         */
        [HttpGet("/course/{courseId:long}/grade")]
        public IActionResult GetGradeByCourseId([FromRoute] long courseId)
        {
            try
            {
                var seminars = _seminarService.ListSeminarByCourseId(courseId);
                List<SeminarGroup> result = new List<SeminarGroup>();
                foreach (var s in seminars)
                {
                    
                }
                return Json(result);
            }
            catch (CourseNotFoundException)
            {
                return StatusCode(404, new { msg = "未找到讨论课" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "课程ID格式错误" });
            }
        }
    }
}
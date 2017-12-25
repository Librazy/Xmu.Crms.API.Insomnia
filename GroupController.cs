﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xmu.Crms.Shared.Exceptions;
using Xmu.Crms.Shared.Models;
using System.Linq;
using Xmu.Crms.Shared.Service;

namespace Xmu.Crms.Insomnia
{
    [Route("")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GroupController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly IClassService _classService;
        private readonly IUserService _userService;
        private readonly IFixGroupService _fixGroupService;
        private readonly ISeminarGroupService _seminarGroupService;
        private readonly ISeminarService _seminarService;
        private readonly ITopicService _topicService;
        private readonly IGradeService _gradeService;

        private readonly JwtHeader _header;

        public GroupController(ICourseService courseService, IClassService classService,
            IUserService userService, IFixGroupService fixGroupService,
            ISeminarGroupService seminarGroupService, ITopicService topicService,
            ISeminarService seminarService, 
            IGradeService gradeService, JwtHeader header)
        {
            _courseService = courseService;
            _classService = classService;
            _userService = userService;
            _fixGroupService = fixGroupService;
            _seminarGroupService = seminarGroupService;
            _topicService = topicService;
            _seminarService = seminarService;
            _gradeService = gradeService;
            _header = header;
        }

        /*
         * Group表内没有相应的组名
         */
        [HttpGet("/group/{groupId:long}")]
        public IActionResult GetGroupById([FromRoute] long groupId)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                var group = _seminarGroupService.GetSeminarGroupByGroupId(groupId);
                var _members = _seminarGroupService.ListSeminarGroupMemberByGroupId(groupId);
                var _topics = _topicService.ListSeminarGroupTopicByGroupId(groupId);
                var report = "";
                return Json(new
                {
                    id = group.Id,
                    name = "1A1",
                    leader = new
                    {
                        id = group.Leader.Id,
                        name = group.Leader.Name
                    },
                    members = _members.Select(m => new
                    {
                        id = m.Id,
                        name = m.Name
                    }),
                    topics = _topics.Select(t => new
                    {
                        id = t.Topic.Id,
                        name = t.Topic.Name
                    }),
                    report = group.Report
                });
            }
            catch (GroupNotFoundException)
            {
                return StatusCode(404, new {msg = "未找到小组"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "组号格式错误"});
            }
        }

        /*
         * 没有找到相应的修改seminarGroup的方法，修改了SeminarGroup变为FixedGroup
         */
        [HttpPut("/group/{groupId:long}")]
        public IActionResult UpdateGroupById([FromRoute] long groupId, [FromBody] /*SeminarGroup*/FixGroup updated)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                if (userlogin.Type == Shared.Models.Type.Teacher)
                {
                    _fixGroupService.UpdateFixGroupByGroupId(groupId, updated);
                    return NoContent();
                }
                return StatusCode(403, new {msg = "权限不足"});
            }
            catch (GroupNotFoundException)
            {
                return StatusCode(404, new {msg = "没有找到组"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "组号格式错误"});
            }
        }

        [HttpPost("/group/{groupId:long}/topic")]
        public IActionResult SelectTopic([FromRoute] long groupId, [FromBody] Topic selected)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                if (userlogin.Type == Shared.Models.Type.Student)
                {
                    _seminarGroupService.InsertTopicByGroupId(groupId, selected.Id); 
                    return Created($"/group/{groupId}/topic/{selected.Id}", new Dictionary<string, string> {["url"] = $" /group/{groupId}/topic/{selected.Id}"});
                }
                return StatusCode(403, new {msg = "权限不足"});
            }
            catch (GroupNotFoundException)
            {
                return StatusCode(404, new {msg = "没有找到该课程"});
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new {msg = "组号格式错误"});
            }
        }

        [HttpDelete("/group/{groupId:long}/topic/{topicId:long}")]
        public IActionResult DeselectTopic([FromRoute] long groupId, [FromRoute] long topicId)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                if (userlogin.Type == Shared.Models.Type.Student)
                {
                    _topicService.DeleteSeminarGroupTopicById(groupId, topicId);
                    return NoContent();
                }
                return StatusCode(403, new { msg = "权限不足" });
            }
            catch (GroupNotFoundException)
            {
                return StatusCode(404, new { msg = "没有找到该课程" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "组号格式错误" });
            }
        }
        
        [HttpGet("/group/{groupId:long}/grade")]
        public IActionResult GetGradeByGroupId([FromRoute] long groupId)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());
                var group = _seminarGroupService.GetSeminarGroupByGroupId(groupId);
                var pGradeTopics = _topicService.ListSeminarGroupTopicByGroupId(groupId);
                return Json(new
                {
                    presentationGrade = pGradeTopics.Select(p => new
                    {
                        id = p.Id,
                        grade = p.PresentationGrade
                    }),
                    reportGrade = group.ReportGrade,
                    grade = group.FinalGrade
                });
            }
            catch (GroupNotFoundException)
            {
                return StatusCode(404, new { msg = "没有找到该课程" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "组号格式错误" });
            }
            
        }

        [HttpPut("/group/{groupId:long}/grade/report")]
        public IActionResult UpdateGradeByGroupId([FromRoute] long groupId, [FromBody] StudentScoreGroup updated)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());

                if (userlogin.Type == Shared.Models.Type.Teacher)
                {
                    if (updated.Grade != null)
                        _gradeService.UpdateGroupByGroupId(groupId, (int) updated.Grade);
                    return NoContent();
                }
                return StatusCode(403, new { msg = "权限不足" });
            }
            catch (GroupNotFoundException)
            {
                return StatusCode(404, new { msg = "没有找到该课程" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "组号格式错误" });
            }
            
        }

        [HttpPut("/group/{groupId:long}/grade/presentation/{studentId:long}")]
        public IActionResult SubmitStudentGradeByGroupId([FromBody] long groupId, [FromBody] long studentId,
            [FromBody] StudentScoreGroup updated)
        {
            try
            {
                var userlogin = _userService.GetUserByUserId(User.Id());

                if (userlogin.Type == Shared.Models.Type.Student)
                {
                    if (updated.Grade != null)
                        _gradeService.InsertGroupGradeByUserId(updated.SeminarGroupTopic.Topic.Id, updated.Student.Id,
                            groupId, (int) updated.Grade);
                    return NoContent();
                }
                return StatusCode(403, new { msg = "权限不足" });
            }
            catch (GroupNotFoundException)
            {
                return StatusCode(404, new { msg = "没有找到该课程" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "组号格式错误" });
            }
            
        }
    }
}
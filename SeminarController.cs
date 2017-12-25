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
    public class SeminarController : Controller
    {
        private readonly ISeminarService _seminarService;
        private readonly ITopicService _topicService;
        private readonly IUserService _userService;
        private readonly ISeminarGroupService _seminargroupService;
        public SeminarController(ISeminarService seminarService, ITopicService topicService)
        {
            _seminarService = seminarService;
            _topicService = topicService;
        }

        [HttpGet("/seminar/{seminarId:long}")]
        public IActionResult GetSeminarById([FromRoute] long seminarId)
        {
            try
            {
                return Json(_seminarService.GetSeminarBySeminarId(seminarId));
            }
            catch (SeminarNotFoundException)
            {
                return StatusCode(404, new { msg = "讨论课不存在" });

            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "讨论课ID输入格式有误" });
            }
        }
        
        [HttpPut("/seminar/{seminarId:long}")]
        public IActionResult UpdateSeminarById([FromRoute] long seminarId, [FromBody] Seminar updated)
        {
            var userlogin = _userService.GetUserByUserId(User.Id());
            if (userlogin.Type != Type.Teacher)
            {
                return StatusCode(403, new { msg = "权限不足" });
            }

            try
            {
                _seminarService.UpdateSeminarBySeminarId(seminarId, updated);
                return NoContent();
            }
            catch (SeminarNotFoundException)
            {
                return StatusCode(404, new { msg = "讨论课不存在" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "讨论课ID输入格式有误" });
            }
        }
        [HttpDelete("/seminar/{seminarId:long}")]
        public IActionResult DeleteSeminarById([FromRoute] long seminarId)
        {
            var userlogin = _userService.GetUserByUserId(User.Id());
            if (userlogin.Type != Type.Teacher)
            {
                return StatusCode(403, new { msg = "权限不足" });
            }
            try
            {
                _seminarService.DeleteSeminarBySeminarId(seminarId);
                return NoContent();
            }
            catch (SeminarNotFoundException)
            {
                return StatusCode(404, new { msg = "讨论课不存在" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "讨论课ID输入格式有误" });
            }
        }








        //serial  groupLeft未加
        [HttpGet("/seminar/{seminarId:long}/topic")]
        public IActionResult GetTopicsBySeminarId([FromRoute] long seminarId)
        {
            /*
             * "id": 257,
    "serial": "A",
    "name": "领域模型与模块",
    "description": "Domain model与模块划分",
    "groupLimit": 5,
    "groupMemberLimit": 6,
    "groupLeft": 2
             */

            try
            {
                var topics = _topicService.ListTopicBySeminarId(seminarId);
                return Json(topics.Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    description = t.Description,
                    groupLimit = t.GroupNumberLimit,
                    groupMemberLimit = t.GroupStudentLimit,

                }));
            }
            catch (SeminarNotFoundException)
            {
                return StatusCode(404, new { msg = "讨论课不存在" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "话题ID输入格式有误" });
            }
        }

        [HttpPut("/seminar/{seminarId:long}/topic")]
        public IActionResult CreateTopicBySeminarId([FromRoute] long seminarId, [FromBody] Topic newTopic)
        {
            var userlogin = _userService.GetUserByUserId(User.Id());
            if (userlogin.Type == Shared.Models.Type.Teacher)
            {
                var topicid = _topicService.InsertTopicBySeminarId(seminarId, newTopic);
                return Created("/topic/" + topicid, newTopic);
            }
            return StatusCode(403, new { msg = "权限不足" });
        }





        //没有小组成员 和 report
        [HttpGet("/seminar/{seminarId:long}/group")]
        public IActionResult GetGroupsBySeminarId([FromRoute] long seminarId)
        {
            try
            {
                var groups = _seminargroupService.ListSeminarGroupBySeminarId(seminarId);
                return Json(groups.Select(t => new
                {
                    id = t.Id,
                    leader = t.Leader,
                    presentationgrade = t.PresentationGrade,
                    reportgrade = t.ReportGrade,
                    finalgrade = t.FinalGrade,
                }));
            }
            catch (SeminarNotFoundException)
            {
                return StatusCode(404, new { msg = "讨论课不存在" });
            }
            catch (ArgumentException)
            {
                return StatusCode(400, new { msg = "讨论课ID输入格式有误" });
            }
        }
    }
}
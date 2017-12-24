using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xmu.Crms.Shared.Exceptions;
using Xmu.Crms.Shared.Models;
using Xmu.Crms.Shared.Service;
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
        }

        [HttpDelete("/seminar/{seminarId:long}")]
        public IActionResult DeleteSeminarById([FromRoute] long seminarId)
        {
            if (User.Type() != Type.Teacher)
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
        }

        [HttpPut("/seminar/{seminarId:long}")]
        public IActionResult UpdateSeminarById([FromRoute] long seminarId, [FromBody] Seminar updated)
        {
            if (User.Type() != Type.Teacher)
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
        }

        [HttpGet("/seminar/{seminarId:long}/topic")]
        public IActionResult GetTopicsBySeminarId([FromRoute] long seminarId)
        {
            try
            {
                return Json(_topicService.ListTopicBySeminarId(seminarId));
            }
            catch (SeminarNotFoundException)
            {
                return StatusCode(404, new { msg = "讨论课不存在" });
            }
        }

        [HttpPut("/seminar/{seminarId:long}/topic")]
        public IActionResult CreateTopicBySeminarId([FromRoute] long seminarId, [FromBody] Seminar newSeminar)
        {
            return Created("/topic/1", newSeminar);
        }

        [HttpGet("/seminar/{seminarId:long}/group")]
        public IActionResult GetGroupsBySeminarId([FromRoute] long seminarId)
        {
            return Json(new List<SeminarGroup>(), Utils.Ignoring("Group*", "Members", "Leader", "Report", "Grade"));
        }
    }
}
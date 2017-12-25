using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xmu.Crms.Shared.Models;
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

        private readonly JwtHeader _header;

        public GroupController(ICourseService courseService, IClassService classService,
            IUserService userService, IFixGroupService fixGroupService,
            ISeminarGroupService seminarGroupService,
            ISeminarService seminarService, JwtHeader header)
        {
            _courseService = courseService;
            _classService = classService;
            _userService = userService;
            _fixGroupService = fixGroupService;
            _seminarGroupService = seminarGroupService;
            _seminarService = seminarService;
            _header = header;
        }

        [HttpGet("/group/{groupId:long}")]
        public IActionResult GetGroupById([FromRoute] long groupId)
        {
            var gs = new List<SeminarGroup>
            {
                new SeminarGroup(),
                new SeminarGroup(),
                new SeminarGroup()
            };
            return Json(gs[(int) groupId]);
        }

        [HttpPut("/group/{groupId:long}")]
        public IActionResult UpdateGroupById([FromRoute] long groupId, [FromBody] SeminarGroup updated)
        {

            return NoContent();
        }

        [HttpPost("/group/{groupId:long}/topic")]
        public IActionResult SelectTopic([FromRoute] long groupId, [FromBody] Topic selected)
        {
            return Created("/group/1/topic/1", new Dictionary<string, string> {["url"] = " /group/1/topic/1"});
        }

        [HttpDelete("/group/{groupId:long}/topic/{topicId:long}")]
        public IActionResult DeselectTopic([FromRoute] long groupId, [FromRoute] long topicId)
        {
            return NoContent();
        }

        [HttpGet("/group/{groupId:long}/grade")]
        public IActionResult GetGradeByGroupId([FromRoute] long groupId)
        {
            return Json(new StudentScoreGroup());
        }

        [HttpPut("/group/{groupId:long}/grade/report")]
        public IActionResult UpdateGradeByGroupId([FromRoute] long groupId, [FromBody] StudentScoreGroup updated)
        {
            return NoContent();
        }

        [HttpPut("/group/{groupId:long}/grade/presentation/{studentId:long}")]
        public IActionResult SubmitStudentGradeByGroupId([FromBody] long groupId, [FromBody] long studentId,
            [FromBody] StudentScoreGroup updated)
        {
            return NoContent();
        }
    }
}
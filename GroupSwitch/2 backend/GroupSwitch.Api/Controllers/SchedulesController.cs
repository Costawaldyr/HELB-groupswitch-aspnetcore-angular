using GroupSwitch.Api.Dtos;
using GroupSwitch.Api.Mapping;
using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using Microsoft.AspNetCore.Mvc;

namespace GroupSwitch.Api.Controllers
{
    [Route("api/schedules")]
    [ApiController]
    public class SchedulesController : ControllerBase
    {
        private const string BlankGroupMessage = "Le paramètre group est obligatoire.";

        private readonly IScheduleQueryService _scheduleQueryService;
        private readonly ScheduleDtoMapper _scheduleDtoMapper;

        public SchedulesController(IScheduleQueryService scheduleQueryService, ScheduleDtoMapper scheduleDtoMapper)
        {
            _scheduleQueryService = scheduleQueryService;
            _scheduleDtoMapper = scheduleDtoMapper;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScheduleDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<IActionResult> GetSchedule(
            [FromQuery] string? group,
            [FromQuery] int? firstWeek,
            [FromQuery] int? lastWeek,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                return BadRequest(BlankGroupMessage);
            }

            try
            {
                var overview = await _scheduleQueryService.GetScheduleAsync(group, firstWeek, lastWeek, cancellationToken);

                return Ok(_scheduleDtoMapper.ToDto(overview));
            }
            catch (InvalidWeekRangeException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (UnknownGroupException exception)
            {
                return NotFound(exception.Message);
            }
        }
    }
}

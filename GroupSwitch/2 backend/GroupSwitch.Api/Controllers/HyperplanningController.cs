using GroupSwitch.Api.Dtos;
using GroupSwitch.Api.Mapping;
using GroupSwitch.Business.Scraping.Interfaces;
using GroupSwitch.Business.Scraping.Models;
using Microsoft.AspNetCore.Mvc;

namespace GroupSwitch.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HyperplanningController : ControllerBase
    {
        private const int LatestRunsCount = 10;
        private const string GroupParameter = "group";
        private const string BlankGroupMessage = "Le paramètre group ne peut pas être vide.";
        private const string NotRunningMessage = "Cet import n'est pas en cours.";

        private readonly IImportQueueService _importQueueService;
        private readonly IHyperplanningImportService _hyperplanningImportService;
        private readonly IImportDtoMapper _importDtoMapper;

        public HyperplanningController(IImportQueueService importQueueService, IHyperplanningImportService hyperplanningImportService, IImportDtoMapper importDtoMapper)
        {
            _importQueueService = importQueueService;
            _hyperplanningImportService = hyperplanningImportService;
            _importDtoMapper = importDtoMapper;
        }

        [HttpPost]
        [Route("imports", Name = nameof(StartImport))]
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(ImportRunDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(string))]
        public async Task<IActionResult> StartImport(
            [FromQuery] string? group,
            [FromQuery] int? firstWeek,
            [FromQuery] int? lastWeek,
            CancellationToken cancellationToken)
        {
            if (Request.Query.ContainsKey(GroupParameter) && string.IsNullOrWhiteSpace(group))
            {
                return BadRequest(BlankGroupMessage);
            }

            try
            {
                var run = await _hyperplanningImportService.StartAsync(new ImportRequest(group, firstWeek, lastWeek), cancellationToken);
                _importQueueService.Enqueue(new QueuedImport(run.Id, run.RequestedGroupName));

                return AcceptedAtAction(nameof(GetImport), new { id = run.Id }, _importDtoMapper.ToDto(run));
            }
            catch (UnknownGroupException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (InvalidWeekRangeException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (ImportAlreadyRunningException exception)
            {
                return Conflict(exception.Message);
            }
        }

        [HttpPost]
        [Route("imports/{id:int}/cancel", Name = nameof(CancelImport))]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(string))]
        public async Task<IActionResult> CancelImport(int id)
        {
            try
            {
                var isCancellationRecorded = await _hyperplanningImportService.RequestCancellationAsync(id);

                return isCancellationRecorded ? Accepted() : Conflict(NotRunningMessage);
            }
            catch (ArgumentException exception)
            {
                return NotFound(exception.Message);
            }
        }

        [HttpGet]
        [Route("imports/{id:int}", Name = nameof(GetImport))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ImportDetailsDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<IActionResult> GetImport(int id)
        {
            try
            {
                var run = await _hyperplanningImportService.GetAsync(id);

                return Ok(_importDtoMapper.ToDetailsDto(run));
            }
            catch (ArgumentException exception)
            {
                return NotFound(exception.Message);
            }
        }

        [HttpGet]
        [Route("imports", Name = nameof(GetLatestImports))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ImportRunDto>))]
        public async Task<IActionResult> GetLatestImports()
        {
            var runs = await _hyperplanningImportService.GetLatestAsync(LatestRunsCount);

            return Ok(runs.Select(_importDtoMapper.ToDto));
        }

        [HttpGet]
        [Route("groups", Name = nameof(GetGroups))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetGroups(CancellationToken cancellationToken)
        {
            var groupNames = await _hyperplanningImportService.GetGroupNamesAsync(cancellationToken);

            return Ok(groupNames);
        }
    }
}

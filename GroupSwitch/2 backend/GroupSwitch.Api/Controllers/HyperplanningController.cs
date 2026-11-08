using GroupSwitch.Api.Dtos;
using GroupSwitch.Api.Mapping;
using GroupSwitch.Business.Interfaces;
using GroupSwitch.Business.Models;
using GroupSwitch.Business.Services;
using GroupSwitch.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace GroupSwitch.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HyperplanningController : ControllerBase
    {
        private const int LatestRunsCount = 10;

        private const string BlankGroupMessage = "Le paramètre group ne peut pas être vide.";

        private readonly IImportQueue _importQueue;
        private readonly IImportService _importService;
        private readonly IImportDtoMapper _importDtoMapper;

        public HyperplanningController(IImportQueue importQueue, IImportService importService, IImportDtoMapper importDtoMapper)
        {
            _importQueue = importQueue;
            _importService = importService;
            _importDtoMapper = importDtoMapper;
        }

        [HttpPost]
        [Route("imports", Name = nameof(StartImport))]
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(ImportRunDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(string))]
        public async Task<IActionResult> StartImport([FromQuery] string? group = null)
        {
            if (Request.Query.ContainsKey("group") && string.IsNullOrWhiteSpace(group))
            {
                return BadRequest(BlankGroupMessage);
            }

            try
            {
                var run = await _importService.StartAsync();
                _importQueue.Enqueue(new ImportRequest(run.Id, group));

                return AcceptedAtAction(nameof(GetImport), new { id = run.Id }, _importDtoMapper.ToDto(run));
            }
            catch (ImportAlreadyRunningException exception)
            {
                return Conflict(exception.Message);
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
                var run = await _importService.GetAsync(id);

                return Ok(ToDetailsDto(run));
            }
            catch (ArgumentException exception)
            {
                return NotFound(exception.Message);
            }
        }

        [HttpGet]
        [Route("imports", Name = nameof(GetLatestImports))]
        public async Task<IActionResult> GetLatestImports()
        {
            var runs = await _importService.GetLatestAsync(LatestRunsCount);

            return Ok(runs.Select(ToDto));
        }



        private ImportRunDto ToDto(ImportRun run)
        {
            return new ImportRunDto(run.Id, run.StartedAt, run.FinishedAt, run.Status.ToString(), run.GroupsImported, run.SessionsImported);
        }

        private ImportDetailsDto ToDetailsDto(ImportRun run)
        {
            var log = run.LogEntriesList
                .OrderBy(entry => entry.Id)
                .Select(entry => new ImportLogDto(entry.Moment, entry.Level, entry.Message));

            return new ImportDetailsDto(ToDto(run), log);
        }
    }
}

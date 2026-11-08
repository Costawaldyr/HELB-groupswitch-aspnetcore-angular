using GroupSwitch.Api.Dtos;
using GroupSwitch.Data.Entities;

namespace GroupSwitch.Api.Mapping
{
    public interface IImportDtoMapper
    {
        ImportRunDto ToDto(ImportRun run);
        ImportDetailsDto ToDetailsDto(ImportRun run);
    }
}
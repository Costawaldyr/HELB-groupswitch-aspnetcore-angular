namespace GroupSwitch.Business.Scraping.Interfaces
{
    public interface IImportLogService
    {
        Task InfoAsync(int importRunId, string message);

        Task ErrorAsync(int importRunId, string message);
    }
}
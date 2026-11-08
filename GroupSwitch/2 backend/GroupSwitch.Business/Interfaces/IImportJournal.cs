namespace GroupSwitch.Business.Interfaces
{
    public interface IImportJournal
    {
        Task InfoAsync(int importRunId, string message);

        Task ErrorAsync(int importRunId, string message);
    }
}
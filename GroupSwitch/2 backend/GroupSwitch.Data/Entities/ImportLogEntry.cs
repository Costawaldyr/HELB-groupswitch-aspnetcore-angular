using System.ComponentModel.DataAnnotations;


namespace GroupSwitch.Data.Entities
{
    public class ImportLogEntry
    {
        public const int MessageMaxLength = 500;


        public int Id { get; set; }
        public DateTime Moment { get; set; }

        [MaxLength(50)]
        public string Level { get; set; } = string.Empty;

        [MaxLength(MessageMaxLength)]
        public string Message { get; set; } = string.Empty;

        public ImportRun? ImportRun { get; set; }
        public int ImportRunId { get; set; }
    }
}

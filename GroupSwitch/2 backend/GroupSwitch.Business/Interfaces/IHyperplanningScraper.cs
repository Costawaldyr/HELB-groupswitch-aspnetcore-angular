using GroupSwitch.Business.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Business.Interfaces
{
    public record WeekResponse(string GroupName, int Week, string Json);

    public interface IHyperplanningScraper
    {
        IAsyncEnumerable<WeekResponse> ScrapeAsync(Func<string, bool> isGroupWanted, Func<int, bool> isWeekWanted);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupSwitch.Business.Scraping
{
    public class QuadrimesterFilters
    {
        public const int FirstQuadrimesterStartWeek = 1;
        public const int FirstQuadrimesterEndWeek = 14;

        public static bool IsInFirstQuadrimester(int week)
        {
            return week >= FirstQuadrimesterStartWeek && week <= FirstQuadrimesterEndWeek;
        }
    }
}

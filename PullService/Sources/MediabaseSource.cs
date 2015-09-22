using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PullService.Sources
{
    public class MediabaseSource : BaseSource
    {
        public MediabaseSource() : base() { }

        protected override SortedList<int, SearchQueue> ParseDocument(HtmlAgilityPack.HtmlDocument document)
        {
            SortedList<int, SearchQueue> results = new SortedList<int, SearchQueue>();

            foreach (var tr in document.DocumentNode.SelectNodes("tbody/tr[position()>1"))
            {

            }

            return results;
        }
    }
}

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PullService
{
    public abstract class BaseSource
    {
        private const string mergeSql = @"MERGE dbo.SearchQueue AS Target
USING ( VALUES(@Station, @Title, @Artist)) AS Source (Station, Title, Artist)
ON Target.Station = Source.Station AND Target.Title = Source.Title AND Target.Artist = Source.Artist
WHEN NOT MATCHED THEN
	INSERT (Station, Title, Artist, DateAdded)
	VALUES (@Station, @Title, @Artist, GetDate());";

        private HttpClient client = new HttpClient();

        public BaseSource()
        {
            client.Timeout = new TimeSpan(0, 0, 30);
            client.MaxResponseContentBufferSize = 256000;
            client.DefaultRequestHeaders.Add("User-Agent", "GrooveyFM/1.0 (http://grooveyfm.com)");
        }

        public async Task RunCheck(Source source, string connectionString)
        {
            var playlists = await GetSongs(source.Url);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(mergeSql, conn))
                {
                    foreach (var playlist in playlists.OrderBy(t => t.Key))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("Station", playlist.Value.Station);
                        cmd.Parameters.AddWithValue("Title", playlist.Value.Title);
                        cmd.Parameters.AddWithValue("Artist", playlist.Value.Artist);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        protected async Task<SortedList<int, SearchQueue>> GetSongs(string url)
        {
            var page = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);
            var stream = await page.Content.ReadAsStreamAsync();

            HtmlDocument doc = new HtmlDocument();
            doc.Load(stream);

            return ParseDocument(doc);
        }

        protected abstract SortedList<int, SearchQueue> ParseDocument(HtmlDocument document);
    }
}

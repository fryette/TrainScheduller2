using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StationParser
{
    public class MainClass
    {
        private static readonly WebClient _client = new WebClient();
        private static string _alphabet = "1234567890абвгдеёжзийклмнопрстуфхцчшщъыьэюя";

        public static void Main(string[] args)
        {
            GetStationAsync();
        }

        private static async Task<string> GetStationAsync()
        {
            var result = new List<TrainStation>();

            for (int i = 0; i < _alphabet.Length; i++)
            {
                result.AddRange(await GetStationAsync(_alphabet[i].ToString()));

                for (var j = 0; j < _alphabet.Length; j++)
                {
                    result.AddRange(await GetStationAsync($"{_alphabet[i]}{_alphabet[j]}"));
                }
            }

            result = result.Distinct(new ItemEqualityComparer()).OrderBy(x=>x.Value).ToList();

            foreach (var item in result)
            {
                item.Country = item.Label_Tail.Trim().Split(' ').LastOrDefault().Trim();
            }

            var groups = result.GroupBy(x => x.Country);

            return null;
        }

        private static async Task<List<TrainStation>> GetStationAsync(string initialString)
        {
            var responseString = _client.DownloadString($"https://rasp.rw.by/ru/ajax/autocomplete/search/?term={initialString}");

            var items = JsonConvert.DeserializeObject<List<TrainStation>>(responseString);

            if (items.Any())
            {
                initialString += initialString[0];
                items.AddRange(await GetStationAsync(initialString));
            }

            return items;
        }
    }

    public class ItemEqualityComparer : IEqualityComparer<TrainStation>
    {
        public bool Equals(TrainStation x, TrainStation y)
        {
            // Two items are equal if their keys are equal.
            return x.Prefix == y.Prefix &&
                    x.Label == y.Label &&
                    x.Label_Tail == y.Label_Tail &&
                    x.Value == y.Value &&
                    x.Gid == y.Gid &&
                    x.Lon == y.Lon &&
                    x.Lat == y.Lat &&
                    x.Ecp == y.Ecp &&
                    x.Otd == y.Otd;
        }

        public int GetHashCode(TrainStation obj)
        {
            return obj.Value.GetHashCode();
        }
    }

    public class TrainStation
    {
        public string Prefix { get; set; }
        public string Label { get; set; }
        public string Label_Tail { get; set; }
        public string Value { get; set; }
        public string Gid { get; set; }
        public string Lon { get; set; }
        public string Lat { get; set; }
        public string Exp { get; set; }
        public string Ecp { get; set; }
        public string Otd { get; set; }
        public string Country { get; set; }
    }
}

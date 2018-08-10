using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using static TrainScheduller2.Parser.TrainModel;

namespace TrainScheduller2.Parser
{
    public class TrainParser
    {
        private Dictionary<string, PlaceType> _russianDictionary = new Dictionary<string, PlaceType>
        {
            {"Сидячий", PlaceType.SEDENTARY},
            {"Общий", PlaceType.GENERAL},
            {"Плацкартный", PlaceType.SECOND_CLASS},
            {"Купе", PlaceType.COUPE},
            {"СВ", PlaceType.LUXERY},
        };

        private Dictionary<string, TrainAdvantages> _russianAdvantageDictionary = new Dictionary<string, TrainAdvantages>
        {
            {"ФП", TrainAdvantages.CLASS_TRAIN},
            {"ЭР", TrainAdvantages.ELECTRONIC_REGISTRATION},
            {"СП", TrainAdvantages.FAST_TRAIN},
        };
        public TrainParser()
        {
        }

        public async Task ParsData()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var address = "https://rasp.rw.by/m/ru/route/?from=%D0%9C%D0%B8%D0%BD%D1%81%D0%BA&from_exp=2100000&from_esr=140210&to=%D0%91%D1%80%D0%B5%D1%81%D1%82&to_exp=2100200&to_esr=130007&date=2018-08-22#results";
            var document = await BrowsingContext.New(config).OpenAsync(address);

            var trains = CreateInitialTrainList(document);
            FillPlaceInfoData(document, ref trains);
            FillTrainAdvantages(document, ref trains);
        }

        private IReadOnlyList<TrainModel> CreateInitialTrainList(IDocument document)
        {
            var elements = document.QuerySelectorAll("div.list_items a").ToList();
            var trains = new List<TrainModel>(elements.Count);

            foreach (var element in elements)
            {
                var train = new TrainModel();

                var times = GetStartEndTime(element);
                var title = element.QuerySelector("div.list_content div")?.TextContent;
                var schedule = element.QuerySelector("div.list_content span")?.TextContent;

                train.StartTime = times?.Item1;
                train.EndTime = times?.Item1;
                train.Schedule = schedule;
                train.Title = title;

                trains.Add(train);
            }

            return trains;
        }

        public void FillTrainAdvantages(IDocument document, ref IReadOnlyList<TrainModel> trains)
        {
            TrainAdvantages ParseAdvantage(IElement element)
            {
                if (_russianAdvantageDictionary.TryGetValue(element.TextContent, out var advantage))
                {
                    return advantage;
                }

                return TrainAdvantages.UNDEFINED;
            }

            var elements = document.QuerySelectorAll("div.b-legend").ToList();

            for (var i = 0; i < elements.Count; i++)
            {
                var advantages = elements[i].QuerySelectorAll("i");

                var result = advantages.Select(ParseAdvantage).Where(x => x != TrainAdvantages.UNDEFINED);

                if (result.Any())
                {
                    trains[i].Advantages = new List<TrainAdvantages>(result);
                }
            }
        }

        private void FillPlaceInfoData(IDocument document, ref IReadOnlyList<TrainModel> trains)
        {
            var elements = document.QuerySelectorAll("table.places_table").ToList();

            for (var i = 0; i < elements.Count; i++)
            {
                var places = elements[i].QuerySelectorAll("tr").ToList();

                var placesInfo = places.Select(ParsePlaceInformation).Where(x => x != null).ToList();

                if (placesInfo.Any())
                {
                    trains[i].Places = new List<PlaceInformation>(placesInfo);
                }
            }
        }

        private PlaceInformation ParsePlaceInformation(IElement element)
        {
            var parts = element.QuerySelectorAll("td");

            if (parts.Length != 3)
            {
                return null;
            }

            var result = new PlaceInformation();

            var placeType = PlaceType.UNDEFINED;

            if (_russianDictionary.TryGetValue(parts[0].TextContent, out placeType))
            {
                result.Type = placeType;
            }

            if (int.TryParse(parts[1].TextContent, out var availableSeats))
            {
                result.AvailableSeats = availableSeats;
            }

            if (!string.IsNullOrWhiteSpace(parts[2].TextContent))
            {
                result.Price = parts[2].TextContent;
            }

            return result;
        }

        private Tuple<DateTime, DateTime> GetStartEndTime(IElement element)
        {
            var times = element.QuerySelectorAll("div.list_time div");

            if (times.Length == 2 && DateTime.TryParse(times[0].TextContent.Replace("<br />", " "), out var start) && DateTime.TryParse(times[1].TextContent.Replace("<br />", " "), out var end))
            {
                return new Tuple<DateTime, DateTime>(start, end);
            }

            return null;
        }

        // public IReadOnlyList<TrainModel> FillInitialData(IDocument document)
        // {
        //     var elements = document.QuerySelectorAll("div.t-item").ToList();

        //     var resultList = new List<TrainModel>(elements.Count);

        //     foreach (var item in elements)
        //     {
        //         var title = item.QuerySelector("div.name").TextContent.RemoveSpaces();
        //         var type = item.QuerySelector("div.type").TextContent.RemoveSpaces();
        //         var num = item.QuerySelector("div.num i")?.TextContent.RemoveSpaces();

        //         var platformInformation = item.QuerySelectorAll("div.info-block i").ToArray();
        //         int platform = -1;
        //         int way = -1;

        //         if (platformInformation.Any() && platformInformation.Length == 2)
        //         {
        //             Int32.TryParse(platformInformation[0].TextContent, out platform);
        //             Int32.TryParse(platformInformation[1].TextContent, out way);
        //         }

        //         var infoBlock = item.QuerySelector("div.info-block");

        //         if (item != null)
        //         {
        //             resultList.Add(new TrainModel { Title = title, Type = type, Numeration = num, Platform = platform, Way = way });
        //         }
        //     }

        //     return resultList;
        // }
    }

    public class TrainModel
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public string Numeration { get; set; }
        public int Platform { get; set; }
        public int Way { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Schedule { get; set; }
        public IList<PlaceInformation> Places { get; set; }
        public IList<TrainAdvantages> Advantages { get; set; }

        public class PlaceInformation
        {
            public PlaceType Type { get; set; }
            public int AvailableSeats { get; set; }

            public string Price { get; set; }
        }

        public enum TrainAdvantages
        {
            UNDEFINED,
            ELECTRONIC_REGISTRATION,
            FAST_TRAIN,
            CLASS_TRAIN,
        }

        public enum PlaceType
        {
            UNDEFINED,
            SEDENTARY,
            GENERAL,
            SECOND_CLASS,
            COUPE,
            LUXERY
        }
    }

    public static class StringExtensions
    {
        public static string RemoveSpaces(this string str) => Regex.Replace(str, @"\s+", " ").Trim();

        public static string RemoveSpe1cialCharacters(this string str)
        {
            return Regex.Replace(str, @"\s+", " ").Trim();
        }
    }
}

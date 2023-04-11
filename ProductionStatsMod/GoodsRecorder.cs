using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using HarmonyLib;
using UnityEngine;
using Eremite.Buildings;
using Eremite.Model;
using Eremite.Services;
using Eremite;
using System.Diagnostics;
using Eremite.Model.State;
using Newtonsoft.Json;
using System.IO;
using Eremite.Controller;

namespace ProductionStatsMod
{
    public class Utils : Serviceable
    {
        public static GoodModel GetGoodModel(Good good)
        {
            return Serviceable.Settings.GetGood(good);
        }
        public static GoodModel GetGoodModel(string goodName)
        {
            return Serviceable.Settings.GetGood(goodName);
        }

        public static GameDate GetGameDate()
        {
            return Serviceable.CalendarService.GameDate;
        }
        public static string GetSaveFolder()
        {
            return Serviceable.ProfilesService.GetFolderPath();
        }
    }

    [Serializable]
    public struct GoodChange
    {
        public string GoodName;
        public string GoodCategory;
        public int GoodDelta;

        public string ProductionCategory;

        public string BuildingName;
        public int BuildingId;

        public GameDate Date;

        public GoodChange(Good good, int multiplier, string productionCategory, Building building, GameDate? date = null)
        {
            GoodModel goodModel = Utils.GetGoodModel(good);
            GoodName = goodModel.displayName.Text;
            GoodCategory = goodModel.category.displayName.Text;
            GoodDelta = good.amount * multiplier;

            ProductionCategory = productionCategory;

            if (building != null)
            {
                BuildingName = building.BuildingModel.Name;
                BuildingId = building.Id;
            }
            else
            {
                BuildingName = "";
                BuildingId = -1;
            }
            if (date != null) 
                Date = date.Value;
            else
                Date = Utils.GetGameDate();
        }

        public override string ToString()
        {
            string s = $"GoodChange: \n\t" +
                $"{GoodCategory}, {GoodName}, {GoodDelta}\n\t" +
                $"{ProductionCategory}";
            if (BuildingName != "")
                s += $"\n\t{BuildingId} {BuildingName}";

            return s;
        }
    }

    [Serializable]
    public class ProductionStats
    {
        public List<GoodChange> _GoodsTimeline = new List<GoodChange>();
        public List<Good> _ProductionSkips = new List<Good>();
        public bool RecordedSinceGameStart;
        public string ModVersion = ProductionStatsMod.pluginVersion;

        public ProductionStats(bool recordedSinceGameStart)
        {
            RecordedSinceGameStart = recordedSinceGameStart;
        }

        public void AddGoodChange(Good good, int multiplier, Building building = null)
        {
            for (int i = 0; i < _ProductionSkips.Count; ++i)
            {
                if (multiplier == 1 && _ProductionSkips[i] == good)
                {
                    Console.WriteLine("Skipping good production event. " + good);
                    _ProductionSkips.RemoveAt(i);
                    return;
                }
            }
            GoodChange goodChange = new GoodChange(good, multiplier, "Unspecified", building);
            if (goodChange.GoodDelta == 0) return;
            Console.WriteLine(goodChange);
            _GoodsTimeline.Add(goodChange);
        }

        public void SkipNextProduction(Good good)
        {
            _ProductionSkips.Add(good);
        }

        public string GetTable(string name)
        {
            string tableString = $"Y\tProd\t\tCons{(RecordedSinceGameStart ? "\t\tStart" : "")}";
            int currentYear = Utils.GetGameDate().year;
            for (int i = 0; i < 4; ++i)
            {
                int year = currentYear - i;
                if (year < 1) break;
                GameDate startDate = new GameDate(year, Season.Drizzle, SeasonQuarter.First);
                GameDate endDate = new GameDate(year, Season.Storm, SeasonQuarter.Fourth);
                GameDate lastYearEnd = new GameDate(year - 1, Season.Storm, SeasonQuarter.Fourth);
                int producedGoods = GetGoodDeltaBetween(startDate, endDate, name, true);
                int consumedGoods = GetGoodDeltaBetween(startDate, endDate, name, false);
                int startGoods = GetGoodStorageAt(lastYearEnd, name);

                tableString += $"\n{year}" +
                    $"\t+{producedGoods}" +
                    $"{(producedGoods >= 100 ? "\t" : "\t\t")}{(consumedGoods == 0 ? "-" : "")}{consumedGoods}";
                if (RecordedSinceGameStart)
                    tableString += $"{(consumedGoods <= -1000 ? "\t" : "\t\t")}{startGoods}";
            }
            if (currentYear == 1)
            {
                tableString += "\n.";
            }
            return tableString;
        }
        
        public int GetGoodStorageAt(GameDate date, string name)
        {
            int sum = 0;
            foreach (GoodChange goodChange in _GoodsTimeline)
            {
                if (goodChange.Date > date) break;
                if (goodChange.GoodCategory == name || goodChange.GoodName == name)
                {
                    sum += goodChange.GoodDelta;
                }
            }
            return sum;
        }
        
        public int GetGoodDeltaBetween(GameDate startDate, GameDate endDate, string name, bool countProduction)
        {
            int sum = 0;
            foreach (GoodChange goodChange in _GoodsTimeline)
            {
                if (goodChange.Date < startDate) continue;
                if (goodChange.Date > endDate) break;
                if (goodChange.GoodCategory != name && goodChange.GoodName != name) continue;

                // Either count all positive or negative changes.
                if ((countProduction && goodChange.GoodDelta > 0) || 
                    (!countProduction && goodChange.GoodDelta < 0))
                {
                    sum += goodChange.GoodDelta;
                }
            }
            return sum;
        }

    }

    public class GoodMonitor
    {
        public static ProductionStats _ProductionStats = new ProductionStats(true);

        public static void Reset()
        {
            ProductionStats prodStats = null;
            if (GameController.Instance.WasLoaded)
            {
                prodStats = LoadFromFile();
            }
            else
            {
                prodStats = new ProductionStats(true);
            }
            _ProductionStats = prodStats;
        }

        public static string GetTable(string name)
        {
            return _ProductionStats.GetTable(name);
        }

        public static void SaveToFile()
        {
            string savePath = Path.Combine(Utils.GetSaveFolder(), "ProductionStats.save");
            Console.WriteLine($"Saving production stats... {savePath}");
            JsonIO.SaveToFile(_ProductionStats, savePath);
        }

        public static ProductionStats LoadFromFile()
        {
            // TODO: Verify that the productionstat save is from the same save instance as the game save.
            string path = Path.Combine(Utils.GetSaveFolder(), "ProductionStats.save");
            Console.WriteLine($"Loading produciton stats... {path}");
            if (!File.Exists(path))
            {
                return new ProductionStats(false);
            }
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<ProductionStats>(json);
        }

        public static void GoodProduced(Good good, Building building = null)
        {
            _ProductionStats.AddGoodChange(good, 1);
        }

        public static void GoodConsumed(Good good, Building building = null)
        {
            _ProductionStats.AddGoodChange(good, -1);
        }

        public static void SkipNextProduction(Good good)
        {
            _ProductionStats.SkipNextProduction(good);
        }
    }
}

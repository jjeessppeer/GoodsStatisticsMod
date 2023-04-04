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

        public GoodChange(Good good, int multiplier, string productionCategory, string buildingName = "", int buildingId = -1)
        {
            GoodModel goodModel = Utils.GetGoodModel(good);
            GoodName = goodModel.displayName.Text;
            GoodCategory = goodModel.category.displayName.Text;
            GoodDelta = good.amount * multiplier;

            ProductionCategory = productionCategory;

            BuildingName = buildingName;
            BuildingId = buildingId;

            Date = Utils.GetGameDate();
        }

        public GoodChange(Good good, int multiplier, string productionCategory, Building building) : 
            this(good, multiplier, productionCategory, building.BuildingModel.Name, building.Id)
        {
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
        public string ModVersion = ProductionStatsMod.pluginVersion;


        public ProductionStats()
        {
            Console.WriteLine($"Initializing production stats...");
        }

        public void AddGoodChange(GoodChange goodChange)
        {
            if (goodChange.GoodDelta == 0) return;
            Console.WriteLine(goodChange);
            _GoodsTimeline.Add(goodChange);
        }

        public string GetTable(string name)
        {
            string tableString = "Y\tProd\t\tCons\t\tStart";
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
                    $"{(producedGoods >= 100 ? "\t" : "\t\t")}{(consumedGoods==0 ? "-" : "")}{consumedGoods}" +
                    $"{(consumedGoods <= -100 ? "\t" : "\t\t")}{startGoods}";
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
        public static ProductionStats _ProductionStats = new ProductionStats();

        public static void Reset(bool loadFromFile)
        {
            if (loadFromFile)
            {
                Console.WriteLine("Resetting saved...");
                LoadFromFile();
            }
            else
            {
                Console.WriteLine("Resetting new...");
                _ProductionStats = new ProductionStats();
            }
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

        public static void LoadFromFile()
        {
            string path = Path.Combine(Utils.GetSaveFolder(), "ProductionStats.save");
            Console.WriteLine($"Loading produciton stats... {path}");
            if (!File.Exists(path))
            {
                Reset(false);
                return;
            }
            string json = File.ReadAllText(path);
            _ProductionStats = JsonConvert.DeserializeObject<ProductionStats>(json);
        }

        public static void InitialGood(Good good)
        {
            GoodChange goodChange = new GoodChange(good, 1, "InitialGoods");
            goodChange.Date = new GameDate(0, Season.Storm, SeasonQuarter.Fourth);
            _ProductionStats.AddGoodChange(goodChange);
        }

        public static void BuildingProduction(Good good, Building building)
        {
            GoodChange goodChange = new GoodChange(good, 1, "BuildingProduction", building);
            _ProductionStats.AddGoodChange(goodChange);
        }

        public static void BuildingConsumption(Good good, Building building)
        {
            GoodChange goodChange = new GoodChange(good, -1, "BuildingConsumption", building);
            _ProductionStats.AddGoodChange(goodChange);
        }

        public static void VillagerFoodConsumed(Good good)
        {
            GoodChange goodChange = new GoodChange(good, -1, "VillagerEat");
            _ProductionStats.AddGoodChange(goodChange);
        }

        public static void ConstructionDeliver(Good good, Building building)
        {
            GoodChange goodChange = new GoodChange(good, -1, "ConstructionDeliver", building);
            _ProductionStats.AddGoodChange(goodChange);
        }

        public static void ConstructionRefund(Good good)
        {
            GoodChange goodChange = new GoodChange(good, 1, "ConstructionRefund");
            _ProductionStats.AddGoodChange(goodChange);
        }

        public static void HearthFuelConsumed(Good good)
        {
            GoodChange goodChange = new GoodChange(good, -1, "HearthFuel");
            _ProductionStats.AddGoodChange(goodChange);
        }

        public static void HearthSacrifice(Good good)
        {
            GoodChange goodChange = new GoodChange(good, -1, "HearthSacrifice");
            _ProductionStats.AddGoodChange(goodChange);
        }

        public static void OtherGoodAdd(Good good)
        {
            GoodChange goodChange = new GoodChange(good, 1, "Other");
            _ProductionStats.AddGoodChange(goodChange);

        }

        public static void OtherGoodRemove(Good good)
        {
            GoodChange goodChange = new GoodChange(good, -1, "Other");
            _ProductionStats.AddGoodChange(goodChange);

        }
    }
}

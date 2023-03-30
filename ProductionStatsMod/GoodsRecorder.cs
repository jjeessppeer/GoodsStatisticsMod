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
using Eremite;
using System.Diagnostics;

namespace ProductionStatsMod
{
    public struct GoodChange
    {
        public int Timestamp;
        public string GoodName;
        public int GoodDelta;

        public string Category;

        public string BuildingName;
        public int BuildingId;

        public GoodChange(string goodName, int goodDelta, string category, string buildingName = "", int buildingId = -1)
        {
            Timestamp = -1;
            GoodName = goodName;
            GoodDelta = goodDelta;
            Category = category;

            BuildingName = buildingName;
            BuildingId = buildingId;
        }
        public override string ToString()
        {
            string s = $"GoodChange: \n\t{GoodName} {GoodDelta}\n\t{Category}";
            if (BuildingName != "")
                s += $"\n\t{BuildingId} {BuildingName}";

            return s;

        }
    }

    public class ProductionStats
    {
        private List<GoodChange> ProductionTimeline = new List<GoodChange>();

        public ProductionStats()
        {
            Console.WriteLine($"Initializing production stats...");
        }

        public void AddGoodChange(GoodChange goodChange)
        {
            Console.WriteLine(goodChange);
            ProductionTimeline.Add(goodChange);
        }
    }

    public static class GoodMonitor
    {
        static ProductionStats _ProductionStats;

        public static void Reset()
        {
            _ProductionStats = new ProductionStats();
        }

        public static void GetTimestamp()
        {

        }

        public static void BuildingProduction(Good good, Building building)
        {
            GoodChange goodChange = new GoodChange(good.name, good.amount, "BuildingProduction", building.name, building.Id);
            _ProductionStats.AddGoodChange(goodChange);
        }

        public static void ConstructionDeliver(Good good, Building building)
        {
            GoodChange goodChange = new GoodChange(good.name, -good.amount, "ConstructionDeliver", building.name, building.Id);
            _ProductionStats.AddGoodChange(goodChange);
        }

        public static void ConstructionRefund(Good good, Building building)
        {
            GoodChange goodChange = new GoodChange()
            {
                GoodName = good.name,
                GoodDelta = good.amount,
                Category = "ConstructionRefund",
                BuildingName = building.Name,
                BuildingId = building.Id
            };
            _ProductionStats.AddGoodChange(goodChange);

        }

        public static void OtherGoodAdd(Good good)
        {
            GoodChange goodChange = new GoodChange()
            {
                GoodName = good.name,
                GoodDelta = good.amount,
                Category = "Other"
            };
            _ProductionStats.AddGoodChange(goodChange);

        }

        public static void OtherGoodRemove(Good good)
        {
            GoodChange goodChange = new GoodChange()
            {
                GoodName = good.name,
                GoodDelta = -good.amount,
                Category = "Other"
            };
            _ProductionStats.AddGoodChange(goodChange);

        }

        

    }
}

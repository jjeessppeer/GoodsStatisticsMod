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
    public class ProductionStats
    {
        struct GoodChange
        {
            int timestamp;
            Good good;
            int delta;
            string source;
        }

        private List<GoodChange> ProductionTimeline = new List<GoodChange>();

        public ProductionStats()
        {
            Console.WriteLine($"Initializing production stats...");

        }

        public void GoodProduced(Good good)
        {
            Console.WriteLine($"Goods produced: {good}");

        }

        public void GoodConsumed(Good good)
        {
            Console.WriteLine($"Goods consumed: {good}");
        }
    }

    [HarmonyPatch]
    public static class StatRecorder
    {
        static ProductionStats _ProductionStats;


        static void Reset()
        {
            _ProductionStats = new ProductionStats();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Eremite.Services.StorageService), "StoreInitialGoods")]
        private static void StoreInitialGoods()
        {
            Console.WriteLine("StoreInitialGoods");
            Reset();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GathererHut), "Store", new Type[] { typeof(Good) })]
        private static void GathererStore(Good good, GathererHut __instance)
        {
            Console.WriteLine($"Gatherer stored: {good} {__instance}");
            StackTrace stackTrace = new StackTrace();
            //Console.WriteLine($"Called from 1: {stackTrace.GetFrame(1).GetMethod().Name}");
            //Console.WriteLine($"Called from 2: {stackTrace.GetFrame(2).GetMethod().Name}");
            //Console.WriteLine($"Called from 3: {stackTrace.GetFrame(3).GetMethod().Name}");
            // Maybe unnececary? Is post ever stored to unless production is finished?
            if (stackTrace.GetFrame(3).GetMethod().Name == "FinishProduction")
            {
                _ProductionStats.GoodProduced(good);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Workshop), "FinishProduction")]
        private static void WorkshipFinish(WorkshopProductionState production, Workshop __instance)
        {
            Good good = production.product * production.multiplier;
            Console.WriteLine($"Workshop produced: {good} {__instance}");
            _ProductionStats.GoodProduced(good);

            // consume
            //production.ingredients; 
            Console.WriteLine($"Storing: {good} {__instance}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using HarmonyLib;
using UnityEngine;
using Eremite.Buildings;
using Eremite.Services;
using Eremite.Model;
using Eremite;
using System.Diagnostics;

namespace ProductionStatsMod
{
    [HarmonyPatch]
    public static class GoodMonitorPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Eremite.Services.StorageService), "StoreInitialGoods")]
        private static void StoreInitialGoods(StorageService __instance)
        {
            Console.WriteLine("StoreInitialGoods");
            //__instance.Main.Store(Serviceable.StateService.Conditions.embarkGoods);
            GoodMonitor.Reset();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GathererHut), "Store", new Type[] { typeof(Good) })]
        private static void GathererProduced(Good good, GathererHut __instance)
        {
            Console.WriteLine($"Gatherer stored: {good} {__instance}");
            StackTrace stackTrace = new StackTrace();
            if (stackTrace.GetFrame(3).GetMethod().Name == "FinishProduction")
            {
                GoodMonitor.BuildingProduction(good, __instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Camp), "Store")]
        private static void CampFinish(Good good, Camp __instance)
        {
            Console.WriteLine($"Camp stored: {good} {__instance}");
            StackTrace stackTrace = new StackTrace();
            if (stackTrace.GetFrame(3).GetMethod().Name == "FinishProduction")
            {
                GoodMonitor.BuildingProduction(good, __instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Workshop), "FinishProduction")]
        private static void WorkshipFinish(WorkshopProductionState production, Workshop __instance)
        {
            Good good = production.product * production.multiplier;
            Console.WriteLine($"Workshop produced: {good} {__instance}");
            GoodMonitor.BuildingProduction(good, __instance);

            // consume
            //production.ingredients; 
            Console.WriteLine($"Storing: {good} {__instance}");
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(Building), "Deliver")]
        private static void BuildingDelivery(Good good, Building __instance)
        {
            GoodMonitor.ConstructionDeliver(good, __instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Storage), "Store", new Type[] { typeof(Good) })]
        private static void StorageStore(Good good, Storage __instance)
        {
            StackTrace stackTrace = new StackTrace();

            // Special cases.
            if (stackTrace.GetFrame(2).GetMethod().Name == "RefundMaterial")
            {
                GoodMonitor.ConstructionRefund(good, __instance);
                return;
            }
            //if (stackTrace.GetFrame(2).GetMethod().Name == "StoreInitialGoods")

            //Ignored cases. Not actual new production, just moving.
            if (
                stackTrace.GetFrame(2).GetMethod().Name == "OnGoodRemoved" ||
                //stackTrace.GetFrame(3).GetMethod().Name == "StoreGoods" || 
                false
                )
            {
                return;
            }
            GoodMonitor.OtherGoodAdd(good);
            // Unhandled cases.
            Console.WriteLine($"Storage other source: {good} {__instance}");
            Console.WriteLine($"1: {stackTrace.GetFrame(1).GetMethod().Name}");
            Console.WriteLine($"2: {stackTrace.GetFrame(2).GetMethod().Name}");
            Console.WriteLine($"3: {stackTrace.GetFrame(3).GetMethod().Name}");
            Console.WriteLine($"4: {stackTrace.GetFrame(4).GetMethod().Name}");
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Storage), "Lock")]
        //private static void StorageLock(int owner, Good good, Storage __instance)
        //{
        //    Console.WriteLine($"Storage Lock: {good} {__instance}");
        //    StackTrace stackTrace = new StackTrace();
        //    Console.WriteLine($"1: {stackTrace.GetFrame(1).GetMethod().Name}");
        //    Console.WriteLine($"2: {stackTrace.GetFrame(2).GetMethod().Name}");
        //    Console.WriteLine($"3: {stackTrace.GetFrame(3).GetMethod().Name}");
        //    Console.WriteLine($"4: {stackTrace.GetFrame(4).GetMethod().Name}");
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Storage), "Release")]
        //private static void StorageRelease(int owner, Storage __instance)
        //{
        //    Good good = __instance.Goods.PeekLockedGood(owner);
        //    Console.WriteLine($"Storage Release: {good} {__instance}");
        //    StackTrace stackTrace = new StackTrace();
        //    Console.WriteLine($"1: {stackTrace.GetFrame(1).GetMethod().Name}");
        //    Console.WriteLine($"2: {stackTrace.GetFrame(2).GetMethod().Name}");
        //    Console.WriteLine($"3: {stackTrace.GetFrame(3).GetMethod().Name}");
        //    Console.WriteLine($"4: {stackTrace.GetFrame(4).GetMethod().Name}");
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Storage), "Take", new Type[] { typeof(int) })]
        //private static void StorageTake(int owner, Storage __instance)
        //{
        //    Good good = __instance.Goods.PeekLockedGood(owner);
        //    Console.WriteLine($"Storage Take int: {good} {__instance}");
        //    StackTrace stackTrace = new StackTrace();
        //    Console.WriteLine($"1: {stackTrace.GetFrame(1).GetMethod().Name}");
        //    Console.WriteLine($"2: {stackTrace.GetFrame(2).GetMethod().Name}");
        //    Console.WriteLine($"3: {stackTrace.GetFrame(3).GetMethod().Name}");
        //    Console.WriteLine($"4: {stackTrace.GetFrame(4).GetMethod().Name}");
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Storage), "Take", new Type[] { typeof(Good) })]
        //private static void StorageTake2(Good good, Storage __instance)
        //{
        //    Console.WriteLine($"Storage Take good: {good} {__instance}");
        //    StackTrace stackTrace = new StackTrace();
        //    Console.WriteLine($"1: {stackTrace.GetFrame(1).GetMethod().Name}");
        //    Console.WriteLine($"2: {stackTrace.GetFrame(2).GetMethod().Name}");
        //    Console.WriteLine($"3: {stackTrace.GetFrame(3).GetMethod().Name}");
        //    Console.WriteLine($"4: {stackTrace.GetFrame(4).GetMethod().Name}");
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Storage), "Remove", new Type[] { typeof(Good) })]
        private static void StorageLock(Good good, Storage __instance)
        {
            Console.WriteLine($"Storage Remove: {good} {__instance}");
            GoodMonitor.OtherGoodRemove(good);
            StackTrace stackTrace = new StackTrace();
            Console.WriteLine($"1: {stackTrace.GetFrame(1).GetMethod().Name}");
            Console.WriteLine($"2: {stackTrace.GetFrame(2).GetMethod().Name}");
            Console.WriteLine($"3: {stackTrace.GetFrame(3).GetMethod().Name}");
            Console.WriteLine($"4: {stackTrace.GetFrame(4).GetMethod().Name}");
        }


    }
}

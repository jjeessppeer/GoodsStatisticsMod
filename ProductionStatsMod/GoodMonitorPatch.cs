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
using Eremite.Characters.Villagers;
using System.Reflection.Emit;
using System.Reflection;

namespace ProductionStatsMod
{
    [HarmonyPatch]
    public class StorePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Storage), "Store", new Type[] { typeof(Good) })]
        static void StorageStoreGood(Good good, IStorage __instance)
        {
            StorePatch.StoreGood(good, __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingStorage), "Store", new Type[] { typeof(Good) })]
        static void BuildingStoreGood(Good good, IStorage __instance)
        {
            StorePatch.StoreGood(good, __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingStorage), "Store", new Type[] { typeof(IList<Good>) })]
        static void BuildingStoreList(IList<Good> goods, IStorage __instance)
        {
            foreach (Good good in goods)
            {
                StorePatch.StoreGood(good, __instance);
            }
        }

        static void StoreGood(Good good, IStorage storage)
        {
            Console.WriteLine($"Storage Store: {good} {storage}");
            StackTrace stackTrace = new StackTrace();
            for (int i = 1; i <= 6; ++i)
                Console.WriteLine($"{i}: {stackTrace.GetFrame(i).GetMethod().Name}");

            //if (stackTrace.GetFrame(3).GetMethod().Name == "OnGoodRemoved")
            //{
            //    // Good returned from building, not produced.
            //    return;
            //}
            GoodMonitor.GoodProduced(good);
        }
    }

    [HarmonyPatch]
    public class RemovePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Storage), "Remove", new Type[] { typeof(Good) })]
        static void RemoveGood(Good good, IStorage __instance)
        {
            Console.WriteLine($"Storage Remove: {good} {__instance}");
            StackTrace stackTrace = new StackTrace();
            for (int i = 1; i <= 6; ++i)
                Console.WriteLine($"{i}: {stackTrace.GetFrame(i).GetMethod().Name}");
            GoodMonitor.GoodConsumed(good);
        }
    }

    [HarmonyPatch]
    public class TakePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Storage), "Take", new Type[] { typeof(int) })]
        static void StorageTakeInt(int owner, Storage __instance)
        {
            Good good = __instance.Goods.PeekLockedGood(owner);
            TakePatch.Take(good, owner, __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingStorage), "Take", new Type[] { typeof(int) })]
        static void BuildingTakeInt(int owner, BuildingStorage __instance)
        {
            Good good = __instance.Goods.PeekLockedGood(owner);
            TakePatch.Take(good, owner, __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Storage), "Take", new Type[] { typeof(Good) })]
        static void StorageTakeGood(Good good, Storage __instance)
        {
            TakePatch.Take(good, __instance.Id, __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Storage), "Take", new Type[] { typeof(Good) })]
        static void BuildingTakeGood(Good good, Storage __instance)
        {
            TakePatch.Take(good, __instance.Id, __instance);
        }

        static void Take(Good good, int owner, IStorage storage)
        {
            Console.WriteLine($"Take: {good}");
            StackTrace stackTrace = new StackTrace(); 
            for (int i = 1; i <= 12; ++i)
                Console.WriteLine($"{i}: {stackTrace.GetFrame(i).GetMethod().Name}");

            //if (stackTrace.GetFrame(7).GetMethod().Name == "StoreCollectedGood")
            //{
            //    // Good is only transported to main storage. Not consumed.
            //    // Skip next production event of good.
            //    // It will either be moved to main storage or cancelled and returned.
            //    return;
            //}

            GoodMonitor.GoodConsumed(good);

        }
    }
}

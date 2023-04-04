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


    [HarmonyPatch(typeof(Hunger), "Consume")]
    public static class HungerPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            var instructionsToInsert = new List<CodeInstruction>();
            instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_1));
            instructionsToInsert.Add(new CodeInstruction(
                OpCodes.Call,
                AccessTools.Method(
                    typeof(GoodMonitor),
                    "VillagerFoodConsumed"
            )));
            codes.InsertRange(codes.Count - 1, instructionsToInsert);
            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch]
    public static class BuildingConsumePatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Workshop), "FinishProduction");
            yield return AccessTools.Method(typeof(BlightPost), "FinishProduction");
        }

        //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var codes = new List<CodeInstruction>(instructions);
        //    var instructionsToInsert = new List<CodeInstruction>();
        //    instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_1));
        //    instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
        //    instructionsToInsert.Add(new CodeInstruction(
        //        OpCodes.Call,
        //        AccessTools.Method(
        //            typeof(GoodMonitor),
        //            "BuildingConsumption"
        //    )));
        //    codes.InsertRange(0, instructionsToInsert);
        //    return codes.AsEnumerable();
        //}

        static void Prefix(ProductionState production, Building __instance)
        {
            List<Good> ingredients = new List<Good>();
            if (production is WorkshopProductionState wsProduction)
                ingredients = wsProduction.ingredients;
            if (production is BlightPostProductionState bpProduction)
                ingredients = bpProduction.ingredients;
            foreach (Good good in ingredients)
            {
                GoodMonitor.BuildingConsumption(good, __instance);
            }
        }
    }

    [HarmonyPatch]
    public static class BuildingProducePatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Extractor), "Store");
            yield return AccessTools.Method(typeof(Mine), "Store");
            yield return AccessTools.Method(typeof(Workshop), "Store");
            yield return AccessTools.Method(typeof(RainCatcher), "Store");
            yield return AccessTools.Method(typeof(GathererHut), "Store");
            yield return AccessTools.Method(typeof(Collector), "Store");
            yield return AccessTools.Method(typeof(Camp), "Store");
            yield return AccessTools.Method(typeof(BlightPost), "Store");
        }
        
        //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var codes = new List<CodeInstruction>(instructions);
        //    var instructionsToInsert = new List<CodeInstruction>();
        //    instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_1));
        //    instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
        //    instructionsToInsert.Add(new CodeInstruction(
        //        OpCodes.Call,
        //        AccessTools.Method(
        //            typeof(GoodMonitor),
        //            "BuildingProduction"
        //    )));
        //    codes.InsertRange(0, instructionsToInsert);
        //    return codes.AsEnumerable();
        //}

        static void Prefix(Good good, Building __instance)
        {
            GoodMonitor.BuildingProduction(good, __instance);
        } 
    }

    [HarmonyPatch]
    class StorageTakePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Storage), "Take", new Type[] { typeof(int) })]
        static void TakeInt(int owner, Storage __instance)
        {
            Good good = __instance.Goods.PeekLockedGood(owner);
            StorageTakePatch.TakeGood(good, __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Storage), "Take", new Type[] { typeof(Good) })]
        static void TakeGood(Good good, Storage __instance)
        {
            Console.WriteLine($"Storage Take: {good} {__instance}");
            StackTrace stackTrace = new StackTrace();
            for (int i = 1; i <= 6; ++i)
                Console.WriteLine($"{i}: {stackTrace.GetFrame(i).GetMethod().Name}");
        }
    }

    [HarmonyPatch]
    class StorageRemovePatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Storage), "Remove", new Type[] { typeof(Good) });
        }

        static void Prefix(Good good, IStorage __instance)
        {
            StackTrace stackTrace = new StackTrace();

            if (SkipStorageRemove(stackTrace))
            {
                return;
            }
            Console.WriteLine($"Storage Remove: {good} {__instance}");

            GoodMonitor.OtherGoodRemove(good);
            //Console.WriteLine($"Storage Remove: {good} {__instance}");
            for (int i = 1; i <= 6; ++i)
                Console.WriteLine($"{i}: {stackTrace.GetFrame(i).GetMethod().Name}");
        }

        private static bool SkipStorageRemove(StackTrace stackTrace)
        {
            if (stackTrace.GetFrame(2).GetMethod().DeclaringType == typeof(Hunger) &&
                (stackTrace.GetFrame(3).GetMethod().Name == "Eat" || stackTrace.GetFrame(4).GetMethod().Name == "Eat"))
            {
                return true;
            }

            if (stackTrace.GetFrame(3).GetMethod().Name == "SetSacrificeEffectLevel")
            {
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch]
    class StorageStorePatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Storage), "Store", new Type[] { typeof(Good) });
        }
        static void Prefix(Good good, IStorage __instance)
        {
            StackTrace stackTrace = new StackTrace();

            if (stackTrace.GetFrame(2).GetMethod().Name == "RefundMaterials")
            {
                GoodMonitor.ConstructionRefund(good);
                return;
            }

            if (stackTrace.GetFrame(3).GetMethod().Name == "DMD<Eremite.Services.StorageService::StoreInitialGoods>")
            {
                GoodMonitor.InitialGood(good);
                return;
            }

            if (SkipStorageStore(stackTrace)) return;

            GoodMonitor.OtherGoodAdd(good);
            for (int i = 1; i <= 6; ++i)
                Console.WriteLine($"{i}: {stackTrace.GetFrame(i).GetMethod().Name}");
        }

        private static bool SkipStorageStore(StackTrace stackTrace)
        {
            if (
                   stackTrace.GetFrame(2).GetMethod().Name == "OnGoodRemoved" ||
                   stackTrace.GetFrame(3).GetMethod().Name == "StoreGoods" ||
                   stackTrace.GetFrame(3).GetMethod().Name == "PayForSacrafice" ||
                   false
                   )
            {
                return true;
            }
            return false;
        }
    }

    [HarmonyPatch]
    public static class GoodMonitorPatches
    {
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Eremite.Services.StorageService), "StoreInitialGoods")]
        //private static void StoreInitialGoods(StorageService __instance)
        //{
        //    Console.WriteLine("StoreInitialGoods");
        //    //__instance.Main.Store(Serviceable.StateService.Conditions.embarkGoods);
        //    GoodMonitor.Reset();
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Building), "Deliver")]
        private static void BuildingDelivery(Good good, Building __instance)
        {
            GoodMonitor.ConstructionDeliver(good, __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Hearth), "DeliverFuel", new Type[] { typeof(Good), typeof(GoodModel) })]
        private static void HearthDelivery(Good good, GoodModel goodModel, Hearth __instance)
        {
            GoodMonitor.HearthFuelConsumed(good);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Hearth), "PayForSacrafice")]
        private static void HearthSacrifice(Good good, Hearth __instance)
        {
            GoodMonitor.HearthSacrifice(good);
        }
    }
}

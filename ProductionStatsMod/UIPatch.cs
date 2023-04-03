using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using HarmonyLib;
using UnityEngine;

using Eremite;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;

using Eremite.View.HUD;
using Eremite.Buildings;
using Eremite.Services;
using Eremite.Model;
using Eremite.Characters.Villagers;
using System.IO;
using TMPro;

namespace ProductionStatsMod
{
    [HarmonyPatch]
    class TrendMarkerUIPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SimpleTooltip), "Show", new Type[] { typeof(RectTransform), typeof(string), typeof(string), typeof(string), typeof(TooltipSettings) });
            //yield return AccessTools.Method(typeof(GoodsCategoryTrendMarker), "GetTooltipDesc");
        }

        static void Prefix(ref string header, ref string desc)
        {
            StackTrace stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(2).GetMethod();
            if (!(method.DeclaringType == typeof(GoodsCategoryTrendMarker) || method.DeclaringType == typeof(GoodTrendMarker)))
                return;
            Console.WriteLine($"OVERRIDING TOOLTIP\n\t{header}");
            string contents = File.ReadAllText("tooltip.txt");
            if (contents == "")
                desc = GoodMonitor.GetTable(header);
            else
                desc = contents;
        }
    }


    //[HarmonyPatch]
    //class TrendMarkerUIPatch : GameMB
    //{
    //    static IEnumerable<MethodBase> TargetMethods()
    //    {
    //        yield return AccessTools.Method(typeof(GoodTrendMarker), "GetTooltipDesc");
    //        yield return AccessTools.Method(typeof(GoodsCategoryTrendMarker), "GetTooltipDesc");
    //    }
    //    static bool Prefix(ref string __result)
    //    {
    //        SimpleTooltip simpleTooltip = MB.TooltipsService.Get<SimpleTooltip>();
    //        if (simpleTooltip == null)
    //        {
    //            return false;
    //        }
    //        Console.WriteLine("OVERRIDING TOOLTIP");
    //        //__result = "Hello there\nthis\nis\na\nreally\nlong\nString\nthislineisreally__________long__________\tline\twa\na\tb\tc\td\te";
    //        //__result = "___________________________________________________";

    //        string contents = File.ReadAllText("tooltip.txt");
    //        __result = contents;
    //        //__result += "\n" +
    //        //    "_\tProduced\tConsumed\t\tStart\t\tEnd" +
    //        //    "\nYear 2\t+10\t-24\t20\t34" +
    //        //    "\nYear 1\t+2\t-4\t10\t20";
    //        return false;
    //    }
    //}


    [HarmonyPatch]
    class UIPatch
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SimpleTooltip), "SetTexts")]
        static void TrendTooltipOverride(ref TMP_Text ___desc)
        {
            Console.WriteLine("TEXT POSTFIX");
            ___desc.horizontalAlignment = HorizontalAlignmentOptions.Left;
        }
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(GoodTooltip), "Show", new Type[] { typeof(RectTransform), typeof(TooltipSettings), typeof(GoodModel), typeof(int), typeof(GoodTooltipMode), typeof(string) })]
        //static void TooltipText(RectTransform target, TooltipSettings settings, GoodModel model, int amount, GoodTooltipMode mode, string footnote, GoodTooltip __instance)
        //{
        //    Console.WriteLine($"TOOLTIP: {model.displayName} {model.Description}");
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(GoodTrendMarker), "GetTooltipDesc")]
        //static bool TrendTooltipOverride(ref string __result)
        //{
        //    Console.WriteLine("OVERRIDING TOOLTIP");
        //    __result = "Hello there";
        //    return false;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(GoodTrendMarker), "OnPointerEnter")]
        //static void TrendTooltipOverride2()
        //{
        //    Console.WriteLine("OVERRIDING TOOLTIP");
        //}
    }

}

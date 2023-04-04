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
            yield return AccessTools.Method(typeof(SimpleTooltip), "Show", new Type[] { typeof(RectTransform), typeof(string), typeof(string), typeof(TooltipSettings) });
            //yield return AccessTools.Method(typeof(GoodsCategoryTrendMarker), "GetTooltipDesc");
        }

        static void Prefix(ref string header, ref string desc)
        {
            StackTrace stackTrace = new StackTrace();
            if (!(
                stackTrace.GetFrame(3).GetMethod().DeclaringType == typeof(GoodsCategoryTrendMarker) ||
                stackTrace.GetFrame(2).GetMethod().DeclaringType == typeof(GoodTrendMarker)
                ))
                return;
            Console.WriteLine($"OVERRIDING TOOLTIP: {header}");
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
    //        string contents = File.ReadAllText("tooltip.txt");
    //        __result = contents;
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
            // TODO: only apply to trendmarker tooltips
            Console.WriteLine("TEXT POSTFIX");
            ___desc.horizontalAlignment = HorizontalAlignmentOptions.Left;
        }
    }

}

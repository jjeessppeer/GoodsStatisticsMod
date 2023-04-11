using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ProductionStatsMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class ProductionStatsMod : BaseUnityPlugin
    {
        public const string pluginGuid = "jesper.goodsstatistics.mod";
        public const string pluginName = "Goods Stats Mod";
        public const string pluginVersion = "1.0.0";

        public void Awake()
        {
            var harmony = new Harmony(pluginGuid);
            harmony.PatchAll();
        }
    }

    
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
using Eremite.Controller.Generator;
using Eremite.Controller;

namespace ProductionStatsMod
{
    [HarmonyPatch]
    class SaveLoadPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(JsonIO), "ExecuteSaveToFile")]
        static void OnSave(string path)
        {
            Console.WriteLine($"Saving game: {path}");
            Console.WriteLine($"{Utils.GetSaveFolder()}");

            Console.WriteLine($"Game Active: {Eremite.Controller.GameController.IsGameActive}");
            string file = Path.GetFileName(path);
            if (file != "Save.save" || !Eremite.Controller.GameController.IsGameActive) return;
            string savePath = Path.Combine(Utils.GetSaveFolder(), "ProductionStats.save");

            //string serializedStats = GoodMonitor.GetSerialized();
            //Console.WriteLine(serializedStats);
            Console.WriteLine($"Saving prodstats: {savePath}");
            Console.WriteLine(GoodMonitor._ProductionStats.Serialize());
            JsonIO.SaveToFile(GoodMonitor._ProductionStats, savePath);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameController), "StartGame")]
        static void OnGameStart()
        {
            Console.WriteLine($"Game started {Utils.GetSaveFolder()}");
            Console.WriteLine($"Game Active: {Eremite.Controller.GameController.IsGameActive}");
        }
        
    }
}

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
using Newtonsoft.Json;

namespace ProductionStatsMod
{
    [HarmonyPatch]
    class SaveLoadPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(JsonIO), "ExecuteSaveToFile")]
        static void OnSave(string path)
        {
            string file = Path.GetFileName(path);
            if (file != "Save.save" || !GameController.IsGameActive) return;

            GoodMonitor.SaveToFile();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLoader), "SetUp")]
        static void OnGameStart2()
        {
            GoodMonitor.Reset(GameController.Instance.WasLoaded);
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(GameController), "StartGame")]
        //static void OnGameStart()
        //{
        //    GoodMonitor.Reset(GameController.Instance.WasLoaded);
        //    //if (!GameController.Instance.WasLoaded)
        //    //{
        //    //    Console.WriteLine("Game was not loaded.");
        //    //    return;
        //    //}
        //    //GoodMonitor.LoadFromFile();
        //}

    }
}

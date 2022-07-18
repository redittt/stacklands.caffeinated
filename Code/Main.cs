using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Sokpop;


namespace Caffeinated
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "horhorou.stacklands.caffeinated";
        public const string pluginName = "Caffeinated";
        public const string pluginVersion = "1.0.0";

        static ManualLogSource logger;

        public void Awake()
        {
            InitializeMod();
        }


        private void InitializeMod()
        {
            Harmony harmony = new Harmony(pluginGuid);
            MethodInfo original = AccessTools.Method(typeof(Villager), "GetActionTimeModifier");
            MethodInfo patched = AccessTools.Method(typeof(Main), "GetActionTimeModifier_Patched");

            logger = new ManualLogSource(pluginGuid);

            harmony.Patch(original, null, new HarmonyMethod(patched));
        }


        public static void GetActionTimeModifier_Patched(ref float __result, string actionId, CardData baseCard)
        {

            __result = 0.125f;
        }



    }
}

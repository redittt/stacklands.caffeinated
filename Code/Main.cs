using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;


namespace Caffeinated
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "horhorou.stacklands.caffeinated";
        public const string pluginName = "Caffeinated";
        public const string pluginVersion = "1.0.1";


        public const float workSpeed = 0.25f;

        static ManualLogSource logger;

        public void Awake()
        {
            InitializeMod();
        }


        private void InitializeMod()
        {
            Harmony harmony = new Harmony(pluginGuid);

            MethodInfo originalTimeModifier = AccessTools.Method(typeof(Villager), "GetActionTimeModifier");
            MethodInfo postfixTimeModifier = AccessTools.Method(typeof(Main), nameof(PostfixTimeModifier));


            MethodInfo originalUpdateCard = AccessTools.Method(typeof(CardData), nameof(CardData.UpdateCard));
            MethodInfo prefixUpdateCard = AccessTools.Method(typeof(Main), nameof(PrefixUpdateCard));



            logger = new ManualLogSource(pluginGuid);


            harmony.Patch(originalTimeModifier, null, new HarmonyMethod(postfixTimeModifier));
            harmony.Patch(originalUpdateCard, new HarmonyMethod(prefixUpdateCard));
        }




        public static void PostfixTimeModifier(ref float __result, string actionId, CardData baseCard)
        {
            __result = workSpeed * 0.5f;
        }


        private static Subprint MatchingPrint(CardData __instance)
        {
            Subprint result = null;
            int num = int.MaxValue;
            int num2 = int.MinValue;
            foreach (Blueprint blueprint in WorldManager.instance.BlueprintPrefabs)
            {
                SubprintMatchInfo subprintMatchInfo;
                Subprint matchingSubprint = blueprint.GetMatchingSubprint(__instance.MyGameCard.GetRootCard(), out subprintMatchInfo);
                if (matchingSubprint != null && (subprintMatchInfo.MatchCount > num2 || (subprintMatchInfo.MatchCount == num2 && subprintMatchInfo.FullyMatchedAt < num)))
                {
                    num = subprintMatchInfo.FullyMatchedAt;
                    num2 = subprintMatchInfo.MatchCount;
                    result = matchingSubprint;
                }
            }
            return result;
        }

        private static void CheckStackValidityAndRestack(CardData __instance)
        {
            List<GameCard> allCardsInStack = __instance.MyGameCard.GetAllCardsInStack();
            List<GameCard> list = new List<GameCard>();
            for (int i = 0; i < allCardsInStack.Count; i++)
            {
                list.Add(allCardsInStack[i]);
                if (i < allCardsInStack.Count - 1 && !allCardsInStack[i].CardData.CanHaveCardOnTop(allCardsInStack[i + 1].CardData))
                {
                    WorldManager.instance.Restack(list);
                    list.Clear();
                }
            }
            WorldManager.instance.Restack(list);
        }

        public static bool PrefixUpdateCard(CardData __instance)
        {
            if (__instance.MyGameCard.IsDemoCard || !__instance.MyGameCard.MyBoard.IsCurrent)
            {
                return false;
            }
            __instance.MyGameCard.HighlightActive = false;
            if (WorldManager.instance.DraggingCard != null && WorldManager.instance.DraggingCard != __instance.MyGameCard)
            {
                if (__instance.CanHaveCardOnTop(WorldManager.instance.DraggingCard.CardData) && __instance.MyGameCard.Child == null && !__instance.MyGameCard.IsChildOf(WorldManager.instance.DraggingCard))
                {
                    __instance.MyGameCard.HighlightActive = true;
                }
                if (!(__instance.MyGameCard.removedChild == WorldManager.instance.DraggingCard))
                {
                    GameCard cardWithStatusInStack = __instance.MyGameCard.GetCardWithStatusInStack();
                    if (cardWithStatusInStack != null && !cardWithStatusInStack.CardData.CanHaveCardsWhileHasStatus())
                    {
                        __instance.MyGameCard.HighlightActive = false;
                    }
                }
            }
            if (__instance.MyGameCard.Parent == null)
            {
                Subprint subprint = MatchingPrint(__instance);
                if (subprint != null)
                {
                    __instance.MyGameCard.TimerBlueprintId = subprint.ParentBlueprint.Id;
                    __instance.MyGameCard.TimerSubprintIndex = subprint.SubprintIndex;
                    __instance.MyGameCard.StartTimer(workSpeed, new TimerAction(__instance.FinishBlueprint), subprint.StatusName, __instance.GetActionId("FinishBlueprint"));
                }
                else
                {
                    __instance.MyGameCard.CancelTimer(__instance.GetActionId("FinishBlueprint"));
                }
            }
            if (__instance.MyGameCard.TimerRunning && __instance.MyGameCard.TimerActionId == "finish_blueprint" && MatchingPrint(__instance) == null)
            {
                __instance.MyGameCard.CancelTimer(__instance.GetActionId("FinishBlueprint"));
            }
            if (!__instance.MyGameCard.BeingDragged && __instance.MyGameCard.LastParent != null && __instance.MyGameCard.Parent == null)
            {
                if (__instance.MyGameCard.LastParent.GetRootCard().CardData.DetermineCanHaveCardsWhenIsRoot)
                {
                    CheckStackValidityAndRestack(__instance);
                }
                __instance.MyGameCard.LastParent = null;
            }
            for (int i = __instance.StatusEffects.Count - 1; i >= 0; i--)
            {
                __instance.StatusEffects[i].Update();
            }

            return false;
        }


    }
}

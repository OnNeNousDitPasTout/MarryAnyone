using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
#if V1720MORE
    using TaleWorlds.CampaignSystem.GameComponents;
#else
    using TaleWorlds.CampaignSystem.SandBox.GameComponents;
#endif

namespace MarryAnyone.Patches.Models
{
#if TRACEPREGNANCY2
        [HarmonyPatch(typeof(DefaultPregnancyModel))]
        internal static class DefaultPregnancyModelPatch
        {
            [HarmonyPatch(typeof(DefaultPregnancyModel), "GetDailyChanceOfPregnancyForHero", new Type[] {typeof(Hero) })]
            [HarmonyPostfix]
            internal static void GetDailyChanceOfPregnancyForHeroPostFix(Hero hero, DefaultPregnancyModel __instance, ref float __result)
            {
                Helper.Print(String.Format("GetDailyChanceOfPregnancyForHeroPostFix for Hero {0} => result : {1}", hero.Name, __result), Helper.PrintHow.PrintToLogAndWrite);
            }
        }
#endif
}

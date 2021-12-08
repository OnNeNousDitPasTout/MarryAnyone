using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace MarryAnyone.Patches
{
#if TRACEEXSPOUSE

    [HarmonyPatch(typeof(Hero))]
    internal static class HeroPatch
    {


        public static Hero heroJustifie = null;

        public static MBReadOnlyList<Hero> HeroExspouses(Hero hero)
        {
            heroJustifie = hero;
            return hero.ExSpouses;
        }

        //[HarmonyPatch(declaringType: typeof(Hero), methodName: "ExSpouses", MethodType.Getter)]
        //[HarmonyPatch(typeof(Hero))]
        //[HarmonyPatch("ExSpouses", methodType: MethodType.Getter)]
        //[HarmonyPatch(declaringType: typeof(Hero), methodName: "ExSpouses", methodType: MethodType.Getter)]
        //[HarmonyPatch(typeof(Hero))]
        //[HarmonyPatch("ExSpouses", methodType: MethodType.Getter)]
        //[HarmonyPatch(nameof(Hero.ExSpouses), PropertyMethod.Getter)]
        [HarmonyPatch(typeof(Hero), nameof(Hero.ExSpouses), MethodType.Getter)]
        [HarmonyPostfix]
        //public static void HeroExSpousesPatch(Hero __instance, ref MBReadOnlyList<Hero> __result)
        public static void HeroExSpousesPatch(Hero __instance, MBReadOnlyList<Hero> __result)
        {
            if (heroJustifie != __instance && __instance != Hero.MainHero)
            {
                String aff;
                if (__result != null)
                    aff = String.Join(", ", __result.Select<Hero, String>(x => x.Name.ToString()).ToList());
                else
                    aff = "VIDE";
                Helper.Print(String.Format("HeroExSpousesPatch on Hero {0} retourne {1}", __instance.Name.ToString(), aff), Helper.PrintHow.PrintToLogAndWrite);
            }
            heroJustifie = null;
        }
    }
#endif
}

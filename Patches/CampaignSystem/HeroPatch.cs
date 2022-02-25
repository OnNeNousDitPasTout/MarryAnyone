using HarmonyLib;
using MarryAnyone.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace MarryAnyone.Patches.CampaignSystem
{
#if TRACEEXSPOUSE || PATCHHOMESETTLEMENT

    [HarmonyPatch(typeof(Hero))]
    internal static class HeroPatch
    {
#if PATCHHOMESETTLEMENT
        [HarmonyPatch(typeof(Hero), "UpdateHomeSettlement")]
        [HarmonyPrefix]
        private static bool UpdateHomeSettlementPrefix(Hero __instance)
        {
            if (__instance.Clan != null && !__instance.Clan.IsNeutralClan && __instance.Clan.HomeSettlement == null)
            {
                Settlement? resolve = null;
                if (__instance.Clan.IsBanditFaction 
                    || __instance.Clan.IsOutlaw)
                {
                    resolve = Settlement.FindAll(x => x.IsHideout 
                                && x.Culture == __instance.Culture 
                                && (x.OwnerClan == null || x.OwnerClan == __instance.Clan)).GetRandomElementInefficiently();

                    if (resolve == null)
                        resolve = Settlement.FindAll(x => x.IsHideout
                                    && (x.OwnerClan == null || x.OwnerClan == __instance.Clan)).GetRandomElementInefficiently();

                    if (resolve == null)
                        resolve = Settlement.FindAll(x => x.IsHideout).GetRandomElementInefficiently();
                }
                if (resolve == null 
                    && (__instance.Clan.IsClanTypeMercenary 
                        || __instance.Clan.IsMafia
                        || __instance.Clan.IsNomad
                        || __instance.Clan.IsRebelClan))
                {
                    resolve = Settlement.FindAll(x => x.IsFortification
                                                && x.Culture == __instance.Culture
                                                && (x.OwnerClan == null || x.OwnerClan == __instance.Clan)).GetRandomElementInefficiently();
                    if (resolve == null)
                        resolve = Settlement.FindAll(x => x.IsFortification
                                                    && (x.OwnerClan == null || x.OwnerClan == __instance.Clan)).GetRandomElementInefficiently();
                    if (resolve == null)
                        resolve = Settlement.FindAll(x => x.IsFortification).GetRandomElementInefficiently();
                }

                if (resolve == null)
                    resolve = Settlement.All.GetRandomElementInefficiently();

                if (resolve == null)
                {
                    Helper.Print(String.Format("UpdateHomeSettlementPrefix Settlement unresolved for hero {0}", __instance.Name), Helper.PrintHow.PrintToLogAndWriteAndForceDisplay);
                    throw new Exception(String.Format("UpdateHomeSettlementPrefix Settlement unresolved for hero {0}", __instance.Name));
                }

#if TRACEPATCH
                Helper.Print(String.Format("UpdateHomeSettlementPrefix:: Pach homeSettlement for the clan {0} of {1} => {2}"
                                            , __instance.Clan
                                            , __instance.Name
                                            , resolve.Name), Helper.PRINT_PATCH);
#endif

                FieldInfo _homeFInfo = AccessTools.Field(typeof(Clan), "_home");
                if (_homeFInfo == null)
                    throw new Exception("_home not resolved on Clan class");
                _homeFInfo.SetValue(__instance.Clan, resolve);
            }
            return true;
        }
#endif

#if TRACKTOMUCHSPOUSE || TRACEEXSPOUSE
        public static ShortLifeObject spouseOn = new ShortLifeObject(200);
        public static ShortLifeObject spouseWith = new ShortLifeObject(200);

        [HarmonyPatch(typeof(Hero), "set_Spouse", new Type[] { typeof(Hero) })]
        //[HarmonyPatch(typeof(Hero), "Spouse", MethodType.Setter)]
        [HarmonyPrefix]
        public static void set_SpousePrefix(Hero value, Hero __instance)
        {
            if (spouseOn.Swap(value) && spouseWith.Swap(__instance))
            {
                Helper.Print(String.Format("set_SpousePrefix with {0} on {1}"
                                , (value != null ? value.Name : "NULL")
                                , __instance.Name), Helper.PRINT_TRACE_WEDDING);
            }
        }

#endif

#if TRACEEXSPOUSE

        public static Hero? heroJustifie = null;
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
#endif

    }
#endif
}

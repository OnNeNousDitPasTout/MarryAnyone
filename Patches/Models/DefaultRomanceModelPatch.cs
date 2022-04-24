﻿using HarmonyLib;
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
#if V1720LESS

    [HarmonyPatch(typeof(DefaultRomanceModel), "CourtshipPossibleBetweenNPCs", new Type[] { typeof(Hero), typeof(Hero) })]
    class DefaultRomanceModel_CourtshipPossibleBetweenNPCs_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Hero person1, Hero person2, ref bool __result)
        {
            Romance.RomanceLevelEnum level = Romance.GetRomanticLevel(person1, person2);

            __result = (level == Romance.RomanceLevelEnum.Untested
                    || level == Romance.RomanceLevelEnum.MatchMadeByFamily
                    || level == Romance.RomanceLevelEnum.CourtshipStarted
                    || level == Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible
                    || level == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage)
                && (person2.Clan == null || Romance.GetCourtedHeroInOtherClan(person1, person2) == null)
                && (person1.Clan == null || Romance.GetCourtedHeroInOtherClan(person2, person1) == null)
                && Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(person1, person2);

#if TRACEROMANCE
            Helper.Print(String.Format("CourtshipPossibleBetweenNPCs entre {0} et {1} répond {2}", person1.Name.ToString(), person2.Name.ToString(), __result.ToString()), Helper.PRINT_TRACE_ROMANCE) ;
#endif

            return false; // On retourne false pour inhiber l'appel classique
        }

    }
#endif
}

using HarmonyLib;
using MarryAnyone.Behaviors;
using MarryAnyone.Helpers;
using MarryAnyone.Models;
using MarryAnyone.Settings;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Barterables;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Localization;

namespace MarryAnyone.Patches.Behaviors
{
    [HarmonyPatch(typeof(RomanceCampaignBehavior))]
    internal static class RomanceCampaignBehaviorPatch
    {
        static Hero? _heroBeingProposedTo = null;

        [HarmonyPatch("conversation_player_eligible_for_marriage_with_conversation_hero_on_condition")]
        [HarmonyPostfix]
        private static void Postfix1(ref bool __result)
        {
            __result = Hero.OneToOneConversationHero != null 
                && Romance.GetCourtedHeroInOtherClan(Hero.MainHero, Hero.OneToOneConversationHero) == null 
                && Campaign.Current.Models.RomanceModel.CourtshipPossibleBetweenNPCs(Hero.MainHero, Hero.OneToOneConversationHero);
        }

        [HarmonyPostfix]
        [HarmonyPatch("RomanceCourtshipAttemptCooldown", MethodType.Getter)]
        private static void Postfix2(ref CampaignTime __result)
        {
            if (Helper.MASettings.RetryCourtship)
            {
                __result = CampaignTime.DaysFromNow(1f);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("conversation_player_can_open_courtship_on_condition")]
        private static bool Prefix1(ref bool __result)
        {
            __result = conversation_player_can_open_courtship_on_condition(false);
            return false;
        }

        public static bool conversation_player_can_open_courtship_on_condition(bool canCheat)
        {
            if (Hero.OneToOneConversationHero is null)
            {
                return false;
            }
            bool flag = (Hero.MainHero.IsFemale && Helper.MASettings.SexualOrientation == "Heterosexual")
                    || (!Hero.MainHero.IsFemale && Helper.MASettings.SexualOrientation == "Homosexual")
                    || (!Hero.OneToOneConversationHero.IsFemale && Helper.MASettings.SexualOrientation == "Bisexual");

#if TRACEROMANCE && TRACELOAD
            Helper.Print(string.Format("Output {0}", Helper.LogPath), Helper.PRINT_TRACE_ROMANCE);
#endif
            //bool areMarried = Util.Util.AreMarried(Hero.MainHero, Hero.OneToOneConversationHero);
            bool areMarried = MARomanceCampaignBehavior.Instance.SpouseOrNot(Hero.MainHero, Hero.OneToOneConversationHero);
            Romance.RomanceLevelEnum romanceLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);

#if TRACEROMANCE
            Helper.Print(String.Format("Couple suitable for mariage: {0}\r\n\tbetween{1} and {2} areMarried {3}"
                        ,MADefaultMarriageModel.IsCoupleSuitableForMarriageStatic(Hero.MainHero, Hero.OneToOneConversationHero, canCheat).ToString()
                        , Hero.MainHero.Name, Hero.OneToOneConversationHero.Name, areMarried), Helper.PRINT_TRACE_ROMANCE);

            Helper.Print("Courtship Possible: " + Campaign.Current.Models.RomanceModel.CourtshipPossibleBetweenNPCs(Hero.MainHero, Hero.OneToOneConversationHero).ToString(), Helper.PRINT_TRACE_ROMANCE);
            Helper.Print("Romantic Level: " + Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero).ToString(), Helper.PRINT_TRACE_ROMANCE);
            Helper.Print("Retry Courtship: " + Helper.MASettings.RetryCourtship.ToString(), Helper.PRINT_TRACE_ROMANCE);
            Helper.Print("romanceLevel: " + romanceLevel.ToString(), Helper.PRINT_TRACE_ROMANCE);
#endif

            //if (Campaign.Current.Models.RomanceModel.CourtshipPossibleBetweenNPCs(Hero.MainHero, Hero.OneToOneConversationHero))
            if (MADefaultMarriageModel.IsCoupleSuitableForMarriageStatic(Hero.MainHero, Hero.OneToOneConversationHero, canCheat))
            {
                if (romanceLevel == Romance.RomanceLevelEnum.Untested)
                {
                    if (Hero.OneToOneConversationHero.IsNoble || Hero.OneToOneConversationHero.IsMinorFactionHero)
                    {
                        if (Hero.OneToOneConversationHero.Spouse is null)
                        {
                            MBTextManager.SetTextVariable("FLIRTATION_LINE",
                                flag
                                    ? "{=lord_flirt}My lord, I note that you have not yet taken a spouse."
                                    : "{=v1hC6Aem}My lady, I wish to profess myself your most ardent admirer.", false);
                        }
                        else
                        {
                            MBTextManager.SetTextVariable("FLIRTATION_LINE",
                                flag
                                    ? "{=lord_cheating_flirt}My lord, I note that you might wish for a new spouse."
                                    : "{=v1hC6Aem}My lady, I wish to profess myself your most ardent admirer.", false);
                        }
                    }
                    else
                    {
                        MBTextManager.SetTextVariable("FLIRTATION_LINE",
                            flag
                                ? "{=goodman_flirt}Goodman, I note that you have not yet taken a spouse."
                                : "{=goodwife_flirt}Goodwife, I wish to profess myself your most ardent admirer.", false);
                    }
                    return true;
                }
                else
                {
                    if (Helper.MASettings.RetryCourtship)
                    {
                        if (romanceLevel == Romance.RomanceLevelEnum.FailedInCompatibility
                            || romanceLevel == Romance.RomanceLevelEnum.FailedInPracticalities
                            || (romanceLevel == Romance.RomanceLevelEnum.Ended && !areMarried)
                            )
                        {
                            if (Hero.OneToOneConversationHero.IsNoble || Hero.OneToOneConversationHero.IsMinorFactionHero)
                            {
                                MBTextManager.SetTextVariable("FLIRTATION_LINE",
                                    flag
                                        ? "{=2WnhUBMM}My lord, may you give me another chance to prove myself?"
                                        : "{=4iTaEZKg}My lady, may you give me another chance to prove myself?", false);
                            }
                            else
                            {
                                MBTextManager.SetTextVariable("FLIRTATION_LINE",
                                    flag
                                        ? "{=goodman_chance}Goodman, may you give me another chance to prove myself?"
                                        : "{=goodwife_chance}Goodwife, may you give me another chance to prove myself?", false);
                            }
                            //if (romanceLevel == Romance.RomanceLevelEnum.Ended)
                            //    // OnNeNousDitPasTout/GrandesMaree Patch
                            //    // Patch we must have only have one romance status for each relation
                            //    Util.Util.CleanRomance(Hero.MainHero, Hero.OneToOneConversationHero);

                            //if (romanceLevel == Romance.RomanceLevelEnum.FailedInCompatibility || romanceLevel == Romance.RomanceLevelEnum.Ended)
                            //{
                            //    ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.CourtshipStarted);
                            //}
                            //else if (romanceLevel == Romance.RomanceLevelEnum.FailedInPracticalities)
                            //{
                            //    ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible);
                            //}
                            return true;
                        }
                    }
                }
            }
#if TRACEROMANCE
            Helper.Print("conversation_player_can_open_courtship_on_condition Repond FALSE", Helper.PRINT_TRACE_ROMANCE);
#endif
            return false;
        }

        public static void conversation_player_opens_courtship_on_consequence()
        {
            Romance.RomanceLevelEnum romanceLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);
            Romance.RomanceLevelEnum newRomanceLevel = Romance.RomanceLevelEnum.CourtshipStarted;
            int remove = 0;
            if (romanceLevel == Romance.RomanceLevelEnum.FailedInCompatibility || romanceLevel == Romance.RomanceLevelEnum.Ended)
            {
                if (romanceLevel == Romance.RomanceLevelEnum.Ended)
                    Util.CleanRomance(Hero.MainHero, Hero.OneToOneConversationHero);
                remove = 2;
                newRomanceLevel = Romance.RomanceLevelEnum.CourtshipStarted;
            }
            else if (romanceLevel == Romance.RomanceLevelEnum.FailedInPracticalities) {
                remove = 3;
                newRomanceLevel = Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible;
            }

            if (remove != 0)
            {
                Helper.Print(string.Format("conversation_player_opens_courtship_on_consequence::Remove 2 of relation with {0}", Hero.OneToOneConversationHero), Helper.PRINT_TRACE_ROMANCE);
                ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, -remove, false, true);
            }

            Helper.Print(string.Format("Romance new level swap to {0}", newRomanceLevel), Helper.PrintHow.PrintDisplay | Helper.PrintHow.PrintToLogAndWrite);
            ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, newRomanceLevel);

            return;
        }

        [HarmonyPrefix]
        [HarmonyPatch("conversation_player_opens_courtship_on_consequence")]
        private static bool conversation_player_opens_courtship_on_consequencePrefix()
        {
            conversation_player_opens_courtship_on_consequence();
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch("conversation_romance_at_stage_1_discussions_on_condition")]
        private static bool conversation_romance_at_stage_1_discussions_on_conditionPrefix(ref bool __result)
        {
            if (Hero.OneToOneConversationHero == null)
            {
                __result = false;
                return false;
            }

            Romance.RomanceLevelEnum romanticLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);
#if TRACEROMANCE
            Helper.Print(string.Format("RomanceCampaignBehaviorPatch::conversation_romance_at_stage_1_discussions_on_condition with {0} Difficulty ?= {1} RomanticLevel ?= {2}"
                            , Hero.OneToOneConversationHero.Name.ToString()
                            , Helper.MASettings.Difficulty
                            , romanticLevel.ToString()), Helper.PRINT_TRACE_ROMANCE);
#endif
            if (Helper.MASettings.Difficulty == MASettings.DIFFICULTY_VERY_EASY
               || (Helper.MASettings.Difficulty == MASettings.DIFFICULTY_EASY && !Hero.OneToOneConversationHero.IsNoble && !Hero.OneToOneConversationHero.IsMinorFactionHero))
            {
                if (romanticLevel == Romance.RomanceLevelEnum.CourtshipStarted)
                    ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible);

                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("conversation_romance_at_stage_2_discussions_on_condition")]
        private static bool conversation_romance_at_stage_2_discussions_on_conditionPatch(ref bool __result)
        {
            if (Hero.OneToOneConversationHero == null)
            {
                __result = false;
                return false;
            }

            Romance.RomanceLevelEnum romanticLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);
#if TRACEROMANCE
            Helper.Print(string.Format("conversation_romance_at_stage_1_discussions_on_condition with {0} Difficulty ?= {1} Romantilevle ?= {2}"
                    , Hero.OneToOneConversationHero.Name.ToString()
                    , Helper.MASettings.Difficulty
                    , romanticLevel.ToString()), Helper.PRINT_TRACE_ROMANCE);
#endif
            if (Helper.MASettings.Difficulty == MASettings.DIFFICULTY_VERY_EASY)
                //|| (settings.Difficulty == "Easy" && !Hero.OneToOneConversationHero.IsNoble && !Hero.OneToOneConversationHero.IsMinorFactionHero))
            {
                if (romanticLevel == Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible)
                    ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.CoupleAgreedOnMarriage);
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("conversation_finalize_courtship_for_hero_on_condition")]
        private static bool conversation_finalize_courtship_for_hero_on_conditionPatch(ref bool __result)
        {
            __result = conversation_finalize_courtship_for_hero_on_condition(false);

            return false;
        }

        public static bool conversation_finalize_courtship_for_hero_on_condition(bool MAPath)
        {
            bool ret = true;
            if (Hero.OneToOneConversationHero == null)
                return false;

            Romance.RomanceLevelEnum romanticLevel = Romance.RomanceLevelEnum.Untested;
            Romance.RomanticState? romanticState = null;

            if (ret)
            {
                romanticLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);
                romanticState = Romance.GetRomanticState(Hero.MainHero, Hero.OneToOneConversationHero);
                if (romanticState != null && romanticState.ScoreFromPersuasion == 0)
                    romanticState.ScoreFromPersuasion = 60;

                ret = MADefaultMarriageModel.IsCoupleSuitableForMarriageStatic(Hero.MainHero, Hero.OneToOneConversationHero, false)
                    && (Hero.OneToOneConversationHero.Clan == null || Hero.OneToOneConversationHero.Clan.Leader == Hero.OneToOneConversationHero)
                    && romanticLevel == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage;
            }

            if (ret
                && !MAPath
                && !Helper.MASettings.DifficultyNormalMode
                && Hero.OneToOneConversationHero.Clan != null
                && Hero.OneToOneConversationHero.Clan.Leader == Hero.OneToOneConversationHero
#if CANT_MA_UPPER
                && HeroInteractionHelper.CanIntegreSpouseInHeroClan(Hero.MainHero, Hero.OneToOneConversationHero)
#endif
                )
            {
#if TRACEROMANCE
                Helper.Print(string.Format("RomanceCampaignBehaviorPatch:: conversation_finalize_courtship_for_hero_on_conditionPatch:: with {0} FAIL For MA Path", Hero.OneToOneConversationHero), Helper.PRINT_TRACE_ROMANCE);
#endif
                ret = false;
            }

            if (ret 
                && !MAPath
                && (Hero.OneToOneConversationHero.Clan == null || Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan))
            {
#if TRACEROMANCE
                Helper.Print("RomanceCampaignBehaviorPatch:: conversation_finalize_courtship_for_hero_on_conditionPatch::FAIL because no clan (MARomanceCampaignBehavior work)", Helper.PRINT_TRACE_ROMANCE);
#endif
                ret = false;
            }

#if TRACEROMANCE
            Helper.Print(string.Format("RomanceCampaignBehaviorPatch:: conversation_finalize_courtship_for_hero_on_conditionPatch:: with {0} Difficulty ?= {1} répond {2} romanticState Score ?= {3}"
                    , Hero.OneToOneConversationHero.Name.ToString()
                    , Helper.MASettings.Difficulty
                    , ret
                    , (romanticState != null ? romanticState.ScoreFromPersuasion.ToString() : "NULL")), Helper.PRINT_TRACE_ROMANCE);
#endif
            return ret;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch("conversation_finalize_courtship_for_other_on_condition")]
        //private static bool conversation_finalize_courtship_for_other_on_conditionPatch(ref bool __result)
        //{

        //}


        [HarmonyPatch("conversation_finalize_marriage_barter_consequence")]
        [HarmonyPrefix]
        private static bool conversation_finalize_marriage_barter_consequencePatch(RomanceCampaignBehavior __instance)
        {
            _heroBeingProposedTo = Hero.OneToOneConversationHero;
            if (Hero.OneToOneConversationHero.Clan != null)
            {
                foreach (Hero hero in Hero.OneToOneConversationHero.Clan.Lords)
                {
                    if (Romance.GetRomanticLevel(Hero.MainHero, hero) == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage)
                    {
                        _heroBeingProposedTo = hero;
                        break;
                    }
                }
            }

            BarterManager bmInstance = BarterManager.Instance;
            Hero mainHero = Hero.MainHero;
            Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
            int score = 0;
            if (Romance.GetRomanticState(Hero.MainHero, _heroBeingProposedTo) != null)
            {
                score = (int) Romance.GetRomanticState(Hero.MainHero, _heroBeingProposedTo).ScoreFromPersuasion;
            }
#if TRACEROMANCE
            Helper.Print(string.Format("RomanceCampaignBehaviorPatch:: conversation_finalize_marriage_barter_consequence between {0}\r\n\t and {1}\r\n\t BarterManager ?= {2}"
                                , mainHero.Name.ToString()
                                , Helper.TraceHero(_heroBeingProposedTo)
                                , (bmInstance != null ? "Existe" : "NULL"))
                        , Helper.PrintHow.PrintToLogAndWriteAndForceDisplay);
#endif
            PartyBase mainParty = PartyBase.MainParty;
            MobileParty partyBelongedTo = Hero.OneToOneConversationHero.PartyBelongedTo;

            if (_heroBeingProposedTo.Clan != null
#if V1630LESS
                && _heroBeingProposedTo.Clan.Leader != _heroBeingProposedTo 
#endif
                && _heroBeingProposedTo.Spouse != mainHero)
            {
#if TRACEROMANCE
                Helper.Print("StartBarterOffer", Helper.PRINT_TRACE_ROMANCE);
#endif
#if V1640MORE
                MarriageBarterable marriageBarterable = new MarriageBarterable(Hero.MainHero, PartyBase.MainParty, _heroBeingProposedTo, Hero.MainHero);
                bmInstance.StartBarterOffer(mainHero, oneToOneConversationHero, mainParty, (partyBelongedTo != null) ? partyBelongedTo.Party : null, null, (Barterable barterable, BarterData _args, object obj)
                            => BarterManager.Instance.InitializeMarriageBarterContext(barterable, _args
                                                    , new Tuple<Hero, Hero>(_heroBeingProposedTo, Hero.MainHero))
                                                    , score, false, new Barterable[] { marriageBarterable  });
#else
                bmInstance.StartBarterOffer(mainHero, oneToOneConversationHero, mainParty, (partyBelongedTo != null) ? partyBelongedTo.Party : null, null, (Barterable barterable, BarterData _args, object obj)
                            => BarterManager.Instance.InitializeMarriageBarterContext(barterable, _args
                                                    , new Tuple<Hero, Hero>(heroBeingProposedTo, Hero.MainHero))
                                                    , score, false, null);
#endif
            }
            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.LeaveEncounter = true;
            }
            return false;
        }

        [HarmonyPatch("conversation_marriage_barter_successful_on_consequence")]
        [HarmonyPostfix]
        internal static void conversation_marriage_barter_successful_on_consequencePATCH()
        {
            if (_heroBeingProposedTo != null && MARomanceCampaignBehavior.Instance != null)
                MARomanceCampaignBehavior.Instance.PartnerRemove(_heroBeingProposedTo);
        }



    }
}
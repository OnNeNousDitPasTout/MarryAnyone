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

#if V4 

        [HarmonyPatch("conversation_courtship_initial_reaction_on_condition")]
        [HarmonyPostfix]
        private static void conversation_courtship_initial_reaction_on_conditionPostfix(ref bool __result)
        {
            if (Hero.OneToOneConversationHero != null && __result) 
            {
#if CANTMARRYIFBUSY
                if (Helper.HeroOccupiedAndCantMarried(Hero.OneToOneConversationHero))
                {
#if TRACE
                    Helper.Print("conversation_courtship_initial_reaction_on_condition return false because busy", Helper.PrintHow.PrintToLogAndWrite);
                    //Helper.Print(String.Format("conversation_courtship_decline_reaction_to_player_on_conditionPrefix ?= {0}", __result), Helper.PrintHow.PrintToLogAndWrite);
#endif
                    __result = false;
                }
#endif
                if (Helper.MASettings.RelationLevelMinForRomance >= 0 && Hero.OneToOneConversationHero.GetRelation(Hero.MainHero) < Helper.MASettings.RelationLevelMinForRomance)
                {
#if TRACE
                    Helper.Print("conversation_courtship_initial_reaction_on_condition return false because not enough relation", Helper.PrintHow.PrintToLogAndWrite);
                    //Helper.Print(String.Format("conversation_courtship_decline_reaction_to_player_on_conditionPrefix ?= {0}", __result), Helper.PrintHow.PrintToLogAndWrite);
#endif
                    __result = false;
                }
            }
            else if (Hero.OneToOneConversationHero != null 
                    && !__result
                    && MADefaultMarriageModel.IsCoupleSuitableForMarriageStatic(Hero.MainHero, Hero.OneToOneConversationHero, false)
                    && (Helper.MASettings.RelationLevelMinForRomance == -1
                        || (Helper.MASettings.RelationLevelMinForRomance >= 0 && Hero.OneToOneConversationHero.GetRelation(Hero.MainHero) < Helper.MASettings.RelationLevelMinForRomance)))
            {

                // Retry
                __result = TryToRetryCourtship();
#if TRACE
                Helper.Print(String.Format("conversation_courtship_initial_reaction_on_condition return {0} == true if retry courtship", __result), Helper.PrintHow.PrintToLogAndWrite);
                //Helper.Print(String.Format("conversation_courtship_decline_reaction_to_player_on_conditionPrefix ?= {0}", __result), Helper.PrintHow.PrintToLogAndWrite);
#endif

            }
        }

        [HarmonyPatch("conversation_courtship_decline_reaction_to_player_on_condition")]
        [HarmonyPostfix]
        private static void conversation_courtship_decline_reaction_to_player_on_conditionPostfix(ref bool __result) // RomanceCampaignBehavior __instance, 
        //private static bool conversation_courtship_decline_reaction_to_player_on_conditionPrefix(ref bool __result) // RomanceCampaignBehavior __instance, 
        {
#if TRACE
            Helper.Print(String.Format("conversation_courtship_decline_reaction_to_player_on_conditionPostfix ?= {0}", __result), Helper.PrintHow.PrintToLogAndWrite);
            //Helper.Print(String.Format("conversation_courtship_decline_reaction_to_player_on_conditionPrefix ?= {0}", __result), Helper.PrintHow.PrintToLogAndWrite);
#endif

            if (Hero.OneToOneConversationHero != null && !__result)
            //if (Hero.OneToOneConversationHero != null)
            {
#if CANTMARRYIFBUSY
                if (Helper.HeroOccupiedAndCantMarried(Hero.OneToOneConversationHero))
                {
                    int relation = Hero.OneToOneConversationHero.GetRelation(Hero.MainHero);
                    if (relation < 0)
                        MBTextManager.SetTextVariable("COURTSHIP_DECLINE_REACTION", "{=ma_tooBusy}I am too busy {?PLAYER.GENDER}lady{?}lord{\\?},{newline}  just let me go.", false);
                    else if (relation < Helper.MASettings.RelationLevelMinForRomance)
                        MBTextManager.SetTextVariable("COURTSHIP_DECLINE_REACTION", "{=ma_tooBusyOther0}I am too busy {?PLAYER.GENDER}my lady{?}my lord{\\?},{newline}  we can see this another day.", false);
                    else
                        MBTextManager.SetTextVariable("COURTSHIP_DECLINE_REACTION", "{=ma_tooBusyOtherMinus}Can you help me about my little issue before {?PLAYER.GENDER}my lady{?}my lord{\\?},{newline}  I will be happy to talk about that after free my mind.", false);
                    __result = true;
                    //return false;
                }
#endif
                if (!__result 
                    && Helper.MASettings.RelationLevelMinForRomance >= 0 
                    && Hero.OneToOneConversationHero.GetRelation(Hero.MainHero) < Helper.MASettings.RelationLevelMinForRomance)
                {
                    MBTextManager.SetTextVariable("COURTSHIP_DECLINE_REACTION", "{=ma_notEnoughRelation}Sorry my {?PLAYER.GENDER}lady{?}lord{\\?}, we don't know enough to talk about this subject.", false);
                    __result = true;
                }
            }
            //return true;
        }
#endif

        [HarmonyPatch("conversation_player_eligible_for_marriage_with_conversation_hero_on_condition")]
        [HarmonyPostfix]
        private static void Postfix1(ref bool __result)
        {
            __result = Hero.OneToOneConversationHero != null
#if V4 && CANTMARRYIFBUSY
                && (!Helper.HeroOccupiedAndCantMarried(Hero.OneToOneConversationHero))
#endif
                && Romance.GetCourtedHeroInOtherClan(Hero.MainHero, Hero.OneToOneConversationHero) == null 
                && Campaign.Current.Models.RomanceModel.CourtshipPossibleBetweenNPCs(Hero.MainHero, Hero.OneToOneConversationHero);
#if TRACE
            Helper.Print(String.Format("conversation_player_eligible_for_marriage_with_conversation_hero_on_condition Return {0}", __result), Helper.PrintHow.PrintToLogAndWrite);
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch("RomanceCourtshipAttemptCooldown", MethodType.Getter)]
        private static void RomanceCourtshipAttemptCooldownPostfix2(ref CampaignTime __result)
        {
            if (Helper.MASettings.RetryCourtship)
            {
                __result = CampaignTime.DaysFromNow(1f);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("conversation_player_can_open_courtship_on_condition")]
        private static bool conversation_player_can_open_courtship_on_conditionPrefix1(ref bool __result)
        {
            __result = conversation_player_can_open_courtship_on_condition(false);
            return false;
        }

        // 2 Mode :
        //  forBeginDiscussion TRUE  => MARomance open : I have something to discut
        //  forBeginDiscussion FALSE => Flirtation line
        public static bool conversation_player_can_open_courtship_on_condition(bool forBeginDiscussion)
        {
            if (Hero.OneToOneConversationHero is null)
                return false;

            bool InterlocutorIsMale = !Hero.OneToOneConversationHero.IsFemale;

            //bool areMarried = Util.Util.AreMarried(Hero.MainHero, Hero.OneToOneConversationHero);
            bool areMarried = MARomanceCampaignBehavior.Instance.SpouseOrNot(Hero.MainHero, Hero.OneToOneConversationHero);
            Romance.RomanceLevelEnum romanceLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);

#if TRACEROMANCE
            Helper.Print(String.Format("conversation_player_can_open_courtship_on_condition(forBeginDiscussion ?= {4})::\r\n\tCouple suitable for mariage: {0}\r\n\tbetween{1} and {2} areMarried {3}"
                        , MADefaultMarriageModel.IsCoupleSuitableForMarriageStatic(Hero.MainHero, Hero.OneToOneConversationHero, forBeginDiscussion).ToString()
                        , Hero.MainHero.Name
                        , Hero.OneToOneConversationHero.Name
                        , areMarried
                        , forBeginDiscussion), Helper.PRINT_TRACE_ROMANCE);

            Helper.Print("Courtship Possible: " + Campaign.Current.Models.RomanceModel.CourtshipPossibleBetweenNPCs(Hero.MainHero, Hero.OneToOneConversationHero).ToString(), Helper.PRINT_TRACE_ROMANCE);
            Helper.Print("Romantic Level: " + Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero).ToString(), Helper.PRINT_TRACE_ROMANCE);
            Helper.Print("Retry Courtship: " + Helper.MASettings.RetryCourtship.ToString(), Helper.PRINT_TRACE_ROMANCE);
            Helper.Print("romanceLevel: " + romanceLevel.ToString(), Helper.PRINT_TRACE_ROMANCE);
#endif

            //if (Campaign.Current.Models.RomanceModel.CourtshipPossibleBetweenNPCs(Hero.MainHero, Hero.OneToOneConversationHero))
            if (MADefaultMarriageModel.IsCoupleSuitableForMarriageStatic(Hero.MainHero, Hero.OneToOneConversationHero, forBeginDiscussion))
            {
                if (romanceLevel == Romance.RomanceLevelEnum.Untested)
                {
                    if (Hero.OneToOneConversationHero.IsNoble || Hero.OneToOneConversationHero.IsMinorFactionHero)
                    {
                        if (Hero.OneToOneConversationHero.Spouse is null)
                        {
                            MBTextManager.SetTextVariable("FLIRTATION_LINE",
                                InterlocutorIsMale
                                    ? "{=lord_flirt}My lord, I note that you have not yet taken a spouse."
                                    : "{=v1hC6Aem}My lady, I wish to profess myself your most ardent admirer.", false);
                        }
                        else
                        {
                            MBTextManager.SetTextVariable("FLIRTATION_LINE",
                                InterlocutorIsMale
                                    ? "{=lord_cheating_flirt}My lord, I note that you might wish for a new spouse."
                                    : "{=v1hC6Aem}My lady, I wish to profess myself your most ardent admirer.", false);
                        }
                    }
                    else
                    {
                        MBTextManager.SetTextVariable("FLIRTATION_LINE",
                            InterlocutorIsMale
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
                                    InterlocutorIsMale
                                        ? "{=2WnhUBMM}My lord, may you give me another chance to prove myself?"
                                        : "{=4iTaEZKg}My lady, may you give me another chance to prove myself?", false);
                            }
                            else
                            {
                                MBTextManager.SetTextVariable("FLIRTATION_LINE",
                                    InterlocutorIsMale
                                        ? "{=goodman_chance}Goodman, may you give me another chance to prove myself?"
                                        : "{=goodwife_chance}Goodwife, may you give me another chance to prove myself?", false);
                            }
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

        public static bool TryToRetryCourtship()
        {
            bool ret = false;
            Romance.RomanceLevelEnum romanceLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);
            Romance.RomanceLevelEnum newRomanceLevel = Romance.RomanceLevelEnum.CourtshipStarted;
            int remove = 0;
            if (romanceLevel == Romance.RomanceLevelEnum.FailedInCompatibility || romanceLevel == Romance.RomanceLevelEnum.Ended)
            {
                if (romanceLevel == Romance.RomanceLevelEnum.Ended)
                    Util.CleanRomance(Hero.MainHero, Hero.OneToOneConversationHero);
                remove = 2;
                newRomanceLevel = Romance.RomanceLevelEnum.CourtshipStarted;
                ret = true;
            }
            else if (romanceLevel == Romance.RomanceLevelEnum.FailedInPracticalities) {
                remove = 3;
                newRomanceLevel = Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible;
                ret = true;
            }

            if (remove != 0)
            {
#if TRACEROMANCE
                Helper.Print(string.Format("tryToRetryCourtship::Remove 2 of relation with {0}", Hero.OneToOneConversationHero), Helper.PRINT_TRACE_ROMANCE);
#endif
                ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, -remove, false, true);
            }

#if TRACEROMANCE
            Helper.Print(string.Format("tryToRetryCourtship::Romance new level swap to {0}", newRomanceLevel), Helper.PrintHow.PrintDisplay | Helper.PrintHow.PrintToLogAndWrite);
#endif
            ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, newRomanceLevel);

            return ret;
        }

        [HarmonyPatch("conversation_player_opens_courtship_on_consequence")]
        [HarmonyPrefix]
        private static bool conversation_player_opens_courtship_on_consequencePrefix()
        {
            // Patch not HEAR
            //conversation_player_opens_courtship_on_consequence();
            // => conversation_courtship_reaction_to_player_on_condition
#if TRACEROMANCE
            Helper.Print("conversation_player_opens_courtship_on_consequencePrefix::return false for inhib the beginMarriage", Helper.PRINT_TRACE_ROMANCE);
#endif
            return false;
        }

        [HarmonyPatch("conversation_courtship_reaction_to_player_on_condition")]
        [HarmonyPostfix]
        private static void conversation_courtship_reaction_to_player_on_conditionPostfix(ref bool __result)
        {
            if (__result)
            {
#if TRACEROMANCE
                Helper.Print("conversation_courtship_reaction_to_player_on_condition:: call TryToRetryCourtship", Helper.PRINT_TRACE_ROMANCE);
#endif
                TryToRetryCourtship();
            }
            return;
        }

        [HarmonyPatch("conversation_romance_at_stage_1_discussions_on_condition")]
        [HarmonyPrefix]
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
                && (Helper.MASettings.CanJoinUpperClanThroughMAPath
                    ||  HeroInteractionHelper.CanIntegreSpouseInHeroClan(Hero.MainHero, Hero.OneToOneConversationHero))
                )
            {
#if TRACEROMANCE
                Helper.Print(string.Format("RomanceCampaignBehaviorPatch:: conversation_finalize_courtship_for_hero_on_conditionPatch:: with {0}\r\n\r FAIL normal Path For MA Path", Hero.OneToOneConversationHero), Helper.PRINT_TRACE_ROMANCE);
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
﻿using HarmonyLib;
using MarryAnyone.Models;
using MarryAnyone.Settings;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;
using TaleWorlds.Localization;

namespace MarryAnyone.Behaviors
{
    internal class MARomanceCampaignBehavior : CampaignBehaviorBase
    {
        protected void AddDialogs(CampaignGameStarter starter)
        {
            foreach (Hero hero in Hero.AllAliveHeroes.ToList())
            {
                // The old fix for occupations not sticking
                if (hero.Spouse == Hero.MainHero || Hero.MainHero.ExSpouses.Contains(hero))
                {
                    MAHelper.OccupationToLord(hero.CharacterObject);
                    MAHelper.PatchHeroPlayerClan(hero);
                }
            }
            
            // To begin the dialog for companions
            starter.AddPlayerLine("main_option_discussions_MA", "hero_main_options", "lord_talk_speak_diplomacy_MA", "{=lord_conversations_343}There is something I'd like to discuss.", new ConversationSentence.OnConditionDelegate(conversation_begin_courtship_for_hero_on_condition), null, 120, null, null);
            //starter.AddPlayerLine("main_option_discussions_MA", "hero_main_options", "lord_start_courtship_response", "{=OD1m1NYx}{STR_INTRIGUE_AGREEMENT}", new ConversationSentence.OnConditionDelegate(conversation_begin_courtship_for_hero_on_conditionFromMain), new ConversationSentence.OnConsequenceDelegate(this.conversation_start_courtship_persuasion_pt1_on_consequence), 120, null, null);
            starter.AddDialogLine("character_agrees_to_discussion_MA", "lord_talk_speak_diplomacy_MA", "lord_talk_speak_diplomacy_2", "{=OD1m1NYx}{STR_INTRIGUE_AGREEMENT}", new ConversationSentence.OnConditionDelegate(conversation_character_agrees_to_discussion_on_condition), null, 100, null);

            // From previous iteration
            //starter.AddDialogLine("persuasion_leave_faction_npc_result_success_2", "lord_conclude_courtship_stage_2", "close_window", "{=k7nGxksk}Splendid! Let us conduct the ceremony, then.", new ConversationSentence.OnConditionDelegate(conversation_finalize_courtship_for_hero_on_condition), new ConversationSentence.OnConsequenceDelegate(conversation_courtship_success_on_consequence), 140, null);
            starter.AddDialogLine("hero_courtship_persuasion_2_success", "lord_start_courtship_response_3", "lord_conclude_courtship_stage_2", "{=xwS10c1b}Yes... I think I would be honored to accept your proposal.", new ConversationSentence.OnConditionDelegate(conversation_finalize_courtship_for_hero_on_condition), null, 120, null);

            //starter.AddPlayerLine("hero_romance_task", "hero_main_options", "lord_start_courtship_response_3", "{=cKtJBdPD}I wish to offer my hand in marriage.", new ConversationSentence.OnConditionDelegate(conversation_finalize_courtship_for_hero_on_condition), null, 140, null, null);
            //starter.AddPlayerLine("hero_romance_conclusion_direct", "hero_main_options", "hero_courtship_final_barter_conclusion", "{=2aW6NC3Q}Let us discuss the final terms of our marriage.", new ConversationSentence.OnConditionDelegate(this.conversation_finalize_courtship_for_hero_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_courtship_success_on_consequence), 90, null, null);
            starter.AddPlayerLine("hero_romance_conclusion_direct", "hero_main_options", "close_window", "{=2aW6NC3Q}Let us discuss the final terms of our marriage.", new ConversationSentence.OnConditionDelegate(this.conversation_finalize_courtship_for_hero_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_courtship_success_on_consequence), 90, null, null);
            //starter.AddPlayerLine("hero_romance_task_pt3a", "hero_main_options", "hero_courtship_final_barter", "{=2aW6NC3Q}Let us discuss the final terms of our marriage.", new ConversationSentence.OnConditionDelegate(this.conversation_finalize_courtship_for_hero_on_condition), null, 100, null, null);
            starter.AddPlayerLine("hero_romance_task_pt3b", "hero_main_options", "hero_courtship_final_barter", "{=jd4qUGEA}I wish to discuss the final terms of my marriage with {COURTSHIP_PARTNER}.", new ConversationSentence.OnConditionDelegate(this.conversation_finalize_courtship_for_other_on_condition), null, 100, null, null);
         
            starter.AddDialogLine("persuasion_leave_faction_npc_result_success_2", "lord_conclude_courtship_stage_2", "close_window", "{=k7nGxksk}Splendid! Let us conduct the ceremony, then.", new ConversationSentence.OnConditionDelegate(conversation_finalize_courtship_for_hero_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_courtship_success_on_consequence), 140, null);

            //starter.AddDialogLine("hero_courtship_final_barter_setup", "hero_courtship_final_barter_conclusion", "close_window", "{=k7nGxksk}Splendid! Let us conduct the ceremony, then.", new ConversationSentence.OnConditionDelegate(this.conversation_marriage_barter_successful_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_courtship_success_on_consequence), 100, null);
            //starter.AddDialogLine("hero_courtship_final_barter_setup", "hero_courtship_final_barter_conclusion", "close_window", "{=iunPaMFv}I guess we should put this aside, for now. But perhaps we can speak again at a later date.", () => !this.conversation_marriage_barter_successful_on_condition(), null, 100, null);
        }

        private bool conversation_begin_courtship_for_hero_on_condition()
        {
            if (Hero.OneToOneConversationHero != null) {

                bool ret = MarryAnyone.Patches.Behaviors.RomanceCampaignBehaviorPatch.conversation_player_can_open_courtship_on_condition();
#if TESTROMANCE
                MAHelper.Print(String.Format("MARomanceCampaignBehavior:: avec {0} va répondre {1}"
                                , MAHelper.TraceHero(Hero.OneToOneConversationHero)
                                , ret.ToString()), MAHelper.PRINT_TEST_ROMANCE);
#endif

                if (ret) { 
#if V2
                    ret = Hero.OneToOneConversationHero.IsWanderer || Hero.OneToOneConversationHero.IsPlayerCompanion;
#else
                    ret = Hero.OneToOneConversationHero.IsWanderer && Hero.OneToOneConversationHero.IsPlayerCompanion;
#endif
                }

                // OnNeNousDitPasTout/GrandesMaree Patch
                // In Fact we can't go through Romance.RomanceLevelEnum.Untested
                // because for the next modification, there will be another romance status
                // And we must have only have one romance status for each relation
                if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.Untested)
                {
                    Util.Util.CleanRomance(Hero.MainHero, Hero.OneToOneConversationHero);
                    bool areMarried = Util.Util.AreMarried(Hero.MainHero, Hero.OneToOneConversationHero);
                    if (areMarried)
                    {
                        ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.Ended);
                        MAHelper.Print("PATCH Married New Romantic Level: " + Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero).ToString(), MAHelper.PRINT_PATCH);
                    }
                }
#if TESTROMANCE
                MAHelper.Print(String.Format("conversation_begin_courtship_for_hero_on_condition(V2) with {0} va répondre {1}"
                        , Hero.OneToOneConversationHero.Name
                        , ret.ToString()), MAHelper.PRINT_TEST_ROMANCE | MAHelper.PrintHow.PrintToLogAndWrite);
#endif
                return ret;
            }
            return false;
        }

//#if V2
//        private bool conversation_begin_courtship_for_hero_on_conditionFromMain()
//        {
//            if (Hero.OneToOneConversationHero != null) { 
//                if (Hero.OneToOneConversationHero.IsWanderer ^ Hero.OneToOneConversationHero.IsPlayerCompanion)
//                {
//                    bool ret = MarryAnyone.Patches.Behaviors.RomanceCampaignBehaviorPatch.conversation_player_can_open_courtship_on_condition();

//     //               ISettingsProvider settings = new MASettings();
//					//if (Campaign.Current.Models.RomanceModel.CourtshipPossibleBetweenNPCs(Hero.MainHero, Hero.OneToOneConversationHero) 
//     //                   && Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.Untested)
//					//{
//					//	if (Hero.MainHero.IsFemale)
//					//	{
//					//		MBTextManager.SetTextVariable("STR_INTRIGUE_AGREEMENT", "{=bjJs0eeB}My lord, I note that you have not yet taken a wife.", false);
//					//	}
//					//	else
//					//	{
//					//		MBTextManager.SetTextVariable("STR_INTRIGUE_AGREEMENT", "{=v1hC6Aem}My lady, I wish to profess myself your most ardent admirer", false);
//					//	}
//     //                   ret = true;
//					//}
//     //               else
//     //               if (settings.RetryCourtship && Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(Hero.MainHero, Hero.OneToOneConversationHero))
//					//{
//					//	if (Hero.MainHero.IsFemale)
//					//	{
//					//		MBTextManager.SetTextVariable("STR_INTRIGUE_AGREEMENT", "{=2WnhUBMM}My lord, may you give me another chance to prove myself?", false);
//					//	}
//					//	else
//					//	{
//					//		MBTextManager.SetTextVariable("STR_INTRIGUE_AGREEMENT", "{=4iTaEZKg}My lady, may you give me another chance to prove myself?", false);
//					//	}
//     //                   ret = true;
//					//}

//                    MAHelper.Print(String.Format("conversation_begin_courtship_for_hero_on_conditionFromMain(V2) with {0} va répondre {1}", Hero.OneToOneConversationHero.Name, ret.ToString()), MAHelper.PRINT_TEST_ROMANCE);
//                    return true;
//                }
//            }
//            return false;

//        }
//#endif

        private bool conversation_character_agrees_to_discussion_on_condition()
        {
            MBTextManager.SetTextVariable("STR_INTRIGUE_AGREEMENT", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_lord_intrigue_accept", CharacterObject.OneToOneConversationCharacter));
            return true;
        }

        // This will either skip or continue romance
        // CoupleAgreedOnMarriage = triggers marriage before bartering
        // CourtshipStarted = skip everything
        // return false = carry out entire romance
        private bool conversation_finalize_courtship_for_hero_on_condition()
        {

            Romance.RomanceLevelEnum romanticLevel = Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero);
            Romance.RomanticState romanticState = Romance.GetRomanticState(Hero.MainHero, Hero.OneToOneConversationHero);


            bool ret = Campaign.Current.Models.RomanceModel.CourtshipPossibleBetweenNPCs(Hero.MainHero, Hero.OneToOneConversationHero)
                        && (Hero.OneToOneConversationHero.Clan == null 
                            || Hero.OneToOneConversationHero.Clan.Leader == Hero.OneToOneConversationHero
                            || Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan);
            if (ret)
            {
                if ((romanticLevel == Romance.RomanceLevelEnum.CourtshipStarted
                       || romanticLevel == Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible) 
                        && MAHelper.MASettings.Difficulty == MASettings.DIFFICULTY_VERY_EASY)
                {
                    ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.CoupleAgreedOnMarriage);
                    romanticLevel = Romance.RomanceLevelEnum.CoupleAgreedOnMarriage;
                }
                ret = (romanticLevel == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage);
            }

//            if (ret && Hero.OneToOneConversationHero.Clan != null && Hero.OneToOneConversationHero.Clan.Leader != Hero.OneToOneConversationHero)
//            {
//#if TESTROMANCE
//                MAHelper.Print(string.Format("MARomanceCampaignBehavior:: conversation_finalize_courtship_for_hero_on_condition with {0} goto defaut Programme", Hero.OneToOneConversationHero.Name), MAHelper.PRINT_TEST_ROMANCE);
//#endif
//                ret = false;
//            }

            if (ret && romanticState != null && romanticState.ScoreFromPersuasion == 0)
                romanticState.ScoreFromPersuasion = 60;

#if TESTROMANCE
            MAHelper.Print(string.Format("MARomanceCampaignBehavior:: conversation_finalize_courtship_for_hero_on_condition with {0} \r\n\tDifficulty ?= {1} RommanticLevel ?= {2}\r\n\trépond {3} \r\n\tromanticState Score ?= {4}"
                    , Hero.OneToOneConversationHero.Name.ToString()
                    , MAHelper.MASettings.Difficulty
                    , romanticLevel.ToString()
                    , ret
                    , (romanticState != null ? romanticState.ScoreFromPersuasion.ToString() : "NULL")), MAHelper.PRINT_TEST_ROMANCE);
#endif
            return ret;
        }

        private bool conversation_finalize_courtship_for_other_on_condition()
        {

            if (Hero.OneToOneConversationHero != null)
            {
                Clan clan = Hero.OneToOneConversationHero.Clan;
                if (clan != null && clan.Leader == Hero.OneToOneConversationHero)
                {
                    foreach (Hero hero in clan.Lords)
                    {
                        if (hero != Hero.OneToOneConversationHero 
                            && Campaign.Current.Models.RomanceModel.CourtshipPossibleBetweenNPCs(Hero.MainHero, hero) 
                            && Romance.GetRomanticLevel(Hero.MainHero, hero) == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage)
                        {
                            MBTextManager.SetTextVariable("COURTSHIP_PARTNER", hero.Name, false);
#if TESTROMANCE
                            MAHelper.Print("MARomanceCampaignBehavior:: conversation_finalize_courtship_for_other_on_condition => TEST SUCCESS", MAHelper.PRINT_TEST_ROMANCE);
#endif
                            return true;
                        }
                    }
                }
            }
#if TESTROMANCE
            MAHelper.Print("MARomanceCampaignBehavior:: conversation_finalize_courtship_for_other_on_condition => FAIL", MAHelper.PRINT_TEST_ROMANCE);
#endif
            return false;
        }

        private bool conversation_marriage_barter_successful_on_condition()
        {
            return Campaign.Current.BarterManager.LastBarterIsAccepted;
        }

        private void conversation_courtship_success_on_consequence()
        {
            Hero hero = Hero.MainHero;
            Hero spouse = Hero.OneToOneConversationHero;
            Hero oldSpouse = hero.Spouse;
            Hero cheatedSpouse = spouse.Spouse;

#if TRACEWEDDING
            MAHelper.Print("MARomanceCampaignBehavior:: conversation_courtship_success_on_consequence", MAHelper.PRINT_TRACE_WEDDING);
#endif
            // If you are marrying a kingdom ruler as a kingdom ruler yourself,
            // the kingdom ruler will have to give up being clan head.
            // Apparently causes issues if this is not done.
            if (spouse.IsFactionLeader && !spouse.IsMinorFactionHero)
            {
                if (hero.Clan.Kingdom != spouse.Clan.Kingdom)
                {
                    if (hero.Clan.Kingdom?.Leader != hero)
                    {
                        // Join kingdom due to lowborn status
                        if (hero.Clan.Leader == Hero.MainHero)
                        {
                            ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(hero.Clan);
                            if (hero.Clan.Leader == Hero.MainHero)
                            {
                                MAHelper.Print("No Heirs", MAHelper.PRINT_TRACE_WEDDING);
                                DestroyClanAction.Apply(hero.Clan);
                                MAHelper.Print("Eliminated Player Clan", MAHelper.PRINT_TRACE_WEDDING);
                            }
                        }
                        foreach (Hero companion in hero.Clan.Companions.ToList())
                        {
                            bool inParty = false;
                            if (companion.PartyBelongedTo == MobileParty.MainParty)
                            {
                                inParty = true;
                            }
                            RemoveCompanionAction.ApplyByFire(hero.Clan, companion);
                            AddCompanionAction.Apply(spouse.Clan, companion);
                            if (inParty)
                            {
                                AddHeroToPartyAction.Apply(companion, MobileParty.MainParty, true);
                            }
                        }
                        hero.Clan = spouse.Clan;
                        var current = Traverse.Create<Campaign>().Property("Current").GetValue<Campaign>();
                        Traverse.Create(current).Property("PlayerDefaultFaction").SetValue(spouse.Clan);
                        MAHelper.Print("Lowborn Player Married to Kingdom Ruler", MAHelper.PRINT_TRACE_WEDDING);
                    }
                    else
                    {
                        ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(spouse.Clan);
                        MAHelper.Print("Kingdom Ruler Stepped Down and Married to Player", MAHelper.PRINT_TRACE_WEDDING);
                    }
                }
            }
            else if (spouse.IsFactionLeader && spouse.IsMinorFactionHero)
            {
                ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(spouse.Clan);
                MAHelper.Print("MinorFaction Ruler Stepped Down and Married to Player", MAHelper.PRINT_TRACE_WEDDING);
            }

            if (spouse.Clan == null) // Patch V2
            {
                spouse.Clan = hero.Clan;
                MAHelper.Print("Spouse Swap clan", MAHelper.PRINT_TRACE_WEDDING);
            }

            // New nobility
            MAHelper.OccupationToLord(spouse.CharacterObject);
            if (!spouse.IsNoble)
            {
                spouse.IsNoble = true;
                MAHelper.Print("Spouse to Noble", MAHelper.PRINT_TRACE_WEDDING);
            }

#if V1640
            if (hero.Clan.Lords.FirstOrDefault(x => x == spouse) == null)
            {
                hero.Clan.Lords.AddItem(spouse);
                MAHelper.Print("Add Spouse to Noble", MAHelper.PRINT_TRACE_WEDDING);
            }
#endif

            // Dodge the party crash for characters part 1
            bool dodge = false;
            if (spouse.PartyBelongedTo == MobileParty.MainParty)
            {
                AccessTools.Property(typeof(Hero), "PartyBelongedTo").SetValue(spouse, null, null);
                MAHelper.Print("Spouse Already in Player's Party", MAHelper.PRINT_TRACE_WEDDING);
                dodge = true;
            }
            // Apply marriage
            ChangeRomanticStateAction.Apply(hero, spouse, Romance.RomanceLevelEnum.Marriage);
            MAHelper.Print("Marriage Action Applied", MAHelper.PRINT_TRACE_WEDDING);

            if (oldSpouse is not null)
            {
                MAHelper.RemoveExSpouses(oldSpouse);
            }
            // Dodge the party crash for characters part 2
            if (dodge)
            {
                AccessTools.Property(typeof(Hero), "PartyBelongedTo").SetValue(spouse, MobileParty.MainParty, null);
            }
            // Activate character if not already activated
            if (!spouse.HasMet)
            {
                spouse.HasMet = true;
            }
            if (!spouse.IsActive)
            {
                spouse.ChangeState(Hero.CharacterStates.Active);
                MAHelper.Print("Activated Spouse", MAHelper.PRINT_TRACE_WEDDING);
            }
            if (spouse.IsPlayerCompanion)
            {
                spouse.CompanionOf = null;
                MAHelper.Print("Spouse No Longer Companion", MAHelper.PRINT_TRACE_WEDDING);
            }
            if (MAHelper.MASettings.Cheating && cheatedSpouse is not null)
            {
                MAHelper.RemoveExSpouses(cheatedSpouse, true);
                MAHelper.RemoveExSpouses(spouse, true);
                MAHelper.Print("Spouse Broke Off Past Marriage", MAHelper.PRINT_TRACE_WEDDING);
            }

#if TRACEWEDDING
            MAHelper.Print(String.Format("Spouse => {0}", MAHelper.TraceHero(spouse)), MAHelper.PRINT_TRACE_WEDDING);
#endif
            MAHelper.RemoveExSpouses(hero);
            MAHelper.RemoveExSpouses(spouse);

            if (PlayerEncounter.Current != null)
                PlayerEncounter.LeaveEncounter = true;

            // New fix to stop some kingdom rulers from disappearing
            if (spouse.PartyBelongedTo != MobileParty.MainParty)
            {
                AddHeroToPartyAction.Apply(spouse, MobileParty.MainParty, true);
            }

            //if (PlayerEncounter.Current != null)
            //{
            //    PlayerEncounter.LeaveEncounter = true;
            //}
        }

        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            foreach (Hero hero in Hero.AllAliveHeroes.ToList())
            {
                // The old fix for occupations not sticking
                if (hero.Spouse == Hero.MainHero || Hero.MainHero.ExSpouses.Contains(hero))
                {
                    MAHelper.OccupationToLord(hero.CharacterObject);
                    MAHelper.PatchHeroPlayerClan(hero);
                }
            }

            AddDialogs(campaignGameStarter);
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
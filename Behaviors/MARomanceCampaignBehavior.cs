using HarmonyLib;
using Helpers;
using MarryAnyone.Helpers;
using MarryAnyone.Models;
using MarryAnyone.Patches;
using MarryAnyone.Patches.Behaviors;
using MarryAnyone.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace MarryAnyone.Behaviors
{
    internal class MARomanceCampaignBehavior : CampaignBehaviorBase
    {

        #region variables
        public List<Hero>? Partners;

        public List<Hero>? NoMoreSpouse;

        private Version? SaveVersion;
        private bool _hasLoading = false;

        private List<PersuasionAttempt>? _previousCheatPersuasionAttempts;

        private List<PersuasionTask>? _allReservations;
        private float _maximumScoreCap;

        // Token: 0x040010C3 RID: 4291
        private float _successValue = 1f;

        // Token: 0x040010C4 RID: 4292
        private float _criticalSuccessValue = 2f;

        // Token: 0x040010C5 RID: 4293
        private float _criticalFailValue = 2f;

        // Token: 0x040010C6 RID: 4294
        private float _failValue = 1f;

        private bool _MAWedding = false;

        #endregion

        public static MARomanceCampaignBehavior? Instance;

        public void PartnerRemove(Hero hero)
        {

            if (Partners != null)
            {
                while (true)
                {
                    if (!Partners.Remove(hero))
                        break;
                }
                if (Partners.Count == 0)
                    Partners = null;
            }
        } 

        #region vie de l'objet
        public MARomanceCampaignBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));
            CampaignEvents.HeroesMarried.AddNonSerializedListener(this, new Action<Hero, Hero, bool>(OnHeroesMarried));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(OnHourTickEvent));
        }

        public void Dispose()
        {
            Partners = null;
            NoMoreSpouse = null;
            _previousCheatPersuasionAttempts = null;
            _allReservations = null;
            PregnancyCampaignBehaviorPatch.Done();
            Instance = null;
        }

        #endregion

        #region Spouses

        public bool PartnerOfPlayer(Hero partner)
        {
            if (Partners != null && Partners.IndexOf(partner) >= 0)
                return true;
            return false;
        }

        public bool SpouseOfPlayer(Hero spouse)
        {
            return ((Hero.MainHero.Spouse == spouse 
                    || Hero.MainHero.ExSpouses.IndexOf(spouse) >= 0)
                    && (NoMoreSpouse == null || NoMoreSpouse.IndexOf(spouse) < 0));
        }

        public bool SpouseOrNot(Hero spouseA, Hero spouseB)
        {
            if (spouseA == Hero.MainHero) {
                if (NoMoreSpouse != null && NoMoreSpouse.IndexOf(spouseB) >= 0)
                    return false;
                if (Hero.MainHero.ExSpouses.IndexOf(spouseB) >= 0)
                    return true;
                if (Partners != null && Partners.IndexOf(spouseB) >= 0)
                    return false;
            }
            if (spouseB == Hero.MainHero) {
                if (NoMoreSpouse != null && NoMoreSpouse.IndexOf(spouseA) >= 0)
                    return false;
                if (Hero.MainHero.ExSpouses.IndexOf(spouseA) >= 0)
                    return true;
                if (Partners != null && Partners.IndexOf(spouseA) >= 0)
                    return false;
            }

            return spouseA.Spouse == spouseB || spouseB.Spouse == spouseA;
        }

        public Hero? FirstHeroExSpouseOkToDoIt()
        {
            Hero? spouse = null;
            if (Hero.MainHero.ExSpouses != null)
            {
                spouse = Hero.MainHero.ExSpouses.LastOrDefault(h => h.IsAlive && NoMoreSpouse.IndexOf(h) < 0 && HeroInteractionHelper.OkToDoIt(Hero.MainHero, h));
            }
            return spouse;
        }

        public Hero? FirstHeroExSpouse()
        {
            Hero? spouse = null;
            if (Hero.MainHero.ExSpouses != null)
            {
                spouse = Hero.MainHero.ExSpouses.LastOrDefault(h => h.IsAlive && NoMoreSpouse.IndexOf(h) < 0);
            }
            return spouse;
        }

        public List<Hero> Spouses
        {
            get
            {
                List<Hero> spouses = Hero.MainHero.ExSpouses.ToList();
                if (Hero.MainHero.Spouse != null)
                {
                    if (spouses.Count > 0 )
                        spouses.Insert(0, Hero.MainHero.Spouse);
                    else
                        spouses.Add(Hero.MainHero.Spouse);
                }

                if (Partners != null)
                    spouses.RemoveAll(x => Partners.IndexOf(x) >= 0);

                if (NoMoreSpouse != null)
                    spouses.RemoveAll(x => NoMoreSpouse.IndexOf(x) >= 0);

                return spouses;
            }
        }

        #endregion

#if TRACKTOMUCHSPOUSE

        public static void VerifySpouse(int diff, string prefix)
        {

            if (Instance != null)
            {
                List<Hero> spouses = Instance.Spouses;
                if (TrackTooMuchSpouse.Instance().Verify(spouses, diff, prefix))
                {
                    TrackTooMuchSpouse.Instance().Validate(spouses);
                }
            }
        }
#endif

        #region dialogues
        protected void AddDialogs(CampaignGameStarter starter)
        {
            
            // To begin the dialog for companions
            starter.AddPlayerLine("main_option_discussions_MA", "hero_main_options", "lord_talk_speak_diplomacy_MA", "{=lord_conversations_343}There is something I'd like to discuss."
                                                , new ConversationSentence.OnConditionDelegate(conversation_begin_courtship_for_hero_on_condition)
                                                , null
                                                , 120, null, null);
            starter.AddDialogLine("character_agrees_to_discussion_MA", "lord_talk_speak_diplomacy_MA", "lord_talk_speak_diplomacy_2", "{=OD1m1NYx}{STR_INTRIGUE_AGREEMENT}"
                                                , new ConversationSentence.OnConditionDelegate(conversation_character_agrees_to_discussion_on_condition)
                                                , null, 100, null);
            // => Go to RomanceCampaingBehavior 

            //starter.AddPlayerLine("player_start_courtship_MA", "lord_talk_speak_diplomacy_2", "lord_start_courtship_response", "{=!}{FLIRTATION_LINE}"
            //                                    , conversation_begin_courtship_for_hero_on_condition
            //                                    , null //new ConversationSentence.OnConsequenceDelegate(this.conversation_start_courtship_persuasion_pt1_on_consequence)
            //                                    , 120, null, null);

            //starter.AddDialogLine("lord_start_courtship_response", "lord_start_courtship_response", "lord_start_courtship_response_player_offer", "{=!}{INITIAL_COURTSHIP_REACTION}"
            //                                    , conversation_courtship_initial_reaction_on_condition
            //                                    , null
            //                                    , 100, null);

            // Bad
            //starter.AddDialogLine("interlocutor_agrees_to_discussion_MA", "lord_start_courtship_response", "lord_start_courtship_response", "{=OD1m1NYx}{STR_INTRIGUE_AGREEMENT}"
            //                                    , conversation_character_agrees_to_discussion_on_condition
            //                                    , RomanceCampaignBehaviorPatch.conversation_player_opens_courtship_on_consequence
            //                                    , 100, null);

            starter.AddPlayerLine("player_cheat_persuasion_start", "lord_talk_speak_diplomacy_2", "acceptcheatingornot"
                                                , "{=Cheat_engage_courtship}I would love to spend some time with you, can you join my party for a few days ?"
                                                , conversation_characacter_agreed_to_cheat
                                                , null //conversation_characacter_test_to_cheat
                                                , 100, null);

            starter.AddPlayerLine("player_Divorce_start", "lord_talk_speak_diplomacy_2", "goodbySpouse", "{=Divorce_engage_dialog}We have been together for too long my {RELATION_TEXT}{newline}, I think it is time to explore new horizons. I want a divorce {INTERLOCUTOR.NAME}."
                                                , conversation_can_divorce
                                                , delegate { conversation_do_divorce(false); }
                                                , 80, null);

            starter.AddPlayerLine("player_DivorceBug_start", "lord_talk_speak_diplomacy_2", "close_window", "{=DivorceBug_engage_dialog}There is a bug in the Marry Anyone, juste leave my team {INTERLOCUTOR.NAME}!"
                                                , conversation_can_divorce
                                                , delegate { conversation_do_divorce(true); }
                                                , 60, null);

            starter.AddPlayerLine("player_LeaveCheat_start", "lord_talk_speak_diplomacy_2", "close_window"
                                                , "{=LeaveCheat_engage_dialog}I think you need to go away {INTERLOCUTOR.NAME}, I have other things to do."
                                                , conversation_can_LeaveCheat
                                                , conversaion_do_LeaveCheat 
                                                , 60, null);

            starter.AddDialogLine("hero_leave_party", "goodbySpouse", "close_window", "{=LeaveSpouseParty}Hope we meet again, maybe in the arena{newline}, so long ..."
                                                , null
                                                , null
                                                , 100, null);

            starter.AddDialogLine("hero_cheat_persuasion_no", "acceptcheatingornot", "lort_pretalk", "{=!}{CHEAT_DECLINE_REACTION}"
                                                , conversation_characacter_notagreed_to_cheat_VariantTest
                                                , null
                                                , 120, null);

            starter.AddDialogLine("hero_cheat_persuasion_start_nomore", "acceptcheatingornot", "lort_pretalk", "{=allready_reply}I have already given you my answer, off you go"
                                                , delegate { return !conversation_characacter_notagreed_to_cheat_VariantTest() && conversation_cheat_allready_done(); }
                                                , null
                                                , 100, null);

            starter.AddDialogLine("hero_cheat_persuasion_start", "acceptcheatingornot", "heroPersuasionNextQuestion", "{=bW3ygxro}Yes, it's good to have a chance to get to know each other."
                                                , delegate { return !conversation_characacter_notagreed_to_cheat_VariantTest() && !conversation_cheat_allready_done(); }
                                                , conversation_characacter_test_to_cheat
                                                , 80, null);

            starter.AddDialogLine("hero_cheat_persuasion_fail", "heroPersuasionNextQuestion", "lort_pretalk", "{=!}{FAILED_PERSUASION_LINE}"
                                                , persuasion_fail
                                                , conversation_characacter_fail_to_cheat_go
                                                , 100, null);

            //starter.AddDialogLine("hero_cheat_persuasion_next1", "acceptcheatingornot", "heroPersuasionQuestion", "{=bW3ygxro}Yes, it's good to have a chance to get to know each other.", null, null, 100, null);
            starter.AddDialogLine("hero_cheat_persuasion_attempt", "heroPersuasionNextQuestion", "player_courtship_argument", "{=!}{PERSUASION_TASK_LINE}"
                                                , persuasion_go_nextStep
                                                , null
                                                , 100, null);

            starter.AddDialogLine("hero_cheat_persuasion_success", "heroPersuasionNextQuestion", "close_window", "{=Cheat_success}Yes, let's have some fun ! I will join your party."
                                                , null
                                                , this.conversation_characacter_success_to_cheat_go
                                                , 100, null);


            starter.AddDialogLine("hero_courtship_persuasion_attempt", "heroPersuasionQuestion", "player_courtship_argument", "{=!}{PERSUASION_TASK_LINE}"
                            , new ConversationSentence.OnConditionDelegate(this.persuasion_conversation_dialog_line)
                            , null
                            , 100, null);

            starter.AddPlayerLine("player_courtship_argument_0", "player_courtship_argument", "hero_courtship_reaction_forcheat", "{=!}{ROMANCE_PERSUADE_ATTEMPT_0}"
                            , delegate { return persuasion_conversation_player_line(0); }
                            , delegate { persuasion_conversation_player_line_clique(0); }
                            , 100
                            , /*ConversationSentence.OnClickableConditionDelegate*/ delegate (out TextObject explanation) { return persuasion_conversation_player_clickable(0, out explanation); }
                            , /* ConversationSentence.OnPersuasionOptionDelegate */ delegate { return persuasion_conversation_player_get_optionArgs(0); } );

            starter.AddPlayerLine("player_courtship_argument_1", "player_courtship_argument", "hero_courtship_reaction_forcheat", "{=!}{ROMANCE_PERSUADE_ATTEMPT_1}"
                            , delegate { return persuasion_conversation_player_line(1); }
                            , delegate { persuasion_conversation_player_line_clique(1); }
                            , 100
                            , delegate (out TextObject explanation) { return persuasion_conversation_player_clickable(1, out explanation); }
                            , delegate { return persuasion_conversation_player_get_optionArgs(1); });

            starter.AddPlayerLine("player_courtship_argument_2", "player_courtship_argument", "hero_courtship_reaction_forcheat", "{=!}{ROMANCE_PERSUADE_ATTEMPT_2}"
                            , delegate { return persuasion_conversation_player_line(2); }
                            , delegate { persuasion_conversation_player_line_clique(2); }
                            , 100
                            , delegate (out TextObject explanation) { return persuasion_conversation_player_clickable(2, out explanation); }
                            , delegate { return persuasion_conversation_player_get_optionArgs(2); });

            starter.AddPlayerLine("player_courtship_argument_3", "player_courtship_argument", "hero_courtship_reaction_forcheat", "{=!}{ROMANCE_PERSUADE_ATTEMPT_3}"
                            , delegate { return persuasion_conversation_player_line(3); }
                            , delegate { persuasion_conversation_player_line_clique(3); }
                            , 100
                            , delegate (out TextObject explanation) { return persuasion_conversation_player_clickable(3, out explanation); }
                            , delegate { return persuasion_conversation_player_get_optionArgs(3); });

            starter.AddPlayerLine("lord_ask_recruit_argument_no_answer", "player_courtship_argument", "lord_pretalk", "{=!}{TRY_HARDER_LINE}"
                            , persuasion_conversation_player_line_tryLater
                            , persuation_abandon_courtship
                            , 100, null, null);

            starter.AddDialogLine("lord_ask_recruit_argument_reaction", "hero_courtship_reaction_forcheat", "heroPersuasionNextQuestion", "{=!}{PERSUASION_REACTION}"
                            , persuasion_go_next
                            , Persuasion_go_next_clique
                            , 100, null);


            // From previous iteration
            //starter.AddDialogLine("persuasion_leave_faction_npc_result_success_2", "lord_conclude_courtship_stage_2", "close_window", "{=k7nGxksk}Splendid! Let us conduct the ceremony, then.", new ConversationSentence.OnConditionDelegate(conversation_finalize_courtship_for_hero_on_condition), new ConversationSentence.OnConsequenceDelegate(conversation_courtship_success_on_consequence), 140, null);
            starter.AddDialogLine("hero_courtship_persuasion_2_success", "lord_start_courtship_response_3", "lord_conclude_courtship_stage_2", "{=xwS10c1b}Yes... I think I would be honored to accept your proposal."
                            , new ConversationSentence.OnConditionDelegate(conversation_finalize_courtship_for_hero_on_condition)
                            , null
                            , 120, null);

            //starter.AddPlayerLine("hero_romance_task", "hero_main_options", "lord_start_courtship_response_3", "{=cKtJBdPD}I wish to offer my hand in marriage.", new ConversationSentence.OnConditionDelegate(conversation_finalize_courtship_for_hero_on_condition), null, 140, null, null);
            //starter.AddPlayerLine("hero_romance_conclusion_direct", "hero_main_options", "hero_courtship_final_barter_conclusion", "{=2aW6NC3Q}Let us discuss the final terms of our marriage.", new ConversationSentence.OnConditionDelegate(this.conversation_finalize_courtship_for_hero_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_courtship_success_on_consequence), 90, null, null);
            starter.AddPlayerLine("hero_romance_conclusion_direct", "hero_main_options", "close_window", "{=2aW6NC3Q}Let us discuss the final terms of our marriage."
                                                , new ConversationSentence.OnConditionDelegate(this.conversation_finalize_courtship_for_hero_on_condition)
                                                , new ConversationSentence.OnConsequenceDelegate(this.conversation_courtship_success_on_consequence), 90, null, null);

            //starter.AddPlayerLine("hero_romance_task_pt3a", "hero_main_options", "hero_courtship_final_barter", "{=2aW6NC3Q}Let us discuss the final terms of our marriage.", new ConversationSentence.OnConditionDelegate(this.conversation_finalize_courtship_for_hero_on_condition), null, 100, null, null);
            starter.AddPlayerLine("hero_romance_task_pt3b", "hero_main_options", "hero_courtship_final_barter", "{=jd4qUGEA}I wish to discuss the final terms of my marriage with {COURTSHIP_PARTNER}."
                                                , new ConversationSentence.OnConditionDelegate(this.conversation_finalize_courtship_for_other_on_condition)
                                                , null
                                                , 100, null, null);
         
            starter.AddDialogLine("persuasion_leave_faction_npc_result_success_2", "lord_conclude_courtship_stage_2", "close_window", "{=k7nGxksk}Splendid! Let us conduct the ceremony, then."
                                                , new ConversationSentence.OnConditionDelegate(conversation_finalize_courtship_for_hero_on_condition)
                                                , new ConversationSentence.OnConsequenceDelegate(this.conversation_courtship_success_on_consequence), 140, null);

            starter.AddPlayerLine("hero_want_to_marry", "hero_main_options", "lord_pretalk", "{=endcourthip}Sorry {INTERLOCUTOR.NAME}, I don't want to marry you anymore."
                                                , conversation_player_end_courtship
                                                , conversation_player_end_courtship_do
                                                , 100, null, null);

            //starter.AddDialogLine("hero_courtship_final_barter_setup", "hero_courtship_final_barter_conclusion", "close_window", "{=k7nGxksk}Splendid! Let us conduct the ceremony, then.", new ConversationSentence.OnConditionDelegate(this.conversation_marriage_barter_successful_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_courtship_success_on_consequence), 100, null);
            //starter.AddDialogLine("hero_courtship_final_barter_setup", "hero_courtship_final_barter_conclusion", "close_window", "{=iunPaMFv}I guess we should put this aside, for now. But perhaps we can speak again at a later date.", () => !this.conversation_marriage_barter_successful_on_condition(), null, 100, null);
        }

        private bool conversation_courtship_initial_reaction_on_condition()
        {
            MBTextManager.SetTextVariable("INITIAL_COURTSHIP_REACTION", "{=KdhnBhZ1}Yes, we are considering offers. These things are not rushed into.", false);
            return true;
        }

        private void conversaion_do_LeaveCheat()
        {
            float relationBetweenPlayer = Hero.OneToOneConversationHero.GetRelationWithPlayer();
            int mulitpe = (int)(relationBetweenPlayer / 40);
            if (relationBetweenPlayer > 0 && mulitpe <= 0) mulitpe = 1;

            ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, (int)-(mulitpe), false, true);
            MobileParty.MainParty.Party.MemberRoster.RemoveTroop(Hero.OneToOneConversationHero.CharacterObject, 1);
            PartnerRemove(Hero.OneToOneConversationHero);
        }

        private bool conversation_can_LeaveCheat()
        {
            if (PartnerOfPlayer(Hero.OneToOneConversationHero))
            {
                StringHelpers.SetCharacterProperties("INTERLOCUTOR", Hero.OneToOneConversationHero.CharacterObject);
                return true;
            }
            return false;
        }

        private void conversation_do_divorce(bool forBug)
        {
            float relationBetweenPlayer = Hero.OneToOneConversationHero.GetRelationWithPlayer();
            int multiple = (int)(relationBetweenPlayer / 40);
            if (relationBetweenPlayer > 0 && multiple <= 0) multiple = 1;

            Clan? clanToJoin = null;
            if (Hero.OneToOneConversationHero.Father != null
                && Hero.OneToOneConversationHero.Father.Clan != null
                && !Hero.OneToOneConversationHero.Father.Clan.IsEliminated)
                clanToJoin = Hero.OneToOneConversationHero.Father.Clan;

            if (clanToJoin == null
                && Hero.OneToOneConversationHero.Mother != null
                && Hero.OneToOneConversationHero.Mother.Clan != null
                && !Hero.OneToOneConversationHero.Mother.Clan.IsEliminated)
                clanToJoin = Hero.OneToOneConversationHero.Mother.Clan;

            if (forBug)
            {
                Helper.RemoveExSpouses(Hero.MainHero, false, null, false, Hero.OneToOneConversationHero);
                Helper.RemoveExSpouses(Hero.OneToOneConversationHero, false, null, false, Hero.MainHero);
            }
            else
            {
                Hero.OneToOneConversationHero.Spouse = null;
                Helper.RemoveExSpouses(Hero.MainHero, false);
                NoMoreSpouse.Add(Hero.OneToOneConversationHero);
            }

            if (clanToJoin != null)
            {
                Helper.SwapClan(Hero.OneToOneConversationHero, Clan.PlayerClan, clanToJoin);
            }
            else
            {
                Helper.RemoveFromClan(Hero.OneToOneConversationHero, Clan.PlayerClan, false);
                Helper.OccupationToCompanion(Hero.OneToOneConversationHero.CharacterObject);
            }
            PartyHelper.SwapPartyBelongedTo(hero: Hero.OneToOneConversationHero, null);
            MobileParty.MainParty.Party.MemberRoster.RemoveTroop(Hero.OneToOneConversationHero.CharacterObject, 1);


            ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, (int) -(relationBetweenPlayer / 5), false, true);

            if (multiple > 0) {
                foreach (Hero spouse in Spouses)
                {
                    float rela1 = spouse.GetRelation(Hero.OneToOneConversationHero);
                    float rela2 = spouse.GetRelationWithPlayer();
#if TRACEROMANCE
                    Helper.Print(String.Format("Hero Relations between {0} and {1} ?= {2} ", spouse.Name, Hero.OneToOneConversationHero.Name, rela1), Helper.PRINT_TRACE_ROMANCE);
                    Helper.Print(String.Format("Hero Relations between {0} and {1} ?= {2} ", spouse.Name, Hero.MainHero.Name, rela2), Helper.PRINT_TRACE_ROMANCE);
#endif
                    int multiple1 = (int) (rela1 / 20);
                    if (multiple1 < 1) multiple1 = 1;
                    int multiple2 = (int) (rela2 / 30);
                    if (multiple2 < 1) multiple2 = 1;
                    ChangeRelationAction.ApplyPlayerRelation(spouse, (int) -(multiple * multiple1 * multiple2), false, true);
                }
            }

        }

        private bool conversation_can_divorce()
        {

            if (this.SpouseOfPlayer(Hero.OneToOneConversationHero))
            {
                GameTexts.SetVariable("RELATION_TEXT", CampaignUIHelper.GetHeroRelationToHeroTextShort(Hero.MainHero, Hero.OneToOneConversationHero));
                StringHelpers.SetCharacterProperties("INTERLOCUTOR", Hero.OneToOneConversationHero.CharacterObject);
                return true;
            }
            return false;
        }

        private void conversation_player_end_courtship_do()
        {
            if (Hero.OneToOneConversationHero != null)
            {
                Helpers.Util.CleanRomance(Hero.MainHero, Hero.OneToOneConversationHero);
                ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.Untested);
                ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, -10, true, true);
            }
        }

        private bool conversation_player_end_courtship()
        {

            if (Hero.OneToOneConversationHero != null
                && Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage)
            {
                StringHelpers.SetCharacterProperties("INTERLOCUTOR", Hero.OneToOneConversationHero.CharacterObject, null, false);
                return true;
            }
            return false;
        }

        private bool conversation_begin_courtship_for_hero_on_condition()
        {
            if (Hero.OneToOneConversationHero != null)
            {

                bool ret = MarryAnyone.Patches.Behaviors.RomanceCampaignBehaviorPatch.conversation_player_can_open_courtship_on_condition(true);
#if TRACEROMANCE
                Helper.Print(String.Format("MARomanceCampaignBehavior:: avec {0} va répondre {1}"
                                , Helper.TraceHero(Hero.OneToOneConversationHero)
                                , ret.ToString()), Helper.PRINT_TRACE_ROMANCE);
#endif

                if (ret) {
                    ret = Helper.MarryEnabledPathMA(Hero.OneToOneConversationHero, Hero.MainHero, false);
                        // || MAHelper.CheatEnabled(Hero.OneToOneConversationHero, Hero.MainHero);
                }

                // OnNeNousDitPasTout/GrandesMaree Patch
                // In Fact we can't go through Romance.RomanceLevelEnum.Untested
                // because for the next modification, there will be another romance status
                // And we must have only have one romance status for each relation
                if (Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero) == Romance.RomanceLevelEnum.Untested)
                {

#if OLDBUG
                    Helpers.Util.CleanRomance(Hero.MainHero, Hero.OneToOneConversationHero);
#endif
                    bool areMarried = MARomanceCampaignBehavior.Instance.SpouseOrNot(Hero.MainHero, Hero.OneToOneConversationHero);
                    if (areMarried)
                    {
#if !OLDBUG
#if PATCHROMANCE
                        Helpers.Util.CleanRomance(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.Marriage);
#else

                        Helpers.Util.CleanRomance(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.Eneded);
#endif
#endif
                        Helper.Print("PATCH Married New Romantic Level: " + Romance.GetRomanticLevel(Hero.MainHero, Hero.OneToOneConversationHero).ToString(), Helper.PRINT_PATCH);
                    }
#if !OLDBUG
                    else
                        Helpers.Util.CleanRomance(Hero.MainHero, Hero.OneToOneConversationHero);
#endif
                }
#if TRACEROMANCE
                Helper.Print(String.Format("conversation_begin_courtship_for_hero_on_condition with {0} va répondre {1}"
                        , Hero.OneToOneConversationHero.Name
                        , ret.ToString()), Helper.PRINT_TRACE_ROMANCE | Helper.PrintHow.PrintToLogAndWrite);
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

        //                    MAHelper.Print(String.Format("conversation_begin_courtship_for_hero_on_conditionFromMain(V2) with {0} va répondre {1}", Hero.OneToOneConversationHero.Name, ret.ToString()), MAHelper.PRINT_TRACE_ROMANCE);
        //                    return true;
        //                }
        //            }
        //            return false;

        //        }
        //#endif

        private bool conversation_characacter_notagreed_to_cheat_VariantTest()
        {
            bool ret = false;
            if (Hero.OneToOneConversationHero.Occupation == Occupation.Lord)
            {
                if (Helper.FactionAtWar(Hero.MainHero, Hero.OneToOneConversationHero)) {
#if TRACECHEAT
                    Helper.Print("conversation_characacter_notagreed_to_cheat_VariantTest:: Condition OK because at war", Helper.PRINT_TRACE_CHEAT);
#endif

                    MBTextManager.SetTextVariable("CHEAT_DECLINE_REACTION", "{=maCheatNotPossibleKingdomAtWar}I am terribly sorry. Our kingdom are actualy at war.", false);
                    ret = true;
                }
                else if ((Hero.OneToOneConversationHero.PartyBelongedTo != null && Hero.OneToOneConversationHero.PartyBelongedTo.LeaderHero == Hero.OneToOneConversationHero) 
                        && (Hero.MainHero.CurrentSettlement == null || !(Hero.MainHero.CurrentSettlement.IsTown || Hero.MainHero.CurrentSettlement.IsCastle)))
                {
#if TRACECHEAT
                    Helper.Print("conversation_characacter_notagreed_to_cheat_VariantTest:: Condition OK because not in town or castle", Helper.PRINT_TRACE_CHEAT);
#endif
                    MBTextManager.SetTextVariable("CHEAT_DECLINE_REACTION", "{=maCheatNotPossibleCantCancelParty}You are a funny {?PLAYER.GENDER}woman my lady{?}man my lord{\\?}. I Can't cancel my party in country like that.", false);
                    ret = true;
                }

            }
            return ret;
        }

        private bool conversation_characacter_agreed_to_cheat()
        {
            bool ret = false;
            if (Hero.OneToOneConversationHero != null)
            {
                if (!PartnerOfPlayer(Hero.OneToOneConversationHero))
                    ret = MarryAnyone.Patches.Behaviors.RomanceCampaignBehaviorPatch.conversation_player_can_open_courtship_on_condition(true);
                        // && (conversation_characacter_agreed_to_cheat_VariantTest()));
                        ////|| MAHelper.CheatEnabled(Hero.OneToOneConversationHero, Hero.MainHero);
            }
            return ret;
        }

#region persuasion Cheat

        private Tuple<TraitObject, int>[] GetTraitCorrelations(int valor = 0, int mercy = 0, int honor = 0, int generosity = 0, int calculating = 0)
        {
            return new Tuple<TraitObject, int>[]
            {
                new Tuple<TraitObject, int>(DefaultTraits.Valor, valor),
                new Tuple<TraitObject, int>(DefaultTraits.Mercy, mercy),
                new Tuple<TraitObject, int>(DefaultTraits.Honor, honor),
                new Tuple<TraitObject, int>(DefaultTraits.Generosity, generosity),
                new Tuple<TraitObject, int>(DefaultTraits.Calculating, calculating)
            };
        }

        private List<PersuasionTask> GetPersuasionTasksForCheat()
        {
            StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, null, false);
            StringHelpers.SetCharacterProperties("INTERLOCUTOR", Hero.OneToOneConversationHero.CharacterObject, null, false);


            List<PersuasionTask> list = new List<PersuasionTask>();

            //RomanceCampaignBehavior romanceBehavior = MASubModule.Instance.GameStarter().CampaignBehaviors.OfType<RomanceCampaignBehavior>().FirstOrDefault();
            //IEnumerable<RomanceCampaignBehavior.RomanceReservationDescription> romanceReservations = this.GetRomanceReservations(wooed, wooer);

            PersuasionTask persuasionTask = new PersuasionTask(0);
            list.Add(persuasionTask);
            persuasionTask.FinalFailLine = new TextObject("{=cheatTestFail}I am not interested.", null);
            persuasionTask.TryLaterLine = new TextObject("{=cheatTestRetry}Well, I think you may want to try some other time.", null);
            persuasionTask.SpokenLine = new TextObject("{=cheatFirstToken}What do you have in mind ?", null);

            Tuple<TraitObject, int>[] traitCorrelations = this.GetTraitCorrelations(1, -1, 0, 1, -1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations);

            PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Valor, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits, false
                                                                    , new TextObject("{=CheatLeaderChoice}Don't worry, just follow me {?INTERLOCUTOR.GENDER}Miss{?}Mister{\\?}.", null)
                                                                    , traitCorrelations, false, true, false);
            persuasionTask.AddOptionToTask(option);

            Tuple<TraitObject, int>[] traitCorrelations2 = this.GetTraitCorrelations(1, 0, 0, -1, 1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits2 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations2);
            PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits2, false
                                                                    , new TextObject("{=CheatCalculateChoice}If we are both thinking the same thing, then let's go", null)
                                                                    , traitCorrelations2, false, true, false);

            persuasionTask.AddOptionToTask(option2);
            Tuple<TraitObject, int>[] traitCorrelations3 = this.GetTraitCorrelations(0, 1, 1, 0, -1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits3 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations3);
            PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits3, false
                                                    , new TextObject("{=CheatMercyChoice}I cannot let {?INTERLOCUTOR.GENDER}a beautiful woman{?}a handsome young man{\\?} get hurt in this dangerous world, I'll protect you.", null)
                                                    , traitCorrelations3, false, true, false);
            persuasionTask.AddOptionToTask(option3);
            Tuple<TraitObject, int>[] traitCorrelations4 = this.GetTraitCorrelations(-1, 0, -1, -1, 0);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits4 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations4);
            PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Negative, argumentStrengthBasedOnTargetTraits4, false
                                                    , new TextObject("{=CheatGenerosityCharm}It is a beautiful day outside, and {?INTERLOCUTOR.GENDER}an adventurous woman{?}an adventurous man{\\?} like you, needs to enjoy the fresh air.", null)
                                                    , traitCorrelations4, false, true, false);
            persuasionTask.AddOptionToTask(option4);

            return list;
        }

        private void conversation_characacter_test_to_cheat()
        {
            this._allReservations = this.GetPersuasionTasksForCheat();
            this._maximumScoreCap = (float)this._allReservations.Count<PersuasionTask>() * 1f;
            float num = 0f;

#if TRACECHEAT
            Helper.Print(String.Format("conversation_characacter_test_to_cheat:: Launch Cheat Persuasion\r\n\t MaxScore ?= {0}\r\n\t Success ?= {1},\r\n\t fail ?= {2}"
                                            , this._maximumScoreCap
                                            , this._successValue
                                            , this._failValue) , Helper.PRINT_TRACE_ROMANCE);
#endif
            ConversationManager.StartPersuasion(this._maximumScoreCap, this._successValue, this._failValue, this._criticalSuccessValue, this._criticalFailValue, num
                                                , Helper.MASettings.DifficultyNormalMode ? PersuasionDifficulty.Hard : PersuasionDifficulty.Medium);
        }

        private void conversation_characacter_fail_to_cheat_go()
        {
            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.LeaveEncounter = true;
            }
            this._allReservations = null;
            ConversationManager.EndPersuasion();
        }

        private void conversation_characacter_success_to_cheat_go()
        {

#if TRACECHEAT
            Helper.Print("conversation_characacter_success_to_cheat_go:: SUCCESS", Helper.PRINT_TRACE_CHEAT);
#endif

            float scoreFromPersuasion = ConversationManager.GetPersuasionProgress() - ConversationManager.GetPersuasionGoalValue();

            if (Partners == null)
            {
                Partners = new List<Hero>();
                Partners.Add(Hero.OneToOneConversationHero);
            }

            if (Hero.OneToOneConversationHero.PartyBelongedTo != MobileParty.MainParty)
                AddHeroToPartyAction.Apply(Hero.OneToOneConversationHero, MobileParty.MainParty, true);

            this._allReservations = null;
            ConversationManager.EndPersuasion();

#if TRACKTOMUCHSPOUSE
            MARomanceCampaignBehavior.VerifySpouse(0, "conversation_characacter_success_to_cheat_go");
#endif
        }

        private void persuasionAttemptCheatClean(Hero forHero)
        {
            if (_previousCheatPersuasionAttempts != null)
            {
                PersuasionAttempt? persuasionAttempt = null;
                while ((persuasionAttempt = _previousCheatPersuasionAttempts.FirstOrDefault(x => x.PersuadedHero == forHero)) != null)
                    _previousCheatPersuasionAttempts.Remove(persuasionAttempt);
            }
        }

        private bool conversation_cheat_allready_done()
        {
            Hero forHero = Hero.OneToOneConversationHero;

            if (_previousCheatPersuasionAttempts != null)
            {
                PersuasionAttempt persuasionAttempt = _previousCheatPersuasionAttempts
                        .FirstOrDefault(x => x.PersuadedHero == forHero
                                                && ((x.Result != PersuasionOptionResult.CriticalFailure 
                                                        && x.GameTime.ElapsedDaysUntilNow < 1f)
                                                    || (x.Result == PersuasionOptionResult.CriticalFailure
                                                        && x.GameTime.ElapsedWeeksUntilNow < 2f )));

                bool ret = persuasionAttempt != null;
                if (!ret)
                    persuasionAttemptCheatClean(forHero);

                return ret;
            }
            return false;
        }

        private bool conversation_cheat_easy_mode()
        {
            return Helper.MASettings.DifficultyVeryEasyMode;
        }

#endregion

#region persuasion system

        private PersuasionTask GetCurrentPersuasionTask()
        {
            foreach (PersuasionTask persuasionTask in this._allReservations)
            {
                if (!persuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
                {
                    return persuasionTask;
                }
            }
            return this._allReservations.Last<PersuasionTask>();
        }

        private PersuasionTask? FindTaskOfOption(PersuasionOptionArgs optionChosenWithLine)
        {
            foreach (PersuasionTask persuasionTask in this._allReservations)
            {
                using (List<PersuasionOptionArgs>.Enumerator enumerator2 = persuasionTask.Options.GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        if (enumerator2.Current.Line == optionChosenWithLine.Line)
                        {
                            return persuasionTask;
                        }
                    }
                }
            }
            return null;
        }

        private bool persuasion_conversation_dialog_line()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask == this._allReservations.Last<PersuasionTask>())
            {
                if (currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
                {
                    return false;
                }
            }
            if (!ConversationManager.GetPersuasionProgressSatisfied())
            {
                MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", currentPersuasionTask.SpokenLine, false);
                return true;
            }
            return false;
        }

        private bool persuasion_conversation_player_line(int noOption)
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count<PersuasionOptionArgs>() > noOption)
            {
                TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
                textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(noOption), true));
                textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(noOption).Line);
                MBTextManager.SetTextVariable("ROMANCE_PERSUADE_ATTEMPT_" + noOption, textObject, false);
                return true;
            }
            return false;
        }

        private bool persuasion_conversation_player_clickable(int noOption, out TextObject hintText)
        {
            hintText = TextObject.Empty;
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Any<PersuasionOptionArgs>())
            {
                return !currentPersuasionTask.Options.ElementAt(noOption).IsBlocked;
            }
            hintText = new TextObject("{=9ACJsI6S}Blocked", null);
            return false;
        }

        private PersuasionOptionArgs persuasion_conversation_player_get_optionArgs(int noOption)
        {
            return this.GetCurrentPersuasionTask().Options.ElementAt(noOption);
        }

        private void persuasion_conversation_player_line_clique(int noOption)
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > noOption)
            {
                currentPersuasionTask.Options[noOption].BlockTheOption(true);
            }
        }

        private bool persuasion_conversation_player_line_tryLater()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            MBTextManager.SetTextVariable("TRY_HARDER_LINE", currentPersuasionTask.TryLaterLine, false);
            return true;
        }

        private void persuation_abandon_courtship()
        {
            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.LeaveEncounter = true;
            }
            this._allReservations = null;
            ConversationManager.EndPersuasion();
        }

        private bool persuasion_go_nextStep()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
            {
#if TRACEROMANCE
                Helper.Print("persuasion_go_nextStep:: Persuasion Blocked return false");
#endif

                MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", currentPersuasionTask.FinalFailLine, false);
                return false;
            }
            if (conversation_cheat_easy_mode())
            {
#if TRACEROMANCE
                Helper.Print("persuasion_go_nextStep:: Very easy mode return false");
#endif
                return false;
            }

            if (!ConversationManager.GetPersuasionProgressSatisfied())
            {
#if TRACEROMANCE
                Helper.Print("persuasion_go_nextStep:: OK");
#endif
                MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", currentPersuasionTask.SpokenLine, false);
                return true;
            }
#if TRACEROMANCE
            Helper.Print("persuasion_go_nextStep:: return false");
#endif
            return false;
        }


        private bool persuasion_go_next()
        {
            PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>().Item2;
            if ((item == PersuasionOptionResult.Failure || item == PersuasionOptionResult.CriticalFailure) && this.GetCurrentPersuasionTask().ImmediateFailLine != null)
            {
                MBTextManager.SetTextVariable("PERSUASION_REACTION", this.GetCurrentPersuasionTask().ImmediateFailLine, false);
                if (item != PersuasionOptionResult.CriticalFailure)
                {
                    return true;
                }
                using (List<PersuasionTask>.Enumerator enumerator = this._allReservations.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        PersuasionTask persuasionTask = enumerator.Current;
                        persuasionTask.BlockAllOptions();
                    }
                    return true;
                }
            }
            MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
            return true;
        }

        private void Persuasion_go_next_clique()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
            float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Medium);
            float moveToNextStageChance;
            float blockRandomOptionChance;
            Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out moveToNextStageChance, out blockRandomOptionChance, difficulty);
            this.FindTaskOfOption(tuple.Item1).ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
            PersuasionAttempt persuasionAttempt = new PersuasionAttempt(Hero.OneToOneConversationHero, CampaignTime.Now, tuple.Item1, tuple.Item2, currentPersuasionTask.ReservationType);
            if (_previousCheatPersuasionAttempts == null)
                _previousCheatPersuasionAttempts = new List<PersuasionAttempt>();

            _previousCheatPersuasionAttempts.Add(persuasionAttempt);
        }

        private bool persuasion_fail()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
            {

                MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", currentPersuasionTask.FinalFailLine, false);
                return true;
            }
            return false;
        }

#endregion

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

            if (!Helper.MASettings.CanJoinUpperClanThroughMAPath
                && !Helpers.HeroInteractionHelper.CanIntegreSpouseInHeroClan(Hero.MainHero, Hero.OneToOneConversationHero))
                return false;

            bool ret = Campaign.Current.Models.RomanceModel.CourtshipPossibleBetweenNPCs(Hero.MainHero, Hero.OneToOneConversationHero)
                        && (Hero.OneToOneConversationHero.Clan == null 
                            || Hero.OneToOneConversationHero.Clan.Leader == Hero.OneToOneConversationHero
                            || Hero.OneToOneConversationHero.Clan == Hero.MainHero.Clan);
            if (ret)
            {
                if ((romanticLevel == Romance.RomanceLevelEnum.CourtshipStarted
                       || romanticLevel == Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible) 
                        && Helper.MASettings.Difficulty == MASettings.DIFFICULTY_VERY_EASY)
                {
                    ChangeRomanticStateAction.Apply(Hero.MainHero, Hero.OneToOneConversationHero, Romance.RomanceLevelEnum.CoupleAgreedOnMarriage);
                    romanticLevel = Romance.RomanceLevelEnum.CoupleAgreedOnMarriage;
                }
                ret = (romanticLevel == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage);
            }

//            if (ret && Hero.OneToOneConversationHero.Clan != null && Hero.OneToOneConversationHero.Clan.Leader != Hero.OneToOneConversationHero)
//            {
//#if TRACEROMANCE
//                MAHelper.Print(string.Format("MARomanceCampaignBehavior:: conversation_finalize_courtship_for_hero_on_condition with {0} goto defaut Programme", Hero.OneToOneConversationHero.Name), MAHelper.PRINT_TRACE_ROMANCE);
//#endif
//                ret = false;
//            }

            if (ret && romanticState != null && romanticState.ScoreFromPersuasion == 0)
                romanticState.ScoreFromPersuasion = 60;

#if TRACEROMANCE
            Helper.Print(string.Format("MARomanceCampaignBehavior:: conversation_finalize_courtship_for_hero_on_condition with {0} \r\n\tDifficulty ?= {1} RommanticLevel ?= {2}\r\n\trépond {3} \r\n\tromanticState Score ?= {4}"
                    , Hero.OneToOneConversationHero.Name.ToString()
                    , Helper.MASettings.Difficulty
                    , romanticLevel.ToString()
                    , ret
                    , (romanticState != null ? romanticState.ScoreFromPersuasion.ToString() : "NULL")), Helper.PRINT_TRACE_ROMANCE);
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
#if TRACEROMANCE
                            Helper.Print("MARomanceCampaignBehavior:: conversation_finalize_courtship_for_other_on_condition => TEST SUCCESS", Helper.PRINT_TRACE_ROMANCE);
#endif
                            return true;
                        }
                    }
                }
            }
#if TRACEROMANCE
            Helper.Print("MARomanceCampaignBehavior:: conversation_finalize_courtship_for_other_on_condition => FAIL", Helper.PRINT_TRACE_ROMANCE);
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
            Clan? spouseLeaveClan = null;
            Clan? heroLeaveClan = null;

            try
            {
                _MAWedding = true;

                PartnerRemove(Hero.OneToOneConversationHero);

#if TRACEWEDDING
                Helper.Print("MARomanceCampaignBehavior:: conversation_courtship_success_on_consequence", Helper.PRINT_TRACE_WEDDING);
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
                            if (!Helper.MASettings.CanJoinUpperClanThroughMAPath)
                                throw new Exception("conversation_courtship_success_on_consequence TU spouse IS MAIN FAIL");

                            bool canDestroyClan = false;
                            heroLeaveClan = hero.Clan;

                            MobileParty? mobilePartyDest = null;
                            if (spouse.CurrentSettlement == hero.CurrentSettlement
                                || (hero.PartyBelongedTo == MobileParty.MainParty && spouse.PartyBelongedTo != null && spouse.PartyBelongedTo == MobileParty.ConversationParty))

                                mobilePartyDest = spouse.PartyBelongedTo;

                            // Join kingdom due to lowborn status
                            if (hero.Clan.Leader == hero)
                                canDestroyClan = true;

                            Action<Hero> swapPartie = (Hero h) =>
                            {
                                bool inParty = false;
                                if (h.PartyBelongedTo == MobileParty.MainParty)
                                {
                                    inParty = true;
                                }
                                RemoveCompanionAction.ApplyByFire(heroLeaveClan, h);
                                AddCompanionAction.Apply(spouse.Clan, h);
                                if (inParty)
                                {
                                    if (mobilePartyDest != null)
                                    {
                                        AddHeroToPartyAction.Apply(h, mobilePartyDest, true);
                                    }
                                    else if (MobileParty.MainParty.MemberRoster.FindIndexOfTroop(h.CharacterObject) < 0)
                                        AddHeroToPartyAction.Apply(h, MobileParty.MainParty, false);
                                }
                            };

                            foreach (Hero companion in hero.Clan.Companions.ToList())
                            {
                                swapPartie(companion);
                            }
                            if (hero == Hero.MainHero && Helper.MASettings.Polygamy)
                            {
                                foreach (Hero exSpouse in hero.ExSpouses)
                                {
                                    if (SpouseOfPlayer(exSpouse))
                                    {
                                        swapPartie(exSpouse);
                                    }
                                }
                            }

                            Helper.SwapClan(hero, heroLeaveClan, spouse.Clan);

                            if (mobilePartyDest != null)
                            {
                                MobileParty oldParty = MobileParty.MainParty;

                                if (oldParty != null)
                                {
                                    if (oldParty.PrisonRoster?.Count > 0)
                                    {
                                        mobilePartyDest.Party.AddPrisoners(oldParty.PrisonRoster);
                                        oldParty.PrisonRoster.Clear();
                                    }

                                    if (oldParty.ItemRoster?.Count > 0)
                                    {
                                        mobilePartyDest.ItemRoster.Add(oldParty.ItemRoster);
                                        oldParty.ItemRoster.Clear();
                                    }
                                }
                                AddHeroToPartyAction.Apply(hero, mobilePartyDest, true);
                                PartyHelper.SwapMainParty(mobilePartyDest);

                                PartyHelper.SwapPartyBelongedTo(hero, mobilePartyDest);
                                mobilePartyDest.ChangePartyLeader(Hero.MainHero);
                                mobilePartyDest.Party.SetCustomOwner(Hero.MainHero);

                                PartyHelper.SetLeaderAtTop(mobilePartyDest.Party);

                                if (oldParty != null)
                                {
                                    MergePartiesAction.Apply(mobilePartyDest.Party, oldParty.Party);
                                }

                                //DestroyPartyAction.Apply(null, oldParty);
#if TRACEWEDDING
                                Helper.Print("Lowborn Player Married to Kingdom Ruler and swap of party", Helper.PRINT_TRACE_WEDDING);
#endif
                            }

                            Helper.FamilyAdoptChild(spouse, hero, heroLeaveClan);
                            Helper.FamilyJoinClan(hero, heroLeaveClan, spouse.Clan);

                            if (canDestroyClan && HeroLeaveClanLeaderAndDestroyClan(heroLeaveClan, spouse.Clan))
                            {
                                heroLeaveClan = null;
                            }

#if V1700LESS
                            Helper.SwapClan(hero, heroLeaveClan, spouse.Clan); // one again
#endif
                            Campaign current = Traverse.Create<Campaign>().Property("Current").GetValue<Campaign>();
                            Traverse.Create(current).Property("PlayerDefaultFaction").SetValue(spouse.Clan);
#if TRACEWEDDING
                            Helper.Print("Lowborn Player Married to Kingdom Ruler and swap of faction", Helper.PRINT_TRACE_WEDDING);
                            if (hero.Clan == null)
                                Helper.Print("Hero.Clan == NULL => FAIL", Helper.PRINT_TRACE_WEDDING);
                            else if (hero.Clan.Lords == null)
                                Helper.Print("Hero.Clan.Lords == NULL => FAIL", Helper.PRINT_TRACE_WEDDING);
#endif
                            ChangeClanLeaderAction.ApplyWithSelectedNewLeader(spouse.Clan, Hero.MainHero);
                        }
                        else
                        {
                            spouseLeaveClan = spouse.Clan;
                            ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(spouse.Clan);
#if TRACEWEDDING
                            Helper.Print("Kingdom Ruler Stepped Down and Married to Player", Helper.PRINT_TRACE_WEDDING);
#endif
                        }
                    }
                }
                else if (spouse.IsFactionLeader && spouse.IsMinorFactionHero)
                {
                    spouseLeaveClan = spouse.Clan;
                    ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(spouse.Clan);
#if TRACEWEDDING
                    Helper.Print("MinorFaction Ruler Stepped Down and Married to Player", Helper.PRINT_TRACE_WEDDING);
#endif
                }

                // Adoption
                if (spouseLeaveClan != null)
                {
                    Helper.FamilyAdoptChild(spouse, hero, spouseLeaveClan);
                }

                if (spouse.Clan == null) // Patch V2
                {
                    if (spouseLeaveClan != null)
                        Helper.FamilyJoinClan(spouse, spouseLeaveClan, hero.Clan);
                    else
                        Helper.SwapClan(spouse, spouse.Clan, hero.Clan);
#if TRACEWEDDING
                    Helper.Print("Spouse Swap clan", Helper.PRINT_TRACE_WEDDING);
#endif
                }

                // New nobility
                Helper.OccupationToLord(spouse.CharacterObject);
                if (!spouse.IsNoble)
                {
                    spouse.IsNoble = true;
#if TRACEWEDDING
                    Helper.Print("Spouse to Noble", Helper.PRINT_TRACE_WEDDING);
#endif
                }

#if V1640MORE
                if (hero.Clan?.Lords.FirstOrDefault(x => x == spouse) == null)
                {
                    hero.Clan.Lords.AddItem(spouse);
                    Helper.Print("Add Spouse to Noble", Helper.PRINT_TRACE_WEDDING);
                }
#endif

                // Dodge the party crash for characters part 1
                bool dodge = false;
                if (spouse.PartyBelongedTo == MobileParty.MainParty)
                {
                    AccessTools.Property(typeof(Hero), "PartyBelongedTo").SetValue(spouse, null, null);
#if TRACEWEDDING
                    Helper.Print("Spouse Already in Player's Party", Helper.PRINT_TRACE_WEDDING);
#endif
                    dodge = true;
                }
                // Apply marriage
                ChangeRomanticStateAction.Apply(hero, spouse, Romance.RomanceLevelEnum.Marriage);
#if TRACEWEDDING
                Helper.Print("Marriage Action Applied", Helper.PRINT_TRACE_WEDDING);
#endif

                if (oldSpouse is not null)
                {
                    Helper.RemoveExSpouses(oldSpouse);
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
#if TRACEWEDDING

                    Helper.Print("Activated Spouse", Helper.PRINT_TRACE_WEDDING);
#endif
                }
                if (spouse.IsPlayerCompanion)
                {
                    spouse.CompanionOf = null;
#if TRACEWEDDING

                    Helper.Print("Spouse No Longer Companion", Helper.PRINT_TRACE_WEDDING);
#endif
                }
                if (/*MAHelper.MASettings.Cheating &&*/ cheatedSpouse is not null)
                {
                    Helper.RemoveExSpouses(cheatedSpouse, true);
#if TRACEWEDDING
                    Helper.Print(String.Format("Cheatedspouse {0} Broke Off Past Marriage", cheatedSpouse.Name), Helper.PRINT_TRACE_WEDDING);
#endif
                }

                Helper.RemoveExSpouses(spouse, true);

#if TRACEWEDDING
                Helper.Print(String.Format("Spouse => {0}", Helper.TraceHero(spouse)), Helper.PRINT_TRACE_WEDDING);
#endif
                Helper.RemoveExSpouses(hero);
                Helper.RemoveExSpouses(spouse);

                if (PlayerEncounter.Current != null)
                    PlayerEncounter.LeaveEncounter = true;

                // New fix to stop some kingdom rulers from disappearing
                if ((spouse.CurrentSettlement != null && spouse.CurrentSettlement == hero.CurrentSettlement) 
                    || (hero.PartyBelongedTo == MobileParty.MainParty && spouse.PartyBelongedTo != null && spouse.PartyBelongedTo == MobileParty.ConversationParty))
                {
#if TRACEWEDDING
                    Helper.Print("Add Spouse do main party", Helper.PRINT_TRACE_WEDDING);
#endif
                    AddHeroToPartyAction.Apply(spouse, MobileParty.MainParty, true);
                }
            }
            finally
            {
                _MAWedding = false;
            }
            //if (PlayerEncounter.Current != null)
            //{
            //    PlayerEncounter.LeaveEncounter = true;
            //}
        }

        // Return true, if destroy the clan
        private bool HeroLeaveClanLeaderAndDestroyClan(Clan fromClan, Clan newClan = null)
        {
            Hero? ancLeader = fromClan.Leader;
            Hero? newLeader = null;
            Hero? heroRAS = null;
            bool supprimeClan = false;

            Dictionary<Hero, int> heirApparents = fromClan.GetHeirApparents(); // ne fonctionne pas car les héros ne sont pas encores listés dans les clans

            if (heirApparents.Count > 0)
            {
                int max = heirApparents.AsEnumerable().Where(x => x.Key != ancLeader).Max(x => x.Value);
                //int max = Max<int>(heirApparents.AsEnumerable().Where(x => x.Key != ancLeader).Select(x => x.Value));
                newLeader = heirApparents.AsEnumerable().FirstOrDefault(x => x.Key != ancLeader && x.Value == max).Key;
            }

            if (newLeader != null)
            {
                ancLeader.Clan = fromClan; // to ApplyWithoutSelectedNewLeader work fine
                ChangeClanLeaderAction.ApplyWithSelectedNewLeader(fromClan, newLeader);
            }
            else
            {
                Helper.RemoveFromClan(ancLeader, fromClan);
                DestroyClanAction.Apply(fromClan);
                supprimeClan = true;
            }
            if (ancLeader != null)
            {
                ancLeader.Clan = newClan;
#if TRACEWEDDING
                Helper.Print(String.Format("Swap Leader for the clan {0} SET The New Clan {1} for AncLeader {2}"
                                            , fromClan.Name
                                            , newClan.Name
                                            , ancLeader.Name), Helper.PRINT_TRACE_WEDDING);

#endif
            }
#if TRACEWEDDING
            if (supprimeClan)
                Helper.Print(String.Format("Swap Leader for the clan {0} ERASE the clan", fromClan.Name), Helper.PRINT_TRACE_WEDDING);
            else if (fromClan.Leader == ancLeader)
                Helper.Print(String.Format("Swap Leader for the clan {0} FAIL because leader unchanged", fromClan.Name), Helper.PRINT_TRACE_WEDDING);
            else
                Helper.Print(String.Format("Swap Leader for the clan {0} SUCCESS swap the leader from {1} to {2}", fromClan.Name, ancLeader.Name, fromClan.Leader == null ? "NULL" : fromClan.Leader.Name), Helper.PRINT_TRACE_WEDDING);
#endif
            return supprimeClan;
        }

#endregion

        private void OnHourTickEvent()
        {
            if (Hero.MainHero.Spouse != null && HeroInteractionHelper.OkToDoIt(Hero.MainHero, Hero.MainHero.Spouse))
                return;

            Hero? otherSpouse = FirstHeroExSpouseOkToDoIt();
            Helper.Print(String.Format("Set Player Spouse {0}", (otherSpouse != null ? otherSpouse.Name : "noone:/")), Helper.PrintHow.PrintToLogAndWriteAndDisplay);
            if (otherSpouse != null)
            {
                Hero.MainHero.Spouse = otherSpouse;
                Helper.RemoveExSpouses(Hero.MainHero);
                Helper.RemoveExSpouses(otherSpouse);
            }
        }

        private void OnHeroesMarried(Hero arg1, Hero arg2, bool arg3)
        {
            if (!_MAWedding && (arg1 == Hero.MainHero || arg2 == Hero.MainHero))
            {
                if (arg1.CurrentSettlement != null && arg1.CurrentSettlement == arg2.CurrentSettlement)
                {
                    if (arg1 == Hero.MainHero && arg2.PartyBelongedTo != MobileParty.MainParty)
                    {
#if TRACEWEDDING
                        Helper.Print(String.Format("OnHeroesMarried:: {0} join your party", arg2.Name), Helper.PRINT_TRACE_WEDDING);
#endif

                        AddHeroToPartyAction.Apply(arg2, MobileParty.MainParty, true);
                    }

                    if (arg2 == Hero.MainHero && arg1.PartyBelongedTo != MobileParty.MainParty)
                    {
#if TRACEWEDDING
                        Helper.Print(String.Format("OnHeroesMarried:: {0} join your party", arg1.Name), Helper.PRINT_TRACE_WEDDING);
#endif
                        AddHeroToPartyAction.Apply(arg1, MobileParty.MainParty, true);
                    }

                }
            }
        }

#region chargements et patch

        private bool SaveVersionOlderThen(String versionChaine)
        {
            if (SaveVersion == null)
                return true;

            Version versionComparaison = new Version(versionChaine);
#if TRACELOAD
            Helper.Print(String.Format("SaveVersionOlderThen SaveVersion ?= {0} < versionComparaison ?= {1} => {2}"
                                    , SaveVersion.ToString()
                                    , versionComparaison.ToString()
                                    , (SaveVersion < versionComparaison).ToString()), Helper.PrintHow.PrintToLogAndWriteAndInit);
#endif
            return SaveVersion < versionComparaison;
        }

        private void patchClanLeader(Clan clan)
        {

            Hero? ancLeader = clan.Leader;
            Hero? newLeader = null;
            Hero? heroRAS = null;
            bool supprimeClan = false;

            Helper.Print(String.Format("Nb Heroes in clan {0} ?= {1}", clan.Name, clan.Heroes.Count), Helper.PRINT_PATCH);
            Helper.Print(String.Format("clan({1}).leader.clan ?= {0}", (clan.Leader != null && clan.Leader.Clan != null ? clan.Leader.Clan.Name : "NULL"), clan.Name), Helper.PRINT_PATCH);

            ancLeader.Clan = clan; // to ApplyWithoutSelectedNewLeader work fine
            Dictionary<Hero, int> heirApparents = clan.GetHeirApparents(); // ne fonctionne pas car les héros ne sont pas encores listés dans les clans
                                                                           //heirApparents = new Dictionary<Hero, int>();
                                                                           //int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
                                                                           //foreach (Hero hero in Hero.AllAliveHeroes)
                                                                           //{
                                                                           //    if (hero.Clan == clan && hero != ancLeader && hero.IsAlive && !hero.IsNotSpawned && !hero.IsDisabled && !hero.IsWanderer && !hero.IsNotable && hero.Age >= (float)heroComesOfAge)
                                                                           //    {
                                                                           //        int value = Campaign.Current.Models.HeirSelectionCalculationModel.CalculateHeirSelectionPoint(hero, ancLeader, ref heroRAS);
                                                                           //        heirApparents.Add(hero, value);
                                                                           //    }
                                                                           //}

            if (heirApparents.Count > 0)
            {
                int max = heirApparents.AsEnumerable().Where(x => x.Key != ancLeader).Max(x => x.Value);
                //int max = Max<int>(heirApparents.AsEnumerable().Where(x => x.Key != ancLeader).Select(x => x.Value));
                newLeader = heirApparents.AsEnumerable().FirstOrDefault(x => x.Key != ancLeader && x.Value == max).Key;
            }

            if (newLeader != null)
            {
                ChangeClanLeaderAction.ApplyWithSelectedNewLeader(clan, newLeader);
                ancLeader.Clan = Hero.MainHero.Clan;
            }
            else
            {

                ancLeader.Clan = Hero.MainHero.Clan;

                Helper.Print(String.Format("AncLeader {0} is alive {1} his clan {2}"
                                , ancLeader.Name
                                , ancLeader.IsAlive.ToString()
                                , (ancLeader.Clan != null ? ancLeader.Clan.Name : "NULL")), Helper.PRINT_PATCH);

                if (ancLeader.IsAlive)
                {
                    Helper.Print("ancLeader TRY to leave the clan", Helper.PRINT_PATCH);
                    Helper.RemoveFromClan(ancLeader, clan);
                }
                DestroyClanAction.Apply(clan);
                supprimeClan = true;
            }

            if (supprimeClan)
                Helper.Print(String.Format("PATCH Leader for the clan {0} ERASE the clan", clan.Name), Helper.PRINT_PATCH);
            else if (clan.Leader == ancLeader)
                Helper.Print(String.Format("PATCH Leader for the clan {0} FAIL because leader unchanged", clan.Name), Helper.PRINT_PATCH);
            else
                Helper.Print(String.Format("PATCH Leader for the clan {0} SUCCESS swap the leader from {1} to {2}", clan.Name, ancLeader.Name, clan.Leader == null ? "NULL" : clan.Leader.Name), Helper.PRINT_PATCH);

        }

        // Return true if patch
        private bool patchParent(Hero children, Hero ?mainFemaleSpouseHero, Hero? mainMaleSpouseHero)
        {
            bool hadSpouse = mainFemaleSpouseHero != null || mainMaleSpouseHero != null;
            bool mainHeroIsFemale = Hero.MainHero.IsFemale;

            if (hadSpouse && children.Father == Hero.MainHero && children.Mother == Hero.MainHero)
            {
                Helper.Print(string.Format("Will Patch Parent of {0}", children.Name), Helper.PRINT_PATCH);
                if (mainHeroIsFemale)
                    children.Father = mainMaleSpouseHero ?? mainFemaleSpouseHero;
                else
                    children.Mother = mainFemaleSpouseHero ?? mainMaleSpouseHero;
                return true;
            }
            if (children.Father == null)
            {
                Helper.Print(string.Format("Will patch Father of {0}", children.Name), Helper.PRINT_PATCH);
                children.Father = mainHeroIsFemale && mainMaleSpouseHero != null ? mainMaleSpouseHero : Hero.MainHero;
                return true;
            }
            if (children.Mother == null)
            {
                Helper.Print(string.Format("Will patch Mother of {0}", children.Name), Helper.PRINT_PATCH);
                children.Mother = !mainHeroIsFemale && mainFemaleSpouseHero != null ? mainFemaleSpouseHero : Hero.MainHero;
                return true;
            }
            return false;
        }

        private void patchSpouses(CampaignGameStarter campaignGameStarter)
        {

            bool needPatch = Helper.MASettings.Patch;
            bool bPatchExecute = false;
            int nbMainHero = 0;

            if (Hero.MainHero.Spouse != null && Hero.MainHero.Spouse.HeroState == Hero.CharacterStates.Disabled)
            {
                Hero.MainHero.Spouse.ChangeState(Hero.CharacterStates.Active);
                Helper.Print(string.Format("Active {0}", Hero.MainHero.Spouse.Name), Helper.PRINT_PATCH);
            }
            foreach (Hero hero in Hero.MainHero.ExSpouses)
            {
                if (hero.HeroState == Hero.CharacterStates.Disabled && hero.IsAlive)
                {
                    hero.ChangeState(Hero.CharacterStates.Active);
                    Helper.Print(string.Format("Active {0}", hero.Name), Helper.PRINT_PATCH);

                }
            }

            List<Hero> spouses = new List<Hero>();
            if (Hero.MainHero.Spouse != null)
            {
                if (Hero.MainHero.Spouse != Hero.MainHero)
                    spouses.Add(Hero.MainHero.Spouse);
#if TRACELOAD
                Helper.Print("Main spouse " + Helper.TraceHero(Hero.MainHero.Spouse), Helper.PRINT_TRACE_LOAD);
#endif
#if !SPOUSEALLWAYSWITHYOU
                Hero.MainHero.Spouse = null;
#endif
            }

            if (Hero.MainHero.ExSpouses != null)
            {
                int nb = Hero.MainHero.ExSpouses.Count;
                //if (Hero.MainHero.Spouse == null)
                //    Hero.MainHero.Spouse = FirstHeroExSpouse();

                Helper.RemoveExSpouses(Hero.MainHero);
                if (Hero.MainHero.Spouse != null)
                    Helper.RemoveExSpouses(Hero.MainHero.Spouse, false, spouses, false); // For Encycolpedie

#if TRACELOAD
                if (nb != Hero.MainHero.ExSpouses.Count)
                    Helper.Print(String.Format("Patch duplicate spouse for mainHero from {0} to {1}", nb, Hero.MainHero.ExSpouses.Count), Helper.PRINT_TRACE_LOAD);
                Helper.Print(String.Format("MainHero {0}", Helper.TraceHero(Hero.MainHero)), Helper.PRINT_TRACE_LOAD);
#endif
                nbMainHero = nb;

                foreach (Hero hero in Hero.MainHero.ExSpouses)
                {
                    if (hero.IsAlive && NoMoreSpouse.IndexOf(hero) < 0 && hero != Hero.MainHero)
                        spouses.Add(hero);
#if TRACELOAD
                    Helper.Print("Other spouse " + Helper.TraceHero(hero), Helper.PRINT_TRACE_LOAD);
#endif
                }
            }

#if PATCHWITHLOGENTRY
            //DefaultLogsCampaignBehavior defaultLogsCampaignBehavior = campaignGameStarter.CampaignBehaviors.OfType<DefaultLogsCampaignBehavior>().FirstOrDefault();
            //if (defaultLogsCampaignBehavior != null)
            //{

            //}
            List<Hero> logSpouses = new List<Hero>();
            if (needPatch || SaveVersionOlderThen("2.6.11"))
            {

                List<CharacterMarriedLogEntry> lecture = new List<CharacterMarriedLogEntry>();
                foreach (CharacterMarriedLogEntry characterMarriedLogEntry
                        in Campaign.Current.LogEntryHistory.GetGameActionLogs<CharacterMarriedLogEntry>(
                                new Func<CharacterMarriedLogEntry, bool>((logEntry) => { return (logEntry.MarriedHero == Hero.MainHero || logEntry.MarriedTo == Hero.MainHero); })))
                {
                    Hero otherHero = characterMarriedLogEntry.MarriedHero == Hero.MainHero ? characterMarriedLogEntry.MarriedTo : characterMarriedLogEntry.MarriedHero;
                    if (otherHero.IsAlive && !SpouseOfPlayer(otherHero))
                    {
                        CharacterMarriedLogEntry? existe = lecture.Find(x => (x.MarriedHero == otherHero || x.MarriedTo == otherHero));
                        if (existe != null && existe.GameTime < characterMarriedLogEntry.GameTime)
                        { // if we don't fint more rescent marriage
                            lecture.Remove(existe);
                            existe = null;
                        }
                        if (existe == null)
                            lecture.Add(characterMarriedLogEntry);
                    }
                }

                foreach (CharacterMarriedLogEntry characterMarriedLogEntry in lecture)
                {
                    Hero otherHero = characterMarriedLogEntry.MarriedHero == Hero.MainHero ? characterMarriedLogEntry.MarriedTo : characterMarriedLogEntry.MarriedHero;
                    if (!Campaign.Current.LogEntryHistory.GetGameActionLogs<CharacterMarriedLogEntry>
                            (x => x.GameTime > characterMarriedLogEntry.GameTime
                               && ((x.MarriedHero == otherHero && x.MarriedTo != Hero.MainHero)
                                   || (x.MarriedTo == otherHero && x.MarriedHero != Hero.MainHero))).Any())
                    {
                        // it's good, we have to merge
                        spouses.Add(otherHero);
                        logSpouses.Add(otherHero);
#if TRACELOAD
                        Helper.Print("Log spouse add " + Helper.TraceHero(otherHero), Helper.PRINT_TRACE_LOAD);
#endif
                    }
                }

                if (logSpouses.Count > 0)
                {
                    Helper.RemoveExSpouses(Hero.MainHero, false, spouses, false, null);
                }
            }
#endif

            // Parent patch
            bool hadSpouse = Hero.MainHero.Spouse != null;

            Hero mainMaleSpouse = this.Spouses.FirstOrDefault(x => !x.IsFemale);
            Hero mainFemaleSpouse = this.Spouses.FirstOrDefault(x => x.IsFemale);

            int i = 0;
            while (i < Hero.MainHero.Children.Count)
            {
                Hero children = Hero.MainHero.Children[i];
                if (patchParent(children, mainFemaleSpouse, mainMaleSpouse))
                    i--;
                i++;
            }

#if PATCHWITHLOGENTRY
            if (logSpouses.Count > 0)
            {
                foreach (Hero spouse in logSpouses)
                {
                    i = 0;
                    while (i < spouse.Children.Count)
                    {
                        Hero children = spouse.Children[i];
                        if (!Hero.MainHero.Children.Any(x => x == children)
                            && children.Clan == Hero.MainHero.Clan
                            && !SpouseOfPlayer(children))
                        {
                            if (patchParent(children, mainFemaleSpouse, mainMaleSpouse))
                                i--;
                            else
                                Hero.MainHero.Children.Add(children);
                        }
                        i++;
                    }
                }
            }
#endif

#if TRACKTOMUCHSPOUSE
            TrackTooMuchSpouse.Instance().Initialise(Spouses);
#endif

            foreach (Clan clan in Clan.FindAll(c => c.IsClan))
            {
                if (clan.Leader != null && SpouseOfPlayer(clan.Leader))
                {
#if TRACEPATCH
                    Helper.Print(String.Format("Will try to patch Clan {0} Leader is a spouse {1}\r\n\tMainHero ?= {2}", clan.Name, clan.Leader.Name, Hero.MainHero.Name), Helper.PRINT_PATCH);
#endif
                    patchClanLeader(clan);
                }
            }

            foreach (Hero hero in spouses)
            {
                if (hero.IsAlive)
                    Helper.PatchHeroPlayerClan(hero, false, true);

                int nb = 0;
#if TRACEEXSPOUSE
                if (HeroPatch.HeroExspouses(hero) != null)
                    nb = HeroPatch.HeroExspouses(hero).Count;
#endif
                if (hero.ExSpouses != null)
                    nb = hero.ExSpouses.Count;

#if PATCHROMANCE
                Romance.RomanceLevelEnum romance = Romance.GetRomanticLevel(hero, Hero.MainHero);
                if (romance == Romance.RomanceLevelEnum.Ended
                    || romance == Romance.RomanceLevelEnum.Untested)
                {
                    Helpers.Util.CleanRomance(hero, Hero.MainHero, Romance.RomanceLevelEnum.Marriage);
                }
                if (nbMainHero != Hero.MainHero.ExSpouses.Count)
                {
#if TRACELOAD
                    Helper.Print(String.Format("Patch Romance with spouse {2} for mainHero from {0} to {1}\r\n\t=>{3}"
                                , nbMainHero, Hero.MainHero.ExSpouses.Count
                                , hero.Name
                                , Helper.TraceHero(Hero.MainHero)), Helper.PRINT_TRACE_LOAD);
#endif
                    Helper.RemoveExSpouses(Hero.MainHero, false);
                    nbMainHero = Hero.MainHero.ExSpouses.Count;
                }
#endif
                Helper.RemoveExSpouses(hero, false, spouses, true);
#if TRACELOAD
#if TRACEEXSPOUSE
                if (nb != HeroPatch.HeroExspouses(hero).Count)
#else
                if (nb != hero.ExSpouses.Count)
#endif
                    Helper.Print(String.Format("Patch duplicate spouse for {2} from {0} to {1}"
                            , nb
#if TRACEEXSPOUSE
                            , HeroPatch.HeroExspouses(hero).Count
#else
                            , hero.ExSpouses.Count
#endif
                            , hero.Name), Helper.PRINT_TRACE_LOAD);
#endif

            }

#if TRACELOAD
            // Voir HeroAgentSpawnCampaignBehavior.AddPartyHero

            foreach (Hero hero in Clan.PlayerClan.Lords)
            {
                Helper.Print(String.Format("Hero {0} in Clan.PlayerClan.Lords", hero.Name.ToString()), Helper.PRINT_TRACE_LOAD);
            }
            using (IEnumerator<Hero> enumerator2 = Hero.MainHero.CompanionsInParty.GetEnumerator())
            {
                while (enumerator2.MoveNext())
                {
                    Hero companion = enumerator2.Current;
                    Helper.Print(String.Format("Hero {0} in companion via enumerator", companion.Name.ToString()), Helper.PRINT_TRACE_LOAD);
                }
            }

#endif
            Helper.Print(String.Format("patchClanLeader {0}", (bPatchExecute ? "OK SUCCESS" : "RAS")), Helper.PRINT_PATCH | (bPatchExecute ? Helper.PrintHow.PrintForceDisplay : 0));
        }

        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {

#if TRACELOAD
            Version version = Helper.VersionGet;
            //Version versionTest = new Version(4, 5, 3);
            Helper.Print(String.Format("MARomanceCampaignBehavior::OnSessionLaunched Assembly version ?= {0} Save ?= {1}, compare ?= {2}"
                            , version.ToString()
                            , (SaveVersion == null ? "NULL" : SaveVersion.ToString())
                            , (SaveVersion != null ? version.CompareTo(SaveVersion).ToString() : "No Comparison")), Helper.PRINT_TRACE_LOAD) ;

            if (Hero.MainHero.Siblings != null)
            {
                foreach (Hero hero in Hero.MainHero.Siblings)
                    Helper.Print(String.Format("Sibling of main Hero {0}", hero.Name), Helper.PRINT_TRACE_LOAD);
            }

#endif
            Helper.MASettingsClean();
            Helper.MAEtape = Helper.Etape.EtapeLoadPas2;

            if (NoMoreSpouse == null)
                NoMoreSpouse = new List<Hero>();

            // MAHelper.RemoveDuplicatedHero(); No Need 

            patchSpouses(campaignGameStarter);

            foreach (Hero hero in Hero.AllAliveHeroes.ToList())
            {
                // The old fix for occupations not sticking
                //if (hero.Spouse == Hero.MainHero || Hero.MainHero.ExSpouses.Contains(hero))
                //{
                //    Helper.OccupationToLord(hero.CharacterObject);
                //    Helper.PatchHeroPlayerClan(hero, false, true);
                //}
#if TRACKTOMUCHSPOUSE
                String affAncien = null;
#endif
                bool move = false;

#if TRACKTOMUCHSPOUSE
                if (hero.Spouse != null || (hero.ExSpouses != null && hero.ExSpouses.Count > 0))
                    affAncien = Helper.TraceHero(hero);
#endif
                if ((hero.Spouse == Hero.MainHero 
                    || (hero.ExSpouses != null && hero.ExSpouses.Contains(Hero.MainHero)))
                    && !SpouseOfPlayer(hero))
                {
#if TRACEPATCH
                    Helper.Print(String.Format("Remove MainHero from spouse for hero {0}", hero.Name.ToString()), Helper.PRINT_PATCH);
#endif
                    Helper.RemoveExSpouses(hero, false, null, false, Hero.MainHero);
                    move = true;
                }
                else if (SpouseOfPlayer(hero))
                {
                    Helper.OccupationToLord(hero.CharacterObject);
                    Helper.PatchHeroPlayerClan(hero, false, true);
                    move = true;
                }
#if CANHAVESPOUSE
                else if (hero.Spouse == null || (hero.ExSpouses != null && hero.ExSpouses.Count > 0))
                {
                    Helper.RemoveExSpouses(hero);
                    move = true;
                }
#endif
#if TRACKTOMUCHSPOUSE
#if TRACEEXSPOUSE
                if (HeroPatch.HeroExspouses(hero) != null)
#else
                if (hero.Spouse != null || (hero.ExSpouses != null && hero.ExSpouses.Count > 0))
#endif
                {
                    if (affAncien != null && move)
                        Helper.Print(String.Format("Hero alive {0}\r\n\tfrom {1}", Helper.TraceHero(hero), affAncien), Helper.PRINT_TRACE_LOAD);
                    else
                        Helper.Print(String.Format("Hero alive {0}", Helper.TraceHero(hero)), Helper.PRINT_TRACE_LOAD);
                }
#endif
            }

            Helper.MASettings.Patch = false; // no more needed because applied

#if TRACKTOMUCHSPOUSE
            MARomanceCampaignBehavior.VerifySpouse(0, "After patch");
#endif

            AddDialogs(campaignGameStarter);
        }

        private void AfterLoad()
        {
            //if (MainHeroSpouses == null)
            //{
            //    MainHeroSpouses = new List<Hero>();
            //    if (Hero.MainHero.Spouse != null)
            //        MainHeroSpouses.Add(Hero.MainHero.Spouse);

            //}
        }

        public override void SyncData(IDataStore dataStore)
        {
            String saveVersion = null;
            if (dataStore.IsSaving)
            {
                //saveVersion = MAHelper.VersionGet; // typeof(MASubModule).Assembly.GetName().Version;
                saveVersion = Helper.VersionGet.ToString();
            }

            dataStore.SyncData<List<Hero>?>("Partners", ref Partners);
            dataStore.SyncData<List<Hero>?>("NoMoreSpouse", ref NoMoreSpouse);
            dataStore.SyncData<List<PersuasionAttempt>?> ("PreviousCheatPersuasionAttempts", ref _previousCheatPersuasionAttempts);
            //dataStore.SyncData<Version?>("SaveVersion", ref saveVersion); 
            dataStore.SyncData<String>("SaveVersion", ref saveVersion);

#if TRACELOAD
            if (dataStore.IsLoading)
            {
                _hasLoading = true;
                Helper.Print("MARomanceCampaignBehavior::AfterLoad", Helper.PRINT_TRACE_LOAD);
                if (String.IsNullOrWhiteSpace(saveVersion))
                    SaveVersion = null;
                else
                    SaveVersion = Version.Parse(saveVersion);
            }
#endif
        }
#endregion
    }
}
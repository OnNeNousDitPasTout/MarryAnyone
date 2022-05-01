using HarmonyLib;
using Helpers;
using MarryAnyone.Behaviors;
using MarryAnyone.Helpers;
using MarryAnyone.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
#if V1720MORE
    using TaleWorlds.CampaignSystem.CampaignBehaviors;
    using TaleWorlds.CampaignSystem.CharacterDevelopment;
#else
    using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
#endif
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static TaleWorlds.CampaignSystem.Conversation.Tags.ConversationTagHelper;

namespace MarryAnyone.Patches.Behaviors
{

    internal class ForHero
    {
        public Hero _hero;
        public Hero? _spouse = null;
        public Hero? _sauveHeroSpouse = null;
        public Hero? _sauveSpouseSpouse = null;
        public bool _wasPregnant = false;
        public bool _wasSpousePregnant = false;
#if !SPOUSEALLWAYSWITHYOU
        private bool _needRemove = false;
#else
        public bool _canKeep = false;
#endif
        public bool Swap = false;

        public ForHero(Hero hero)
        {
            _hero = hero;
            _wasPregnant = hero.IsPregnant;
            if (hero.Spouse != null)
            {
                _spouse = hero.Spouse;
                _wasSpousePregnant = hero.Spouse.IsPregnant;
            }
        }

        public void SwapSpouse(Hero spouse)
        {
            if (spouse != _hero.Spouse)
            {
                Swap = true;
                _sauveHeroSpouse = _hero.Spouse;
                _sauveSpouseSpouse = null;
                _spouse = spouse;
                if (_spouse != null)
                {
#if !SPOUSEALLWAYSWITHYOU
                    if (_hero == Hero.MainHero)
                        _needRemove = !Helper.IsSpouseOrExSpouseOf(_hero, _spouse);
                    else
                        _needRemove = !Helper.IsSpouseOrExSpouseOf(_spouse, _hero);
#else
                    if (_hero == Hero.MainHero && MARomanceCampaignBehavior.Instance != null)
                        _canKeep = MARomanceCampaignBehavior.Instance.SpouseOfPlayer(_spouse);
                    else
                       if (_spouse == Hero.MainHero && MARomanceCampaignBehavior.Instance != null)
                            _canKeep = MARomanceCampaignBehavior.Instance.SpouseOfPlayer(_hero);
#endif
                    _sauveSpouseSpouse = _spouse.Spouse;
#if !SPOUSEALLWAYSWITHYOU
                    _spouse.Spouse = _hero;
#else
                    if (_canKeep)
                    {
                        if (_hero == Hero.MainHero)
                            _hero.Spouse = _spouse;
                        else
                            _spouse.Spouse = _hero;

                        Helper.RemoveExSpouses(_hero);
                        Helper.RemoveExSpouses(_spouse);

                    }
                    else
                        Helper.SetSpouse(_spouse, _hero, Helper.enuSetSpouse.JustSet);
#endif
                    _wasSpousePregnant = _spouse.IsPregnant;
                }
                else
                {
                    _sauveSpouseSpouse = null;
                    _wasSpousePregnant = false;
                }
#if !SPOUSEALLWAYSWITHYOU
                _hero.Spouse = _spouse;
#else
                if (!_canKeep)
                    Helper.SetSpouse(_hero, _spouse, Helper.enuSetSpouse.JustSet);
#endif
            }
        }
        public void UnSwap()
        {
            if (Swap && !_canKeep)
            {
                if (_spouse != null)
                {
#if !SPOUSEALLWAYSWITHYOU
                    _spouse.Spouse = _sauveSpouseSpouse;
                    Helper.RemoveExSpouses(_spouse, removeHero: (_needRemove ? _hero : null));
                    if (_sauveSpouseSpouse != null)
                        Helper.RemoveExSpouses(_sauveSpouseSpouse);
#else
                    Helper.SetSpouse(_spouse, _sauveSpouseSpouse, Helper.enuSetSpouse.JustSet);
#endif
                }
#if !SPOUSEALLWAYSWITHYOU
                _hero.Spouse = _sauveHeroSpouse;
                Helper.RemoveExSpouses(_hero, removeHero: (_needRemove ? _spouse : null));

                if (_sauveHeroSpouse != null)
                    Helper.RemoveExSpouses(_sauveHeroSpouse);
#else
                Helper.SetSpouse(_hero, _sauveHeroSpouse, Helper.enuSetSpouse.JustSet);
#endif

#if TRACEPREGNANCY
#if !SPOUSEALLWAYSWITHYOU
                Helper.Print(String.Format("UnSwap:: _needRemove ?= {1} Hero {0}", Helper.TraceHero(_hero), _needRemove), Helper.PRINT_TRACE_PREGNANCY);
                if (_spouse != null)
                    Helper.Print(String.Format("UnSwap:: _needRemove ?= {1} spouse {0}", Helper.TraceHero(_spouse), _needRemove), Helper.PRINT_TRACE_PREGNANCY);

                if (_sauveHeroSpouse != null && _sauveHeroSpouse != _spouse)
                    Helper.Print(String.Format("UnSwap:: _sauveHeroSpouse {0}", Helper.TraceHero(_sauveHeroSpouse)), Helper.PRINT_TRACE_PREGNANCY);
#else
                Helper.Print(String.Format("UnSwap:: Hero {0}", Helper.TraceHero(_hero)), Helper.PRINT_TRACE_PREGNANCY);
                if (_spouse != null)
                    Helper.Print(String.Format("UnSwap:: spouse {0}", Helper.TraceHero(_spouse)), Helper.PRINT_TRACE_PREGNANCY);

                if (_sauveHeroSpouse != null && _sauveHeroSpouse != _spouse)
                    Helper.Print(String.Format("UnSwap:: _sauveHeroSpouse {0}", Helper.TraceHero(_sauveHeroSpouse)), Helper.PRINT_TRACE_PREGNANCY);
#endif
#endif
                Swap = false;
            }
#if TRACEPREGNANCY
            else if (Swap && _canKeep)
            {
                Helper.Print(String.Format("UnSwap:: NOT because _canKeep \r\nHero {0}\r\nSpouse {1}"
                                    , Helper.TraceHero(_hero)
                                    , (_spouse != null ? Helper.TraceHero(_spouse) : "NULL")), Helper.PRINT_TRACE_PREGNANCY);
            }
#endif
        }

    }

    // Add in a setting for enabling polyamory so it does not have to be a harem
    [HarmonyPatch(typeof(PregnancyCampaignBehavior))]
    internal static class PregnancyCampaignBehaviorPatch
    {

        private static readonly ShortLifeObject _shortLifeObject = new ShortLifeObject(100);

        private static List<Hero>? _spouses;
        private static Hero? _sideFemaleHero;
        private static bool _playerRelation = false;
        private static ForHero? _forHero = null; 
#if TRACEPREGNANCY
        private static bool _needTrace = false;
#endif
        [HarmonyPatch(typeof(PregnancyCampaignBehavior), "HeroPregnancyCheckCondition", new Type[] { typeof(Hero) })]
        [HarmonyPrefix]
        private static bool HeroPregnancyCheckConditionPatch(Hero hero, PregnancyCampaignBehavior __instance, ref bool __result)
        {
            __result = hero.IsFemale && hero.IsAlive && hero.Age > (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && !CampaignOptions.IsLifeDeathCycleDisabled;

            return false;
        }

        [HarmonyPatch(typeof(PregnancyCampaignBehavior), "DailyTickHero", new Type[] {typeof(Hero) })]
        [HarmonyPrefix]
        private static void DailyTickHeroPrefix(Hero hero)
        {

            if (!_shortLifeObject.Swap(hero))
                return;

            if (_forHero != null && _forHero.Swap)
                _forHero.UnSwap();

            _forHero = new ForHero(hero);
            _spouses = null;
            _sideFemaleHero = null;
            _playerRelation = false;
#if TRACEPREGNANCY
            _needTrace = false;
#endif

            if (hero.IsFemale && HeroInteractionHelper.OkToDoIt(hero))
            {
                bool maRomanceCampaignBehaviorIsOk = MARomanceCampaignBehavior.Instance != null;
                bool isPartner = maRomanceCampaignBehaviorIsOk
                                    && MARomanceCampaignBehavior.Instance.PartnerOfPlayer(hero);

                if (hero.Spouse is null && !isPartner && (hero.ExSpouses is null || hero.ExSpouses.IsEmpty()))
                {
                    //#if TRACEPREGNANCY
                    //                    Helper.Print(string.Format("DailyTickHero:: {0} has No Spouse", hero.Name), Helper.PRINT_TRACE_PREGNANCY);
                    //#endif
                }
                else if (hero == Hero.MainHero 
                            || isPartner 
                            || (maRomanceCampaignBehaviorIsOk && MARomanceCampaignBehavior.Instance.SpouseOfPlayer(hero))) // If you are the MainHero go through advanced process
                {   
                    // MainHero or MainHero spouses
                    _playerRelation = true;
                    MASettings settings = Helper.MASettings;

                    if (_spouses == null)
                        _spouses = new List<Hero>();
#if TRACEPREGNANCY
                    _needTrace = true;
                    Helper.Print(string.Format("DailyTickHero::{0} Pregnant {2}\r\nPolyamory ?= {1}", hero.Name, settings.Polyamory, hero.IsPregnant), Helper.PRINT_TRACE_PREGNANCY);
#endif

                    if ((isPartner || Hero.MainHero.ExSpouses.Contains(hero)) && HeroInteractionHelper.OkToDoIt(hero, Hero.MainHero, true))
                    {
#if TRACEPREGNANCY
                        Helper.Print(string.Format("DailyTickHero::{0} ISPartener or exSpouse add mainHero\r\n=>{1}"
                                                    , hero.Name
                                                    , Helper.TraceHero(Hero.MainHero)), Helper.PRINT_TRACE_PREGNANCY);
#endif
                        _spouses.Add(Hero.MainHero);

                    }

                    if (hero.Spouse != null && HeroInteractionHelper.OkToDoIt(hero, hero.Spouse, Helper.MASettings.ImproveBattleRelation) && _spouses.IndexOf(hero.Spouse) < 0)
                    {
#if TRACEPREGNANCY
                        Helper.Print(String.Format("DailyTickHero::{0} add hero Spouse {1}", hero.Name, hero.Spouse.Name), Helper.PRINT_TRACE_PREGNANCY);
#endif
                        _spouses.Add(hero.Spouse);
                    }

                    if (settings.Polyamory)
                    {
                        if (MARomanceCampaignBehavior.Instance.Partners != null)
                        {

                            foreach (Hero withHero in MARomanceCampaignBehavior.Instance.Partners)
                            {
                                if (withHero != hero && HeroInteractionHelper.OkToDoIt(hero, withHero) && _spouses.IndexOf(withHero) < 0)
                                {
#if TRACEPREGNANCY
                                    Helper.Print(String.Format("DailyTickHero::{0} add partner {1}", hero.Name, withHero.Name), Helper.PRINT_TRACE_PREGNANCY);
#endif

                                    _spouses.Add(withHero);

                                }
                            }

                        }

                        if (Hero.MainHero.ExSpouses != null)
                        {
                            foreach (Hero withHero in Hero.MainHero.ExSpouses)
                            {
                                if (withHero.IsAlive && withHero != hero && HeroInteractionHelper.OkToDoIt(hero, withHero) && _spouses.IndexOf(withHero) < 0)
                                {
#if TRACEPREGNANCY
                                Helper.Print(String.Format("DailyTickHero::{0} add exSpouse {1}", hero.Name, withHero.Name), Helper.PRINT_TRACE_PREGNANCY);
#endif

                                    _spouses.Add(withHero);

                                }
                            }
                        }
                    }
                }
                else
                {
                    if (_spouses == null)
                        _spouses = new List<Hero>();

                    if (hero.Spouse != null 
                        && HeroInteractionHelper.OkToDoIt(hero, hero.Spouse, false) 
                        && _spouses.IndexOf(hero.Spouse) < 0)
                    {
#if TRACEPREGNANCY
                        Helper.Print(String.Format("DailyTickHero::{0} add hero Spouse {1}", hero.Name, hero.Spouse.Name), Helper.PRINT_TRACE_PREGNANCY);
#endif
                        _spouses.Add(hero.Spouse);
                    }

                    if (hero.ExSpouses != null)
                    {
                        foreach (Hero withHero in hero.ExSpouses)
                        {
                            if (withHero.IsAlive && withHero != hero && HeroInteractionHelper.OkToDoIt(hero, withHero) && _spouses.IndexOf(withHero) < 0)
                            {
#if TRACEPREGNANCY
                                Helper.Print(String.Format("DailyTickHero::{0} add exSpouse {1}", hero.Name, withHero.Name), Helper.PRINT_TRACE_PREGNANCY);
#endif

                                _spouses.Add(withHero);

                            }
                        }
                    }
                }

                int i = -1;
                if (_spouses != null && _spouses.Count > 1)
                {
                    // The shuffle!
                    List<int> attractionGoal = new();
                    int attraction = 0;
                    int addAttraction = 0;
                    foreach (Hero spouse in _spouses)
                    {
                        addAttraction = Campaign.Current.Models.RomanceModel.GetAttractionValuePercentage(hero, spouse);
                        attraction += addAttraction * (spouse.IsFemale ? 1 : 3); // To up the pregnancy chance
                        attractionGoal.Add(attraction);
#if TRACEPREGNANCY
                        Helper.Print(string.Format("Spouse {0} attraction {1}", spouse.Name, attraction), Helper.PRINT_TRACE_PREGNANCY);
#endif
                    }
                    int attractionRandom = MBRandom.RandomInt(attraction);
                    Helper.Print("Random: " + attractionRandom, Helper.PRINT_TRACE_PREGNANCY);
                    i = 0;
                    while (i < _spouses.Count)
                    {
                        if (attractionRandom <= attractionGoal[i])
                        {
#if TRACEPREGNANCY
                            Helper.Print(string.Format("Résoud Index{0} => Spouse {1}", i, _spouses[i].Name), Helper.PRINT_TRACE_PREGNANCY);
#endif
                            break;
                        }
                        i++;
                    }
                }
                else if (_spouses != null && _spouses.Count() == 1)
                    i = 0;

                if (i >= 0)
                {
                    if (i >= _spouses.Count) 
                        i = _spouses.Count - 1; // Sécurity
#if TRACKTOMUCHSPOUSE
                    Helper.Print(String.Format("Will swap between {0} \r\nand {1}", Helper.TraceHero(hero), Helper.TraceHero(_spouses[i])), Helper.PrintHow.PrintToLogAndWrite);
#endif
                    _forHero.SwapSpouse(_spouses[i]);
                }
                else
                {

#if !TRACEPREGNANCY
                    if (Helper.MASettings.Debug)
                    {
#endif
                        if (hero == Hero.MainHero)
                            Helper.Print("No spouse or cheating partner allowed for your sex time", Helper.PRINT_TRACE_PREGNANCY);
                        else if (MARomanceCampaignBehavior.Instance != null
                                && (MARomanceCampaignBehavior.Instance.SpouseOfPlayer(hero)
                                || MARomanceCampaignBehavior.Instance.PartnerOfPlayer(hero)))
                            Helper.Print(String.Format("No spouse or cheating partner allowed for {0} sex time", hero.Name), Helper.PRINT_TRACE_PREGNANCY);
#if !TRACEPREGNANCY
                    }
#endif
                    hero.Spouse = null;
                }
            }
            //#if TRACEPREGNANCY
            //            else
            //            {
            //                Helper.Print(string.Format("DailyTickHero:: avoid for {0}", Helper.TraceHero(hero)), Helper.PRINT_TRACE_PREGNANCY);
            //                _needTrace = true;
            //            }
            //#endif

            // Outside of female pregnancy behavior
            if (hero.Spouse is not null)
            {
                if (hero.IsFemale == hero.Spouse.IsFemale)
                {
                    // Decided to do this at the end so that you are not always going out with the opposite gender
#if TRACEPREGNANCY
                    Helper.Print("DailyTickHero:: Spouse Unassigned because (same sex): " + hero.Spouse, Helper.PRINT_TRACE_PREGNANCY);
#endif
                    _sideFemaleHero = hero.Spouse;
                    hero.Spouse.Spouse = null;
                    hero.Spouse = null;
                }
            }
        }

        [HarmonyPatch(typeof(PregnancyCampaignBehavior), "DailyTickHero", new Type[] { typeof(Hero) })]
        [HarmonyPostfix]
        private static void DailyTickHeroPostfix(Hero hero)
        {
            // Make things looks better in the encyclopedia

#if TRACKTOMUCHSPOUSE
            Hero spouse = hero.Spouse;
#endif

#if TRACEPREGNANCY
            String traceAff = "";
#endif

            if (hero.Spouse == null && _sideFemaleHero != null)
                hero.Spouse = _sideFemaleHero;

            bool forHeroOk = _forHero != null && _forHero._hero == hero && _forHero._spouse == hero.Spouse;

            Hero? otherMainHero = null;
            if (hero == Hero.MainHero)
                otherMainHero = hero.Spouse;
            else if (hero.Spouse == Hero.MainHero)
                otherMainHero = hero;

            bool justPregnant = false;

            if (forHeroOk && Helper.MASettings.ImproveRelation && hero != null && hero.Spouse != null)
            {

                bool needNotify = _playerRelation;
                if (needNotify && !Helper.MASettings.NotifyRelationImprovementWithinFamily)
                    needNotify = otherMainHero != null;


                justPregnant = hero.IsPregnant && forHeroOk && !_forHero._wasPregnant;
                int relationChange = 0;
                float zeroAUn = MBRandom.RandomFloat;

                int relactionActuelle = hero.GetRelation(hero.Spouse);
                int compatible = (Helper.TraitCompatibility(hero, hero.Spouse, DefaultTraits.Calculating)
                                + Helper.TraitCompatibility(hero, hero.Spouse, DefaultTraits.Generosity) * 3
                                + Helper.TraitCompatibility(hero, hero.Spouse, DefaultTraits.Valor)) / 2; // TaleWorlds.CampaignSystem.Conversation.Tags.ConversationTagHelper.TraitCompatibility(hero, otherHero, DefaultTraits.Calculating)

#if TRACEPREGNANCY
                traceAff = String.Format("relactionActuelle ?= {0}, compatible ?= {1} zeroAUn ?= {2}", relactionActuelle, compatible, zeroAUn);
#endif
                

                if (justPregnant)
                {
                    relationChange = ((int)(zeroAUn * 9)) + 1 + (compatible > 0 ? compatible * 2 : 0);
                }
                if (relactionActuelle < 0)
                    relationChange = ((int)(zeroAUn * 6)) - 3 + (compatible <= -2 ? -1 : compatible);
                else if (relactionActuelle < 25)
                    relationChange = ((int)(zeroAUn * 5)) - 2 + (compatible <= -2 ? -1 : compatible);
                else if (relactionActuelle < 50)
                    relationChange = ((int)(zeroAUn * 4)) - 1 + (compatible <= -2 ? -1 : compatible);
                else 
                    relationChange = ((int)(zeroAUn * 5)) + (compatible <= -3 ? -2 : compatible);

#if TRACEPREGNANCY
                traceAff += String.Format("\r\n\t => relationChange {0}", relationChange);
#endif

                if (relationChange != 0)
                {
                    if (needNotify)
                    {
                        ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, hero.Spouse, relationChange, false);
                        StringHelpers.SetCharacterProperties("HEROONE", hero.CharacterObject);
                        StringHelpers.SetCharacterProperties("HEROTOW", hero.Spouse.CharacterObject);
                        MBTextManager.SetTextVariable("INCREMENT", relationChange);
                        MBTextManager.SetTextVariable("FINALRELATION", hero.GetRelation(hero.Spouse));
                        Color color = relationChange < 0 ? Color.FromUint(16722716U) : (justPregnant ? Helper.yellowCollor : Color.FromUint(6750105U));
                        TextObject textObject = null;
                        if (justPregnant)
                        {
                            textObject = new TextObject("{=TheTwoOfThemHaveAGoodTime}{HEROONE.NAME} and {HEROTOW.NAME} have a good time together, their relationship up from {INCREMENT} points to {FINALRELATION}");
                            Helper.PrintWithColor(textObject.ToString(), Helper.yellowCollor);
                        }
                        else
                        {
                            if (relationChange > 0)
                                textObject = new TextObject("{=TheTwoOfThemSpendTime}{HEROONE.NAME} and {HEROTOW.NAME} spend time together, their relationship up from {INCREMENT} points to {FINALRELATION}");
                            else
                                textObject = new TextObject("{=TheTwoOfThemSpendTimeDown}{HEROONE.NAME} and {HEROTOW.NAME} spend time together, their relationship down from {INCREMENT} points to {FINALRELATION}");

                            Helper.PrintWithColor(textObject.ToString(), color);
                        }
                    }

                }
            }

#if TRACEPREGNANCY
            if (forHeroOk)
            {
                justPregnant = hero.IsPregnant && forHeroOk && !_forHero._wasPregnant;
                if (_needTrace || justPregnant)
                    Helper.Print(String.Format("Post Pregnancy Hero {0} justPregnant ?= {1}\r\n{2}", hero.Name, justPregnant, traceAff), Helper.PRINT_TRACE_PREGNANCY);

                justPregnant = hero.Spouse != null && hero.Spouse.IsPregnant && forHeroOk && !_forHero._wasSpousePregnant;
                if (_needTrace || justPregnant)
                    Helper.Print(String.Format("Post Pregnancy Spouse {0} justPregnant ?= {1}", hero.Name, justPregnant), Helper.PRINT_TRACE_PREGNANCY);
            }
#endif

            if (_forHero != null)
                _forHero.UnSwap();

            _forHero = null;

#if TRACKTOMUCHSPOUSE
            MARomanceCampaignBehavior.VerifySpouse(0, String.Format("After DailiTick between {0} and {1}", hero.Name, (spouse != null ? spouse.Name : "NULL")));
#endif
        }

        static public void Done()
        {
            _shortLifeObject.Done();
        }

    }
}
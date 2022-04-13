using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;
#if V1720MORE
using TaleWorlds.CampaignSystem.CharacterDevelopment;
#endif

namespace MarryAnyone.Helpers
{

    public class HeroCompatibleTrait
    {
        Hero _hero;
        Hero _otherHero;

        public HeroCompatibleTrait(Hero hero, Hero otherHero)
        {
            _hero = hero;
            _otherHero = otherHero;
        }

        public int TraitCompatible(TraitObject trait, TraitObject otherTrait, bool signeDifferent = false)
        {
            int traitHero = _hero.GetTraitLevel(trait);
            int traitOtherHero = _otherHero.GetTraitLevel(otherTrait);

            if (signeDifferent)
                traitHero *= -1;

            if (traitHero <= 0 && traitOtherHero >= 0)
                return 0;
            return Math.Abs(Math.Min(traitHero, traitOtherHero));
        }
    }

    internal static class HeroInteractionHelper
    {
        public enum ShowWhat
        {
            ShowRAS = 0,
            ShowNotification = 1,
            ShowThroughHelper = 2,
            ShowFinalRelation = 4
        }


        public static bool CanIntegreSpouseInHeroClan(Hero hero, Hero spouse)
        {
            if (spouse.IsFactionLeader && !spouse.IsMinorFactionHero)
            {
                if (hero.Clan.Kingdom != spouse.Clan.Kingdom)
                {
                    if (hero.Clan.Kingdom?.Leader != hero)
                        return false;
                }

            }
            return true;
        }

        public static bool HeroCanMeet(Hero hero, Hero otherHero)
        {
            if (hero.CurrentSettlement != null && hero.CurrentSettlement == otherHero.CurrentSettlement)
                return true;
            if (hero.PartyBelongedTo != null && hero.PartyBelongedTo == otherHero.PartyBelongedTo)
                return true;
            return false;
        }


        public static bool OkToDoIt(Hero hero, Hero? otherHero = null, bool withRelationTest = true)
        {
            if (hero.IsAlive && hero.Age >= Campaign.Current.Models.AgeModel.HeroComesOfAge)
            {
                if (otherHero != null)
                {
                    if (!otherHero.IsAlive 
                        || otherHero.Age < Campaign.Current.Models.AgeModel.HeroComesOfAge 
                        || !HeroCanMeet(hero, otherHero))
                        return false;

                    if (withRelationTest && Helper.MASettings.RelationLevelMinForSex >= 0)
                    {
                        int relation = hero.GetRelation(otherHero);

                        int compatible = Helper.TraitCompatibility(hero, otherHero, DefaultTraits.Calculating)
                                        + Helper.TraitCompatibility(hero, otherHero, DefaultTraits.Generosity) * 2
                                        + Helper.TraitCompatibility(hero, otherHero, DefaultTraits.Valor)
                                        + Helper.TraitCompatibility(hero, otherHero, DefaultTraits.Honor); // TaleWorlds.CampaignSystem.Conversation.Tags.ConversationTagHelper.TraitCompatibility(hero, otherHero, DefaultTraits.Calculating)

                        return relation + compatible > Helper.MASettings.RelationLevelMinForSex;
                    }
                }
                return true;
            }
            return false;
        }

        public static int PositiveTraits(Hero hero)
        {
            return hero.GetTraitLevel(DefaultTraits.Honor) 
                + hero.GetTraitLevel(DefaultTraits.Valor) 
                + hero.GetTraitLevel(DefaultTraits.Generosity) 
                + hero.GetTraitLevel(DefaultTraits.Mercy);
        }

        public static int MAX_COMPATIBLE_BATTLE_TRAIT = 14;
        public static int MAX_COMPATIBLE_BATTLE_TRAIT_ON_3 = 4;
        public static int MAX_COMPATIBLE_BATTLE_TRAIT_ON_7 = 2;

        public static int CompatibleBattleTraits(Hero hero, Hero otherHero)
        {
            HeroCompatibleTrait calc = new HeroCompatibleTrait(hero, otherHero);

            return calc.TraitCompatible(DefaultTraits.Honor, DefaultTraits.Honor)
                    + calc.TraitCompatible(DefaultTraits.Mercy, DefaultTraits.Valor, true)
                    + calc.TraitCompatible(DefaultTraits.Valor, DefaultTraits.Mercy, true)
                    + calc.TraitCompatible(DefaultTraits.Generosity, DefaultTraits.Valor)
                    + calc.TraitCompatible(DefaultTraits.Valor, DefaultTraits.Generosity)
                    + calc.TraitCompatible(DefaultTraits.Calculating, DefaultTraits.Valor)
                    + calc.TraitCompatible(DefaultTraits.Valor, DefaultTraits.Calculating);
        }

        internal static void ChangeHeroRelation(Hero? hero1, Hero? hero2, int coeff, TextObject? raison = null, int maxCoeff = 5, ShowWhat showWhat = ShowWhat.ShowNotification)
        {
            if (hero1 != null && hero2 != null)
            {
#if TRACEBATTLERELATION
                int borne = coeff;
#endif

                bool playerIn = false;

                if (coeff > maxCoeff)
                    coeff = maxCoeff;
                else if (coeff < -maxCoeff)
                    coeff = -maxCoeff;
                if (coeff > 0)
                    coeff = Convert.ToInt32(Math.Round((double) MBRandom.Random.Next(0, coeff * 10) / 10));
                else
                    coeff = Convert.ToInt32(Math.Round((double)MBRandom.Random.Next(coeff * 10, 0) / 10));

                //hero1.SetPersonalRelation(hero2, coeff);
                if (coeff != 0)
                {
                    if (raison != null)
                        showWhat &= ~ShowWhat.ShowNotification;

                    if (hero1 == Hero.MainHero)
                    {
                        ChangeRelationAction.ApplyPlayerRelation(hero2, coeff, false, (showWhat & ShowWhat.ShowNotification) != 0);
                        playerIn = true;
                    }
                    if (hero2 == Hero.MainHero) {
                        ChangeRelationAction.ApplyPlayerRelation(hero1, coeff, false, (showWhat & ShowWhat.ShowNotification) != 0);
                        playerIn = true;
                    }
                    else
                        ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero1, hero2, coeff, (showWhat & ShowWhat.ShowNotification) != 0);

                    if (raison != null)
                    {
                        TextObject text = null;
                        if ((showWhat & ShowWhat.ShowFinalRelation) != 0)
                        {
                            if (playerIn)
                            {
                                if (coeff > 0)
                                    text = new TextObject("{=ChangeRelationUpToFinalWithPlayer} Your relation up from {RELATION} to {FINALRELATION}");
                                else
                                    text = new TextObject("{=ChangeRelationDownToFinalWithPlayer} Your elation down from {RELATION} to {FINALRELATION}");
                            }
                            else
                            {
                                if (coeff > 0)
                                    text = new TextObject("{=ChangeRelationUpToFinal} Their relation up from {RELATION} to {FINALRELATION}");
                                else
                                    text = new TextObject("{=ChangeRelationDownToFinal} Their relation down from {RELATION} to {FINALRELATION}");
                            }
                        }
                        else
                        {
                            if (playerIn)
                            {
                                if (coeff > 0)
                                    text = new TextObject("{=ChangeRelationUpToWithPlayer} Your relation up from {RELATION}");
                                else
                                    text = new TextObject("{=ChangeRelationDownToWithPlayer} Your relation down from {RELATION}");
                            }
                            else
                            {
                                if (coeff > 0)
                                    text = new TextObject("{=ChangeRelationUpTo} Their relation up from {RELATION}");
                                else
                                    text = new TextObject("{=ChangeRelationDownTo} Their relation down from {RELATION}");
                            }
                        }
                        text.SetTextVariable("RELATION", coeff.ToString());
                        text.SetTextVariable("FINALRELATION", hero1.GetRelation(hero2).ToString());

                        Helper.PrintWithColor(raison.ToString() + text.ToString(), coeff > 0 ? 6750105U : 16722716U);
                    }
                }

#if TRACEBATTLERELATION
                Helper.Print(String.Format("ChangeHeroRelation between {0} and {1} coeff ?= {2}/{3}"
                                    , hero1.Name
                                    , hero2.Name
                                    , coeff
                                    , borne), Helper.PrintHow.PrintToLogAndWrite);
#endif
            }

        }
    }
}

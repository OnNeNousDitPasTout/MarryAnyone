using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace MarryAnyone.Helpers
{
    internal static class HeroInteractionHelper
    {
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


    }
}

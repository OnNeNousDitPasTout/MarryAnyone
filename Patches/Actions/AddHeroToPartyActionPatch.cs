using HarmonyLib;
using MarryAnyone.Behaviors;
using MarryAnyone.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace MarryAnyone.Patches.Actions
{
#if SPOUSEALLWAYSWITHYOU
#if NONEED

    static class AddHeroToPartyActionPatch
    {
        [HarmonyPatch(typeof(AddHeroToPartyAction), "ApplyInternal", new Type[] { typeof(Hero), typeof(MobileParty), typeof(bool)})]
        [HarmonyPostfix]
        private static void ApplyInternalPatch(Hero hero, MobileParty newParty, bool showNotification = true)
        {
            if (MARomanceCampaignBehavior.Instance != null)
            {
                if (hero == Hero.MainHero || MARomanceCampaignBehavior.Instance.SpouseOfPlayer(hero))
                {
                    if (Hero.MainHero.Spouse != null && HeroInteractionHelper.OkToDoIt(Hero.MainHero, Hero.MainHero.Spouse))
                        return;

                    Hero otherSpouse = MARomanceCampaignBehavior.Instance.FirstHeroExSpouseOkToDoIt();
                    if (otherSpouse != null) {
                        Hero.MainHero.Spouse = otherSpouse;
                        Helper.RemoveExSpouses(Hero.MainHero);
                        Helper.RemoveExSpouses(otherSpouse);
                    }
                }
                if (MARomanceCampaignBehavior.Instance.Partners != null && MARomanceCampaignBehavior.Instance.Partners.Contains(hero))
            }
        }

    }
#endif
#endif
}

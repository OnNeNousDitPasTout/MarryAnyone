using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
#if V1720MORE
	using TaleWorlds.CampaignSystem.CampaignBehaviors;
#else
	using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
#endif

namespace MarryAnyone.Patches.TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors
{
#if PATCHRANSOMOFFER
    [HarmonyPatch(typeof(RansomOfferCampaignBehavior))]
    static class RansomOfferCampaignBehaviorPatch
    {
        [HarmonyPatch(typeof(RansomOfferCampaignBehavior), "GetCaptorClanOfPrisoner", new Type[] { typeof(Hero)})]
        [HarmonyPrefix]
        static private bool GetCaptorClanOfPrisonerPatch(Hero hero, ref Clan? __result)
        {
			bool ret = true;
			if (hero.PartyBelongedToAsPrisoner == null)
            {
				Helper.Print(String.Format("GetCaptorClanOfPrisonerPatch::Patch for Hero {0}", hero.Name), Helper.PRINT_PATCH);
				__result = null;
				return false;
            }

			if (hero.PartyBelongedToAsPrisoner.IsMobile)
			{
				if ((hero.PartyBelongedToAsPrisoner.MobileParty.IsMilitia 
					|| hero.PartyBelongedToAsPrisoner.MobileParty.IsGarrison 
					|| hero.PartyBelongedToAsPrisoner.MobileParty.IsCaravan 
					|| hero.PartyBelongedToAsPrisoner.MobileParty.IsVillager) && hero.PartyBelongedToAsPrisoner.Owner != null)
				{

					if (hero.PartyBelongedToAsPrisoner.Owner == null)
					{
						Helper.Print(String.Format("GetCaptorClanOfPrisonerPatch::Patch 2 for Hero {0}", hero.Name), Helper.PRINT_PATCH);
						__result = null;
						return false;
					}
					if (hero.PartyBelongedToAsPrisoner.Owner.IsNotable)
					{
						if (hero.PartyBelongedToAsPrisoner.Owner.CurrentSettlement == null)
						{
							Helper.Print(String.Format("GetCaptorClanOfPrisonerPatch::Patch 3 for Hero {0}", hero.Name), Helper.PRINT_PATCH);
							__result = hero.PartyBelongedToAsPrisoner.Owner.Clan;
							return false;
						}
						__result = hero.PartyBelongedToAsPrisoner.Owner.CurrentSettlement.OwnerClan;
					}
					else
					{
						__result = hero.PartyBelongedToAsPrisoner.Owner.Clan;
					}
				}
				else
				{
					__result = hero.PartyBelongedToAsPrisoner.MobileParty.ActualClan;
				}
			}
			else
			{
				if (hero.PartyBelongedToAsPrisoner.Settlement == null)
				{
					Helper.Print(String.Format("GetCaptorClanOfPrisonerPatch::Patch 10 for Hero {0}", hero.Name), Helper.PRINT_PATCH);
					__result = null;
					return false;
				}
				__result = hero.PartyBelongedToAsPrisoner.Settlement.OwnerClan;
			}
			return ret;
		}


	}
#endif
}

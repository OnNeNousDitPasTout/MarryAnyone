using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace MarryAnyone.Helpers
{
    internal static class PartyHelper
    {
		public static void SwapMainParty(MobileParty newMainParty)
		{

			Helper.Print(String.Format("Swap MainParty to {0}", newMainParty.Name.ToString()), Helper.PrintHow.PrintToLogAndWrite);

			FieldInfo field = AccessTools.Field(typeof(Campaign), "<MainParty>k__BackingField");
			if (field == null)
				throw new Exception("Property MainParty not found on Campaign instance");
			field.SetValue(Campaign.Current, newMainParty);

			Helper.Print(String.Format("Swap MainParty to {0} FAIT", newMainParty.Name.ToString()), Helper.PrintHow.PrintToLogAndWrite);

		}

		public static void SwapPartyBelongedTo(Hero hero, MobileParty? party)
		{

			Helper.Print(String.Format("Swap PartyBelongedTo for Hero {0} to party {1}"
							, hero.Name.ToString()
							, (party == null ? "NULL" : party.Name.ToString())), Helper.PrintHow.PrintToLogAndWrite);

			FieldInfo field = typeof(Hero).GetField("_partyBelongedTo", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null)
				throw new Exception("_partyBelongedTo no found on Hero");
			field.SetValue(hero, party);
		}
	}
}

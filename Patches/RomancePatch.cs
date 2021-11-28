using HarmonyLib;
using MarryAnyone.Behaviors;
using MarryAnyone.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace MarryAnyone.Patches
{
    [HarmonyPatch(typeof(Romance))]
    static class Romance_Patch
	{
        [HarmonyPatch(typeof(Romance), "GetCourtedHeroInOtherClan", new Type[] { typeof(Hero), typeof(Hero) })]
        [HarmonyPrefix]
        public static bool GetCourtedHeroInOtherClanPrefix(Hero person1, Hero person2, Hero? __result)
        {
			__result = null;

            if (person2.Clan == null)
                goto avantRetour;

            foreach (Hero person3 in from x in person2.Clan.Lords
									 where x != person2
									 select x)
			{
				if (Romance.GetRomanticLevel(person1, person3) >= Romance.RomanceLevelEnum.MatchMadeByFamily)
				{
					__result = person3;
					return false;
				}
			}

            avantRetour:
			return false;
		}

        [HarmonyPatch(typeof(Romance), "EndAllCourtships", new Type[] { typeof(Hero) })]
        [HarmonyPrefix]
        private static bool EndAllCourtshipsPrefix(Hero forHero)
        {

#if TRACEROMANCE
            Helper.Print(String.Format("EndAllCourtshipsPrefix for {0}", forHero.Name), Helper.PRINT_TRACE_ROMANCE);
#endif

            foreach (Romance.RomanticState romanticState in Romance.RomanticStateList.ToList())
            {
                if ((romanticState.Person1 == forHero || romanticState.Person2 == forHero)  
                        && (romanticState.Level == Romance.RomanceLevelEnum.Marriage
                            || romanticState.Level == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage
                            || romanticState.Level == Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible
                            || romanticState.Level == Romance.RomanceLevelEnum.CourtshipStarted))
                {
                    if (romanticState.Level == Romance.RomanceLevelEnum.Marriage
                            && MARomanceCampaignBehavior.Instance.SpouseOrNot(romanticState.Person1, romanticState.Person2)
                            && Helper.MASettings.Polygamy)
                        ;
                    else
                    {
                        romanticState.Level = Romance.RomanceLevelEnum.Ended;

                        if (romanticState.Level == Romance.RomanceLevelEnum.Marriage)
                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(romanticState.Person1, romanticState.Person2, -30, true);
                        else if (romanticState.Level == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage)
                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(romanticState.Person1, romanticState.Person2, -20, true);
                        else if (romanticState.Level == Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible)
                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(romanticState.Person1, romanticState.Person2, -10, true);
                        else if (romanticState.Level == Romance.RomanceLevelEnum.CourtshipStarted)
                            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(romanticState.Person1, romanticState.Person2, -4, true);

                    }
                }
            }

            if (forHero == Hero.MainHero && forHero.Spouse != null) {
#if TRACEROMANCE
                Helper.Print(String.Format("EndAllCourtshipsPrefix for {0} Swap spouse {1} in exSpouse", forHero.Name, forHero.Spouse), Helper.PRINT_TRACE_ROMANCE);
#endif
                forHero.ExSpouses.AddItem(forHero.Spouse);
                forHero.Spouse = null;
                Helper.RemoveExSpouses(forHero);
            }


            return false;
        }

    }

}

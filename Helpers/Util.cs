using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using static TaleWorlds.CampaignSystem.Romance;

namespace MarryAnyone.Helpers
{
    // OnNeNousDitPasTout/GrandesMaree Patch
    internal static class Util
    {
        //public static bool AreMarried(Hero hero, Hero otherHero)
        //{

        //    if (otherHero == null)
        //        return false;

        //    //if (hero.Spouse == null) NO we can have no spouse and have exSpouses who ares my spouses
        //    //{
        //    //    MAHelper.Print(String.Format("AreMarried FAIL car {0} n'a aucune épouse", hero.Name));
        //    //    return false;
        //    //}

        //    if (hero.Spouse != null && hero.Spouse == otherHero)
        //    {
        //        MAHelper.Print(String.Format("AreMarried SUCCESS because {0} marry {1}", hero.Name, otherHero.Name), MAHelper.PRINT_TRACE_WEDDING);
        //        return true;
        //    }

        //    if (hero.ExSpouses != null && hero.ExSpouses.FirstOrDefault<Hero>(x => x == otherHero) != null)
        //    {
        //        MAHelper.Print(String.Format("AreMarried SUCCESS because {0} hax {1} in his ex-spouses", hero.Name, otherHero.Name), MAHelper.PRINT_TRACE_WEDDING);
        //        return true;
        //    }

        //    MAHelper.Print(String.Format("AreMarried FAIL for {0} and {1}", hero.Name, otherHero.Name), MAHelper.PRINT_TRACE_WEDDING);
        //    return false;
        //}

        public static void CleanRomance(Hero hero, Hero otherHero, RomanceLevelEnum newRomanceLevel = RomanceLevelEnum.Untested)
        {
            RomanticState lastRomance = null;
            int n = 0;
            while (true)
            {
                RomanticState romance = Romance.RomanticStateList.FirstOrDefault<RomanticState>(x => (x.Person1 == hero && x.Person2 == otherHero) || (x.Person2 == hero && x.Person1 == otherHero));
                if (romance != null)
                {
                    if (newRomanceLevel != RomanceLevelEnum.Untested)
                    {
                        if (lastRomance == null)
                            lastRomance = romance;
                        else
                        {
                            if (romance.Level == lastRomance.Level)
                            {
                                if (romance.LastVisit > lastRomance.LastVisit)
                                    lastRomance = romance;
                            }
                            else if (romance.Level == newRomanceLevel)
                                lastRomance = romance;
                            else if (lastRomance.Level == newRomanceLevel)
                                ;
                            else if (romance.LastVisit > lastRomance.LastVisit)
                                lastRomance = romance;

                        }
                    }
                    Romance.RomanticStateList.Remove(romance);
                    n++;
                }
                else
                    break;
            }

            if (n > 0)
                Helper.Print(String.Format("Nettoyage romances entre {0} et {1} => {2} suppressions", hero.Name, otherHero.Name, n), Helper.PrintHow.PrintToLogAndWriteAndDisplay);

            if (newRomanceLevel != RomanceLevelEnum.Untested)
            {
                RomanticState romanticState = null;
#if TRACEPATCH
                RomanceLevelEnum ancRomanceLevel = RomanceLevelEnum.Untested;
#endif
                if (lastRomance == null)
                {
                    romanticState = new RomanticState();
                    romanticState.Person1 = hero;
                    romanticState.Person2 = otherHero;
                }
                else
                {
                    romanticState = lastRomance;
#if TRACEPATCH
                    ancRomanceLevel = lastRomance.Level;
#endif
                }

#if OLDBUG
                romanticState.Level = RomanceLevelEnum.Untested;
#else
                romanticState.Level = newRomanceLevel; // RomanceLevelEnum.Untested;
#endif
#if TRACEPATCH
                Helper.Print(String.Format("PATCH Romance between {0} and {1} swap from {2} to {3}", hero.Name, otherHero.Name, ancRomanceLevel, newRomanceLevel), Helper.PRINT_PATCH);
#endif
                Romance.RomanticStateList.Add(romanticState);
            }

        }


    }
}

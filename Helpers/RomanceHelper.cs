using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace MarryAnyone.Helpers
{
    internal static class RomanceHelper
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
    }
}

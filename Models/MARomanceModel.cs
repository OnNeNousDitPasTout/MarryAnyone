using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
#if V1720MORE
    using TaleWorlds.CampaignSystem.GameComponents;
#else
    using TaleWorlds.CampaignSystem.SandBox.GameComponents;
#endif

namespace MarryAnyone.Models
{
    public class MARomanceModel : DefaultRomanceModel
    {

        static public bool CourtshipPossibleBetweenNPCsStatic(Hero person1, Hero person2)
        {
#if V1720LESS
            Romance.RomanceLevelEnum level = Romance.GetRomanticLevel(person1, person2);
#endif
            return 
#if V1720MORE
                (person1.MapFaction == null || person2.MapFaction == null || !FactionManager.IsAtWarAgainstFaction(person1.MapFaction, person2.MapFaction))
#else
                (level == Romance.RomanceLevelEnum.Untested
                    || level == Romance.RomanceLevelEnum.MatchMadeByFamily
                    || level == Romance.RomanceLevelEnum.CourtshipStarted
                    || level == Romance.RomanceLevelEnum.CoupleDecidedThatTheyAreCompatible
                    || level == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage)                
                && (person2.Clan == null || Romance.GetCourtedHeroInOtherClan(person1, person2) == null)
                && (person1.Clan == null || Romance.GetCourtedHeroInOtherClan(person2, person1) == null)
#endif
                && Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(person1, person2);

        }

#if V1720LESS
        public override bool CourtshipPossibleBetweenNPCs(Hero person1, Hero person2)
        {
            return CourtshipPossibleBetweenNPCsStatic(person1, person2);
        }
#endif
    }
}

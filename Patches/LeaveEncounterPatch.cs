using HarmonyLib;
using MarryAnyone.Behaviors;
using TaleWorlds.CampaignSystem;

namespace MarryAnyone.Patches
{
// Removed the 09/02/2022
//    [HarmonyPatch(typeof(PlayerEncounter), "LeaveEncounter", MethodType.Setter)]
//    internal class LeaveEncounterPatch
//    {
//        private static bool Prefix()
//        {

//#if TRACKTOMUCHSPOUSE
//            MARomanceCampaignBehavior.VerifySpouse(0, string.Format("LeaveEncounter with {0}", (Hero.OneToOneConversationHero != null ? Hero.OneToOneConversationHero.Name.ToString() : "NULL")));
//#endif

//            if (Hero.OneToOneConversationHero is not null)
//            {
//                if (Hero.OneToOneConversationHero.PartyBelongedTo == MobileParty.MainParty)
//                {
//#if TRACKTOMUCHSPOUSE
//                    Helper.Print(string.Format("Don't leave {0}", Hero.OneToOneConversationHero.Name), Helper.PrintHow.PrintToLogAndWriteAndForceDisplay);
//#endif

//                    return false;
//                }
//            }
//            return true;
//        }
//    }
}
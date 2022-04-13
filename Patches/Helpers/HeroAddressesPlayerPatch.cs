using HarmonyLib;
using MarryAnyone.Behaviors;
using TaleWorlds.CampaignSystem;
#if V1720MORE
    using TaleWorlds.CampaignSystem.Conversation;
#endif
using TaleWorlds.Localization;

namespace MarryAnyone.Patches.Helpers
{
    [HarmonyPatch(typeof(ConversationHelper), "HeroAddressesPlayer")]
    internal class HeroAddressesPlayerPatch
    {
        private static void Postfix(ref TextObject __result, Hero talkTroop)
        {
            TextObject tempResult = __result;
            __result = HeroAddressesPlayer(talkTroop);
            if (__result == TextObject.Empty)
            {
                __result = tempResult;
            }
        }

        // Account for different relationships
        private static TextObject HeroAddressesPlayer(Hero talkTroop)
        {
            bool areMarried = false;
            if (MARomanceCampaignBehavior.Instance != null)
                areMarried = MARomanceCampaignBehavior.Instance.SpouseOfPlayer(talkTroop);
            else
                areMarried = Hero.MainHero.Spouse == talkTroop
                            || (Hero.MainHero.ExSpouses != null && Hero.MainHero.ExSpouses.Contains(talkTroop));

            if (areMarried)
            {
                if (Hero.MainHero.IsFemale)
                    return new TextObject("{=t6sRVI5C}My wife", null);
                else
                    return new TextObject("{=rPrBa7gK}My husband", null);
            }
            //// Same-sex
            //if (talkTroop.Spouse == Hero.MainHero && !talkTroop.IsFemale && !Hero.MainHero.IsFemale)
            //{
            //    return new TextObject("{=rPrBa7gK}My husband", null);
            //}
            //if (talkTroop.Spouse == Hero.MainHero && talkTroop.IsFemale && Hero.MainHero.IsFemale)
            //{
            //    return new TextObject("{=t6sRVI5C}My wife", null);
            //}
            //// Polygamy and same-sex
            //if (talkTroop.ExSpouses.Contains(Hero.MainHero) && !talkTroop.IsFemale && !Hero.MainHero.IsFemale)
            //{
            //    return new TextObject("{=rPrBa7gK}My husband", null);
            //}
            //if (talkTroop.ExSpouses.Contains(Hero.MainHero) && talkTroop.IsFemale && Hero.MainHero.IsFemale)
            //{
            //    return new TextObject("{=t6sRVI5C}My wife", null);
            //}
            //// Polygamy
            //if (talkTroop.ExSpouses.Contains(Hero.MainHero) && talkTroop.IsFemale)
            //{
            //    return new TextObject("{=rPrBa7gK}My husband", null);
            //}
            //if (talkTroop.ExSpouses.Contains(Hero.MainHero))
            //{
            //    return new TextObject("{=t6sRVI5C}My wife", null);
            //}
            return TextObject.Empty;
        }
    }
}

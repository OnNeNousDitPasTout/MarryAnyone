using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace MarryAnyone.Patches.Actions
{
#if TRACEKILLCHARACTERBYREMOVE

    [HarmonyPatch(typeof(KillCharacterAction))]
    static class KillCharacterActionPatch
    {

        [HarmonyPatch(typeof(KillCharacterAction), "ApplyByRemove", new Type[] {typeof(Hero), typeof(bool)})]
        [HarmonyPostfix]
        static void ApplyByRemovePostFix(Hero victim, bool showNotification = false)
        {
            Helper.Print(String.Format("KillCharacterAction::ApplyByRemovePatch for {0} showNotification ?= {1}", (victim != null ? victim.Name : "NULL"), showNotification), Helper.PrintHow.PrintToLogAndWriteAndInit);
            //return true;
        }
    }
#endif
}

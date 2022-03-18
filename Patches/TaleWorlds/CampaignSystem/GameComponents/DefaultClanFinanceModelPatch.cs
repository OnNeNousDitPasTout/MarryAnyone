using HarmonyLib;
using MarryAnyone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;

namespace MarryAnyone.Patches.TaleWorlds.CampaignSystem.GameComponents
{
#if PATCHMAINPARTYWAGE

    [HarmonyPatch(typeof(DefaultClanFinanceModel))]
    static class DefaultClanFinanceModelPatch
    {
#if TRACEMAINPARTYWAGE

        [HarmonyPatch(typeof(DefaultClanFinanceModel), "ApplyMoraleEffect", new Type[] {typeof(MobileParty), typeof(int), typeof(int) })]
        [HarmonyPrefix]
        private static bool ApplyMoraleEffectPatch(MobileParty mobileParty, int wage, int paymentAmount)
        {
            if (paymentAmount < wage && wage > 0)
            {
                if (mobileParty == Hero.MainHero.PartyBelongedTo)
                {
                    Helper.Print("My CASE", Helper.PrintHow.PrintToLogAndWrite);
                    Helper.Print("My CASE", Helper.PrintHow.PrintToLogAndWrite);
                    Helper.Print("My CASE", Helper.PrintHow.PrintToLogAndWrite);
                    Helper.Print("My CASE", Helper.PrintHow.PrintToLogAndWrite);
                }
                Helper.Print(String.Format("ApplyMoraleEffectPatch moral down for party {0} wage ?= {1} paymentAmount ?= {2}"
                                                , mobileParty.Name
                                                , wage
                                                , paymentAmount)
                                        , Helper.PrintHow.PrintToLogAndWrite);

                if (mobileParty == Hero.MainHero.PartyBelongedTo)
                {
                    Helper.Print("My CASE", Helper.PrintHow.PrintToLogAndWrite);
                    Helper.Print("My CASE", Helper.PrintHow.PrintToLogAndWrite);
                    Helper.Print("My CASE", Helper.PrintHow.PrintToLogAndWrite);
                    Helper.Print("My CASE", Helper.PrintHow.PrintToLogAndWrite);
                }
            }
            return true;
        }
#endif

        [HarmonyPatch(typeof(DefaultClanFinanceModel), "AddExpenseFromLeaderParty")] // , new Type[] { typeof(Clan), typeof(ExplainedNumber), typeof(bool) })]
        [HarmonyPrefix]
        private static bool AddExpenseFromLeaderPartyPatch(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, DefaultClanFinanceModel __instance)
        {
            Hero leader = clan.Leader;
            MobileParty mobileParty = (leader != null) ? leader.PartyBelongedTo : null;
            if (mobileParty != null && leader != mobileParty.LeaderHero && mobileParty.LeaderHero == Hero.MainHero)
            {
                Helper.Print(String.Format("AddExpenseFromLeaderPartyPatch for clan {0} leader ?= {1} playerClan ?= {2}"
                                                , clan.Name
                                                , leader.Name
                                                , (Clan.PlayerClan != null ? Clan.PlayerClan.Name : "NULL"))
                                            , Helper.PrintHow.PrintToLogAndWrite);
                // Leader is invited 
                return false;
            }
            return true;
        }
    }
#endif
    }

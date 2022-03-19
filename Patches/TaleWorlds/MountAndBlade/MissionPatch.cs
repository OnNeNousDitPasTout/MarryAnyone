using HarmonyLib;
using Helpers;
using MarryAnyone.Behaviors;
using MarryAnyone.Helpers;
using MarryAnyone.MA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace MarryAnyone.Patches.TaleWorlds.MountAndBlade
{
    [HarmonyPatch(typeof(Mission))]
    static class MissionPatch
    {
        private const int BORNE_TRAIT_POSITIF = 2;

        private static ShortLifeObject _affectedAgent = new ShortLifeObject(400);
        private static ShortLifeObject _affectorAgent = new ShortLifeObject(400);



        [HarmonyPatch(typeof(Mission), "OnAgentRemoved", new Type[] { typeof(Agent), typeof(Agent), typeof(AgentState), typeof(KillingBlow) })]
        [HarmonyPostfix]
        private static void OnAgentRemovedPostfix(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            if (_affectedAgent.Swap(affectedAgent) || _affectorAgent.Swap(affectorAgent) && MARomanceCampaignBehavior.Instance != null && Helper.MASettings.ImproveBattleRelation)
            {
                if (affectedAgent != null && affectorAgent != null && affectedAgent.Character != null && affectorAgent.Character != null)
                {
                    if (affectedAgent.Mission != null)
                        MARomanceCampaignBehavior.Instance.VerifyMission(affectedAgent.Mission);

                    MATeam? maTeamAffectedAgent = MARomanceCampaignBehavior.Instance.ResolveMATeam(affectedAgent.Character.StringId);
                    MATeam? maTeamAffectorAgent = MARomanceCampaignBehavior.Instance.ResolveMATeam(affectorAgent.Character.StringId);

//#if TRACEBATTLERELATION
//                    Helper.Print(String.Format("OnAgentRemoved {0} by {1}\r\nResolve TeamAffected {2} killingBlow.IsValid ?= {3} IsMissile ?= {4}\r\nResolve TeamAffector {5}"
//                                    , affectedAgent.Name
//                                    , affectorAgent.Name
//                                    , maTeamAffectedAgent == null ? "NULL" : maTeamAffectedAgent.ToString()
//                                    , killingBlow.IsValid
//                                    , killingBlow.IsMissile
//                                    , maTeamAffectorAgent == null ? "NULL" : maTeamAffectorAgent.ToString()
//                                    ), Helper.PRINT_TRACE_BATTLE_RELATION);

//#endif

                    Hero? heroAffectedAgent = null;
                    Hero? heroAffectorAgent = null;

                    int positiveTraitAffectedAgent = 0;
                    int positiveTraitAffectorAgent = 0;
                    int compatibleBattleTraits = 0;

                    if (maTeamAffectedAgent != null) 
                    {
                        heroAffectedAgent = maTeamAffectedAgent.CurrentHero();
                        if (heroAffectedAgent != null)
                            positiveTraitAffectedAgent = HeroInteractionHelper.PositiveTraits(heroAffectedAgent);
                    }
                    if (maTeamAffectorAgent != null) 
                    { 
                        heroAffectorAgent = maTeamAffectorAgent.CurrentHero();
                        if (heroAffectorAgent != null)
                            positiveTraitAffectorAgent = HeroInteractionHelper.PositiveTraits(heroAffectorAgent);
                    }
                    if (heroAffectedAgent != null && heroAffectorAgent != null)
                        compatibleBattleTraits = HeroInteractionHelper.CompatibleBattleTraits(heroAffectedAgent, heroAffectorAgent);

                    if ((heroAffectedAgent != null || heroAffectorAgent != null) && killingBlow.IsValid)
                    {
                        int coeff = 0;
                        TextObject? raison = null;
                        int coeffRaison = 0;
                        bool isPlayerTeamAffector = heroAffectorAgent != null ? MARomanceCampaignBehavior.Instance.IsPlayerTeam(heroAffectorAgent) : false;
                        bool isPlayerTeamAffected = heroAffectedAgent != null ? MARomanceCampaignBehavior.Instance.IsPlayerTeam(heroAffectedAgent) : false;
#if TRACEBATTLERELATION
                        Helper.Print(String.Format("Resolve heroAffectedAgent {0} isPlayerTeamAffected ?= {1}"
                                                        , heroAffectedAgent == null ? "NULL" : heroAffectedAgent.Name
                                                        , isPlayerTeamAffected), Helper.PRINT_TRACE_BATTLE_RELATION);
                        Helper.Print(String.Format("Resolve heroAffectorAgent {0} isPlayerTeamAffector ?= {1}"
                                                        , heroAffectorAgent == null ? "NULL" : heroAffectorAgent.Name
                                                        , isPlayerTeamAffector), Helper.PRINT_TRACE_BATTLE_RELATION);
#endif
                        if (isPlayerTeamAffected || isPlayerTeamAffector)
                        {
                            if (isPlayerTeamAffected)
                            {
#if TRACEBATTLERELATION
                                Helper.Print(String.Format("CASE isPlayerTeamAffected maTeamAffectorAgent ?= {0} HonorAffector ?= {1} compatibleBattleTraits ?= {2} / {3}"
                                                , maTeamAffectorAgent == null ? "NULL" : maTeamAffectorAgent.ToString()
                                                , heroAffectorAgent.GetTraitLevel(DefaultTraits.Honor)
                                                , compatibleBattleTraits
                                                , HeroInteractionHelper.MAX_COMPATIBLE_BATTLE_TRAIT_ON_3), Helper.PRINT_TRACE_BATTLE_RELATION);
#endif
                                if (maTeamAffectorAgent != null 
                                    && compatibleBattleTraits >= HeroInteractionHelper.MAX_COMPATIBLE_BATTLE_TRAIT_ON_7
                                    && heroAffectorAgent.GetTraitLevel(DefaultTraits.Honor) < 0)
                                {
                                    coeffRaison = -heroAffectorAgent.GetTraitLevel(DefaultTraits.Honor);
                                    coeff += coeffRaison;
                                    if (heroAffectedAgent == Hero.MainHero)
                                        raison = new TextObject("{=BattleRelationLikeWeakOpponentAgainstPlayer}{AFFECTORHERO.NAME} happily taunt you as you fall to the ground !");
                                    else
                                        raison = new TextObject("{=BattleRelationLikeWeakOpponent}{AFFECTORHERO.NAME} happily taunt {AFFECTEDHERO.NAME} as {?AFFECTEDHERO.GENDER}she{?}he{\\?} fall to the ground !");
                                }
                                else if (maTeamAffectorAgent != null && heroAffectorAgent.GetTraitLevel(DefaultTraits.Honor) > 0)
                                {
                                    coeffRaison = heroAffectorAgent.GetTraitLevel(DefaultTraits.Honor);
                                    coeff += coeffRaison;
                                    if (heroAffectedAgent == Hero.MainHero)
                                        raison = new TextObject("{=BattleRelationLikeStrongOpponentAgainstPlayer}{AFFECTORHERO.NAME} respects your strength in battle.");
                                    else
                                        raison = new TextObject("{=BattleRelationLikeStrongOpponent}{AFFECTORHERO.NAME} respects {AFFECTEDHERO.NAME} strength in battle.");
                                }
                            }
                            if (coeff != 0)
                            {
                                if (heroAffectorAgent != null)
                                    StringHelpers.SetCharacterProperties("AFFECTORHERO", heroAffectorAgent.CharacterObject, raison);

                                if (heroAffectedAgent != null)
                                    StringHelpers.SetCharacterProperties("AFFECTEDHERO", heroAffectedAgent.CharacterObject, raison);

                                HeroInteractionHelper.ChangeHeroRelation(heroAffectorAgent, heroAffectedAgent, coeff, raison, showWhat: HeroInteractionHelper.ShowWhat.ShowFinalRelation);
                            }

                            coeff = 0;
                            if (isPlayerTeamAffected && heroAffectedAgent != Hero.MainHero)
                            {
                                float distance = -1;
                                Hero? otherHero = null;
                                Vec3 pos = affectedAgent.Position;
                                foreach (Tuple<Hero, Agent> element in maTeamAffectedAgent._heroes)
                                {
                                    Vec3 otherPos = element.Item2.Position;
                                    float otherDistance = pos.Distance(otherPos);
                                    if (element.Item1 != heroAffectedAgent && (distance == -1 || otherDistance < distance))
                                    {
                                        otherHero = element.Item1;
                                        distance = otherDistance;
                                    }
                                }

                                if (otherHero != null)
                                {
                                    compatibleBattleTraits = HeroInteractionHelper.CompatibleBattleTraits(heroAffectedAgent, otherHero);
#if TRACEBATTLERELATION
                                    Helper.Print(String.Format("Solve {0} {1} at distance {2} compatibleBattleTraits ?= {3} / {4}"
                                                    , heroAffectedAgent.Name
                                                    , otherHero.Name
                                                    , distance
                                                    , compatibleBattleTraits
                                                    , HeroInteractionHelper.MAX_COMPATIBLE_BATTLE_TRAIT_ON_7), Helper.PRINT_TRACE_BATTLE_RELATION);
#endif

                                    if (compatibleBattleTraits < HeroInteractionHelper.MAX_COMPATIBLE_BATTLE_TRAIT_ON_7)
                                    {
                                        coeff = -2;
                                        if (otherHero == Hero.MainHero)
                                            raison = new TextObject("{=BattleRelationLostAFreindPlayer}{AFFECTEDHERO.NAME} rescent you because you were looking away when {?AFFECTEDHERO.GENDER}she{?}he{\\?} fall to the ground.");
                                        else
                                            raison = new TextObject("{=BattleRelationLostAFreind}{AFFECTEDHERO.NAME} rescent {OTHERHERO.NAME} because {?OTHERHERO.GENDER}she{?}he{\\?} was looking away when {?AFFECTEDHERO.GENDER}she{?}he{\\?} fall to the ground.");
                                    }
                                    else if (compatibleBattleTraits >= HeroInteractionHelper.MAX_COMPATIBLE_BATTLE_TRAIT_ON_7)
                                    {
                                        coeff = 2;
                                        if (otherHero == Hero.MainHero)
                                            raison = new TextObject("{=BattleRelationNeedAFreindPlayer}{AFFECTEDHERO.NAME} count on you to avenge {?AFFECTEDHERO.GENDER}her{?}him{\\?}.");
                                        else
                                            raison = new TextObject("{=BattleRelationNeedAFreind}{AFFECTEDHERO.NAME} count on {OTHERHERO.NAME} to avenge {?AFFECTEDHERO.GENDER}her{?}him{\\?}.");
                                    }
                                }

                                if (coeff != 0)
                                {
                                    if (heroAffectorAgent != null)
                                        StringHelpers.SetCharacterProperties("AFFECTEDHERO", heroAffectedAgent.CharacterObject, raison);

                                    if (otherHero != null)
                                        StringHelpers.SetCharacterProperties("OTHERHERO", otherHero.CharacterObject, raison);

                                    HeroInteractionHelper.ChangeHeroRelation(heroAffectedAgent, otherHero, coeff, raison, showWhat: HeroInteractionHelper.ShowWhat.ShowFinalRelation);
                                }
                            }

                            coeff = 0;
                            if (isPlayerTeamAffector && !isPlayerTeamAffected)
                            {
                                if (positiveTraitAffectedAgent >= BORNE_TRAIT_POSITIF)
                                {
                                    coeffRaison = Math.Min(BORNE_TRAIT_POSITIF / 2, 2);
                                    coeff = coeffRaison;
                                    if (heroAffectorAgent == Hero.MainHero)
                                        raison = new TextObject("{=BattleRelationRespectLostAgainstPlayer}{AFFECTEDHERO.NAME} respects your strength in battle.");
                                    else
                                        raison = new TextObject("{=BattleRelationRespectLostAgainstNPC}{AFFECTEDHERO.NAME} respects {AFFECTORHERO.NAME} strength in battle.");
                                }
                                else if (positiveTraitAffectedAgent <= -BORNE_TRAIT_POSITIF)
                                {
                                    coeffRaison = -Math.Max(BORNE_TRAIT_POSITIF / 2, 2);
                                    coeff = coeffRaison;
                                    if (heroAffectorAgent == Hero.MainHero)
                                        raison = new TextObject("{=BattleRelationFrustatedLostAgainstPlayer}{AFFECTEDHERO.NAME} holde a grudge against you when {?AFFECTEDHERO.GENDER}she{?}he{\\?} fall to the ground.");
                                    else
                                        raison = new TextObject("{=BattleRelationFrustatedLostAgainstNPC}{AFFECTEDHERO.NAME} holde a grudge against {AFFECTORHERO.NAME} when {?AFFECTEDHERO.GENDER}she{?}he{\\?} fall to the ground.");
                                }
                            }
                            if (coeff != 0)
                            {
                                if (heroAffectorAgent != null)
                                    StringHelpers.SetCharacterProperties("AFFECTORHERO", heroAffectorAgent.CharacterObject, raison);

                                if (heroAffectedAgent != null)
                                    StringHelpers.SetCharacterProperties("AFFECTEDHERO", heroAffectedAgent.CharacterObject, raison);

                                HeroInteractionHelper.ChangeHeroRelation(heroAffectorAgent, heroAffectedAgent, coeff, raison, showWhat: HeroInteractionHelper.ShowWhat.ShowFinalRelation);
                            }

                            coeff = 0;
                            if (isPlayerTeamAffector)
                            {
                                float distance = -1;
                                Hero? otherHero = null;
                                Vec3 pos = affectorAgent.Position;
                                foreach (Tuple<Hero, Agent> element in maTeamAffectorAgent._heroes)
                                {
                                    Vec3 otherPos = element.Item2.Position;
                                    float otherDistance = pos.Distance(otherPos);
                                    if (element.Item1 != Hero.MainHero && element.Item1 != heroAffectorAgent && (distance == -1 || otherDistance < distance))
                                    {
                                        otherHero = element.Item1;
                                        distance = otherDistance;
                                    }
                                }

                                if (otherHero != null)
                                {
                                    compatibleBattleTraits = HeroInteractionHelper.CompatibleBattleTraits(heroAffectorAgent, otherHero);
#if TRACEBATTLERELATION
                                    Helper.Print(String.Format("Solve {0} {1} at distance {2} compatibleBattleTraits ?= {3}"
                                                    , heroAffectorAgent.Name
                                                    , otherHero.Name
                                                    , distance
                                                    , compatibleBattleTraits), Helper.PRINT_TRACE_BATTLE_RELATION);
#endif

                                    if (compatibleBattleTraits >= HeroInteractionHelper.MAX_COMPATIBLE_BATTLE_TRAIT_ON_3)
                                    {
                                        coeff = 1;
                                        if (heroAffectorAgent == Hero.MainHero)
                                            raison = new TextObject("{=BattleRelationBattleWithFreindPlayer}{OTHERHERO.NAME} happily watches you beat enemies to the ground.");
                                        else
                                            raison = new TextObject("{=BattleRelationBattleWithFreind}{OTHERHERO.NAME} happily watches {AFFECTORHERO.NAME} beat enemies to the ground.");
                                    }
                                }

                                if (coeff != 0)
                                {
                                    if (heroAffectorAgent != null)
                                        StringHelpers.SetCharacterProperties("AFFECTORHERO", heroAffectorAgent.CharacterObject, raison);

                                    if (otherHero != null)
                                        StringHelpers.SetCharacterProperties("OTHERHERO", otherHero.CharacterObject, raison);

                                    HeroInteractionHelper.ChangeHeroRelation(heroAffectorAgent, otherHero, coeff, raison, showWhat: HeroInteractionHelper.ShowWhat.ShowFinalRelation);
                                }
                            }
                        }

#if TRACEBATTLERELATION
                        Helper.LogClose();
#endif
                    }
                }
            }
        }

#if TRACEBATTLERELATION
        [HarmonyPatch(typeof(Mission), "AfterStart")]
        [HarmonyPostfix]
        private static void AfterStartPatch(Mission __instance)
        {
            Helper.Print(String.Format("AfterStartPatch Mission {0}"
                            , (__instance.SceneName == null ? "NULL" : __instance.SceneName)), Helper.PrintHow.PrintToLogAndWrite);

        }

        [HarmonyPatch(typeof(Mission), "FinalizePlayerDeployment")]
        [HarmonyPostfix]
        private static void FinalizePlayerDeploymentPatch(Mission __instance)
        {
            Helper.Print(String.Format("FinalizePlayerDeployment Mission {0}"
                            , (__instance.SceneName == null ? "NULL" : __instance.SceneName)), Helper.PrintHow.PrintToLogAndWrite);

        }

        [HarmonyPatch(typeof(Mission), "ResetMission")]
        [HarmonyPostfix]
        private static void ResetMissionPatch(Mission __instance)
        {
            Helper.Print(String.Format("ResetMissionPatch Mission {0}"
                            , (__instance.SceneName == null ? "NULL" : __instance.SceneName)), Helper.PrintHow.PrintToLogAndWrite);

        }
#endif
        [HarmonyPatch(typeof(Mission), "SetMissionMode", new Type[] { typeof(MissionMode), typeof(bool) })]
        [HarmonyPrefix]
        private static void SetMissionModePatch(bool atStart, MissionMode newMode, Mission __instance)
        {
            if (newMode == MissionMode.Battle && newMode != __instance.Mode && MARomanceCampaignBehavior.Instance != null)
            {
                MARomanceCampaignBehavior.Instance.VerifyMission(__instance, true);
            }

#if TRACEBATTLERELATION
            Helper.Print(String.Format("SetMissionMode Mission {0} newMode ?= {1} aStart ?= {2}"
                            , (__instance.SceneName == null ? "NULL" : __instance.SceneName)
                            , newMode
                            , atStart), Helper.PrintHow.PrintToLogAndWrite);
#endif

        }
    }
}

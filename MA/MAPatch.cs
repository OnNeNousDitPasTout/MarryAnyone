using MarryAnyone.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.LogEntries;
using static TaleWorlds.CampaignSystem.Romance;

namespace MarryAnyone.MA
{
#if NEWSAVE
    static class MAPatch
    {
        internal static void PatchClanLeader(Clan clan)
        {
            Hero? ancLeader = clan.Leader;
            Hero? newLeader = null;
            Hero? heroRAS = null;
            bool supprimeClan = false;

            Helper.Print(String.Format("Nb Heroes in clan {0} ?= {1}", clan.Name, clan.Heroes.Count), Helper.PRINT_PATCH);
            Helper.Print(String.Format("clan({1}).leader.clan ?= {0}", (clan.Leader != null && clan.Leader.Clan != null ? clan.Leader.Clan.Name : "NULL"), clan.Name), Helper.PRINT_PATCH);

            ancLeader.Clan = clan; // to ApplyWithoutSelectedNewLeader work fine
            Dictionary<Hero, int> heirApparents = clan.GetHeirApparents(); // ne fonctionne pas car les héros ne sont pas encores listés dans les clans
                                                                           //heirApparents = new Dictionary<Hero, int>();
                                                                           //int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
                                                                           //foreach (Hero hero in Hero.AllAliveHeroes)
                                                                           //{
                                                                           //    if (hero.Clan == clan && hero != ancLeader && hero.IsAlive && !hero.IsNotSpawned && !hero.IsDisabled && !hero.IsWanderer && !hero.IsNotable && hero.Age >= (float)heroComesOfAge)
                                                                           //    {
                                                                           //        int value = Campaign.Current.Models.HeirSelectionCalculationModel.CalculateHeirSelectionPoint(hero, ancLeader, ref heroRAS);
                                                                           //        heirApparents.Add(hero, value);
                                                                           //    }
                                                                           //}

            if (heirApparents.Count > 0)
            {
                int max = heirApparents.AsEnumerable().Where(x => x.Key != ancLeader).Max(x => x.Value);
                //int max = Max<int>(heirApparents.AsEnumerable().Where(x => x.Key != ancLeader).Select(x => x.Value));
                newLeader = heirApparents.AsEnumerable().FirstOrDefault(x => x.Key != ancLeader && x.Value == max).Key;
            }

            if (newLeader != null)
            {
                ChangeClanLeaderAction.ApplyWithSelectedNewLeader(clan, newLeader);
                ancLeader.Clan = Hero.MainHero.Clan;
            }
            else
            {

                ancLeader.Clan = Hero.MainHero.Clan;

                Helper.Print(String.Format("AncLeader {0} is alive {1} his clan {2}"
                                , ancLeader.Name
                                , ancLeader.IsAlive.ToString()
                                , (ancLeader.Clan != null ? ancLeader.Clan.Name : "NULL")), Helper.PRINT_PATCH);

                if (ancLeader.IsAlive)
                {
                    Helper.Print("ancLeader TRY to leave the clan", Helper.PRINT_PATCH);
                    Helper.RemoveFromClan(ancLeader, clan);
                }
                DestroyClanAction.Apply(clan);
                supprimeClan = true;
            }

            if (supprimeClan)
                Helper.Print(String.Format("PATCH Leader for the clan {0} ERASE the clan", clan.Name), Helper.PRINT_PATCH);
            else if (clan.Leader == ancLeader)
                Helper.Print(String.Format("PATCH Leader for the clan {0} FAIL because leader unchanged", clan.Name), Helper.PRINT_PATCH);
            else
                Helper.Print(String.Format("PATCH Leader for the clan {0} SUCCESS swap the leader from {1} to {2}", clan.Name, ancLeader.Name, clan.Leader == null ? "NULL" : clan.Leader.Name), Helper.PRINT_PATCH);
        }

        // Return true if patch
        internal static bool PatchParent(Hero children, Hero? mainFemaleSpouseHero, Hero? mainMaleSpouseHero)
        {
            bool hadSpouse = mainFemaleSpouseHero != null || mainMaleSpouseHero != null;
            bool mainHeroIsFemale = Hero.MainHero.IsFemale;

            if (hadSpouse && children.Father == Hero.MainHero && children.Mother == Hero.MainHero)
            {
                Helper.Print(string.Format("Will Patch Parent of {0}", children.Name), Helper.PRINT_PATCH);
                if (mainHeroIsFemale)
                    children.Father = mainMaleSpouseHero ?? mainFemaleSpouseHero;
                else
                    children.Mother = mainFemaleSpouseHero ?? mainMaleSpouseHero;
                return true;
            }
            if (children.Father == null)
            {
                Helper.Print(string.Format("Will patch Father of {0}", children.Name), Helper.PRINT_PATCH);
                children.Father = mainHeroIsFemale && mainMaleSpouseHero != null ? mainMaleSpouseHero : Hero.MainHero;
                return true;
            }
            if (children.Mother == null)
            {
                Helper.Print(string.Format("Will patch Mother of {0}", children.Name), Helper.PRINT_PATCH);
                children.Mother = !mainHeroIsFemale && mainFemaleSpouseHero != null ? mainFemaleSpouseHero : Hero.MainHero;
                return true;
            }
            return false;
        }

        #region Log Patch
        public static void LogLectureAdd(List<CharacterMarriedLogEntry> lecture, Hero otherHero, CharacterMarriedLogEntry characterMarriedLogEntry)
        {
            CharacterMarriedLogEntry? existe = lecture.Find(x => (x.MarriedHero == otherHero || x.MarriedTo == otherHero));
            if (existe != null && existe.GameTime < characterMarriedLogEntry.GameTime)
            { // if we don't fint more rescent marriage
                lecture.Remove(existe);
                existe = null;
            }
            if (existe == null)
                lecture.Add(characterMarriedLogEntry);
        }

        public static void LogLectureVerify(List<CharacterMarriedLogEntry> lecture, List<Hero> spouses, List<Hero> logSpouses)
        {
            foreach (CharacterMarriedLogEntry characterMarriedLogEntry in lecture)
            {
                Hero otherHero = characterMarriedLogEntry.MarriedHero == Hero.MainHero
                                        ? characterMarriedLogEntry.MarriedTo
                                        : characterMarriedLogEntry.MarriedHero;

                if (!Campaign.Current.LogEntryHistory.GetGameActionLogs<CharacterMarriedLogEntry>
                        (x => x.GameTime > characterMarriedLogEntry.GameTime
                           && ((x.MarriedHero == otherHero && x.MarriedTo != Hero.MainHero)
                               || (x.MarriedTo == otherHero && x.MarriedHero != Hero.MainHero))).Any())
                {
                    // it's good, we have to merge
                    if (spouses.IndexOf(otherHero) < 0)
                        spouses.Add(otherHero);

                    if (logSpouses.IndexOf(otherHero) < 0)
                        logSpouses.Add(otherHero);
#if TRACELOAD
                    Helper.Print("Log spouse add " + Helper.TraceHero(otherHero), Helper.PRINT_TRACE_LOAD);
#endif
                }
            }
        }
        #endregion

        public static void RemoveMainHeroSpouse_(Hero oldSpouse, Hero withHero)
        {
            // Patch
            if (Romance.GetRomanticLevel(oldSpouse, withHero) == RomanceLevelEnum.Marriage)
                Util.CleanRomance(oldSpouse, withHero, RomanceLevelEnum.Untested);

            Helper.RemoveExSpouses(withHero, Helper.RemoveExSpousesHow.RemoveOtherHero, null, oldSpouse);
        }

        public static void RemoveMainHeroSpouse(Hero withHero, Hero oldSpouse, bool removeHeroFromParty = true)
        {
            Hero? spouseOriginal = ResolveOriginalSpouseForPartner(oldSpouse);
            Clan? clanToJoin = (spouseOriginal != null ? spouseOriginal.Clan : null);

            if (clanToJoin == null
                && oldSpouse.Mother != null
                && oldSpouse.Mother.Clan != null
                && !oldSpouse.Mother.Clan.IsEliminated)
                clanToJoin = oldSpouse.Mother.Clan;

            if (clanToJoin == null
                && oldSpouse.Father != null
                && oldSpouse.Father.Clan != null
                && !oldSpouse.Father.Clan.IsEliminated)
                clanToJoin = oldSpouse.Father.Clan;

            RemoveMainHeroSpouse_(oldSpouse, withHero);

            foreach (Hero mainHeroSpouse in withHero.ExSpouses)
            {
                if (mainHeroSpouse != oldSpouse)
                    RemoveMainHeroSpouse(oldSpouse, mainHeroSpouse);
            }

#if TRACEPATCH
            Helper.Print(String.Format("RemoveMainHeroSpouse:: Patch spouse of hero {0} with new spouse {1}"
                                            , oldSpouse.Name
                                            , (spouseOriginal != null ? spouseOriginal.Name : "NULL")
                                            ), Helper.PRINT_PATCH);
#endif

            Helper.RemoveExSpouses(oldSpouse
                                    , (Helper.RemoveExSpousesHow.CompletelyRemove
                                        | Helper.RemoveExSpousesHow.RemoveOnSpouseToo
                                        | Helper.RemoveExSpousesHow.AddOtherHero)
                                    , null, spouseOriginal);

            if (clanToJoin != null && clanToJoin != oldSpouse.Clan)
            {
#if TRACEPATCH
                Helper.Print(String.Format("RemoveMainHeroSpouse::for hero {0} Swap clan from {1} to {2}"
                                            , oldSpouse.Name
                                            , (oldSpouse.Clan != null ? oldSpouse.Clan.Name : "NULL")
                                            , (clanToJoin != null ? clanToJoin.Name : "NULL"))
                                        , Helper.PRINT_PATCH);
#endif
                Helper.SwapClan(oldSpouse, oldSpouse.Clan, clanToJoin);
            }
            else
            {
                Helper.RemoveFromClan(oldSpouse, Clan.PlayerClan, false);
                Helper.OccupationToCompanion(oldSpouse.CharacterObject);
            }

            if (removeHeroFromParty)
            {
                PartyHelper.SwapPartyBelongedTo(hero: oldSpouse, null);
                if (MobileParty.MainParty.Party.MemberRoster.FindIndexOfTroop(oldSpouse.CharacterObject) >= 0)
                    MobileParty.MainParty.Party.MemberRoster.RemoveTroop(oldSpouse.CharacterObject, 1);
            }

#if TRACKTOMUCHSPOUSE

            VerifySpouse(0, "RemoveMainHeroSpouse");
#endif

        }

        public static Hero? ResolveOriginalSpouseForPartner(Hero partner)
        {
            List<Hero> partnerSpouses = partner.ExSpouses.ToList();
            if (partner.Spouse != null)
                partnerSpouses.Add(partner.Spouse);

            partnerSpouses.RemoveAll(x => x == Hero.MainHero || Hero.MainHero.ExSpouses.Contains(x));

            Hero? spouseOriginal = null;
            if (partnerSpouses.Count > 0)
            {
                spouseOriginal = partnerSpouses.FirstOrDefault(x => x.IsAlive && x.Clan == partner.Clan && x.Spouse == partner);

                if (spouseOriginal == null)
                    spouseOriginal = partnerSpouses.FirstOrDefault(x => x.IsAlive && x.Clan == partner.Clan && x.Spouse == null);

                if (spouseOriginal == null)
                    spouseOriginal = partnerSpouses.FirstOrDefault(x => x.IsAlive && x.Spouse == partner);

                if (spouseOriginal == null)
                    spouseOriginal = partnerSpouses.FirstOrDefault(x => x.IsAlive && x.Spouse == null);
            }

            return spouseOriginal;
        }

    }
#endif
}

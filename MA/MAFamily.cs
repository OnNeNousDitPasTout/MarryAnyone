using MarryAnyone.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace MarryAnyone.MA
{
#if NEWSAVE
    public class MAFamily
    {
#nullable enable
        public Hero MainHero = null;
        private List<Hero>? _buggedSpouses;

        [SaveableProperty(1)]
        public List<Hero> Spouses { get; set; }

        [SaveableProperty(2)]
        public List<Hero> NoMoreSpouses { get; set; }

        [SaveableProperty(3)]
        public List<Hero> Partners { get; set; }

        [SaveableProperty(4)]
        public List<PersuasionAttempt> PreviousCheatPersuasionAttempts { get; set; }

#nullable restore

        public MAFamily(List<Hero>? noMoreSpouses, List<Hero>? partners, List<PersuasionAttempt>? previousCheatPersuasionAttempts)
        {
            NoMoreSpouses = NoMoreSpouses;
            Partners = partners;
            PreviousCheatPersuasionAttempts = previousCheatPersuasionAttempts;
        }

        public MAFamily(Hero hero, Hero spouse)
        {
            Spouses = new List<Hero>();
            Spouses.Add(hero);
            if (spouse != null)
                Spouses.Add(spouse);
            MainHero = hero;
        }

        #region PersuasionCheatAttempt

        public void PersuasionAttemptCheatClean(Hero forHero)
        {
            if (PreviousCheatPersuasionAttempts != null)
            {
                PersuasionAttempt? persuasionAttempt = null;
                while ((persuasionAttempt = PreviousCheatPersuasionAttempts.FirstOrDefault(x => x.PersuadedHero == forHero)) != null)
                    PreviousCheatPersuasionAttempts.Remove(persuasionAttempt);

                if (PreviousCheatPersuasionAttempts.Count == 0)
                    PreviousCheatPersuasionAttempts = null;
            }
        }

        public void PreviousCheatPersuasionAttemptsAdd(PersuasionAttempt persuasionAttempt)
        {
            if (PreviousCheatPersuasionAttempts == null)
                PreviousCheatPersuasionAttempts = new List<PersuasionAttempt>();

            PreviousCheatPersuasionAttempts.Add(persuasionAttempt);
        }
        #endregion

        #region Patch

        public bool IsBuggedSpouse(Hero spouse)
        {
            if (_buggedSpouses != null && _buggedSpouses.Contains(spouse))
                return true;
            return false;
        }

        public void PatchSpouses(Hero mainHero)
        {
            bool bPatchExecute = false;
            int nbMainHero = 0;
            int i = 0;

            bool needPatch = Helper.MASettings.Patch;
            bool needPatchPartner = false;

            MainHero = mainHero;

            if (MainHero.Spouse != null && MainHero.Spouse.HeroState == Hero.CharacterStates.Disabled && MainHero.Spouse.IsAlive)
            {
                MainHero.Spouse.ChangeState(Hero.CharacterStates.Active);
#if TRACELOAD
                Helper.Print(string.Format("patchSpouses:: Active {0}", MainHero.Spouse.Name), Helper.PRINT_PATCH);
#endif
            }
            foreach (Hero hero in MainHero.ExSpouses)
            {
                if (hero.HeroState == Hero.CharacterStates.Disabled && hero.IsAlive)
                {
                    hero.ChangeState(Hero.CharacterStates.Active);
#if TRACELOAD
                    Helper.Print(string.Format("patchSpouses:: Active {0}", hero.Name), Helper.PRINT_PATCH);
#endif
                }
            }

            List<Hero> spouses = new List<Hero>();
            if (MainHero.Spouse != null)
            {
                if (MainHero.Spouse != Hero.MainHero)
                    spouses.Add(MainHero.Spouse);
#if TRACELOAD
                Helper.Print("Main spouse " + Helper.TraceHero(MainHero.Spouse), Helper.PRINT_TRACE_LOAD);
#endif
#if !SPOUSEALLWAYSWITHYOU
                MainHero.Spouse = null;
#endif
            }

            if (MainHero.ExSpouses != null)
            {
                int nb = MainHero.ExSpouses.Count;

                Helper.RemoveExSpouses(MainHero);
                if (MainHero.Spouse != null)
                    Helper.RemoveExSpouses(MainHero.Spouse, Helper.RemoveExSpousesHow.RAS, spouses); // For Encycolpedie

#if TRACELOAD
                if (nb != MainHero.ExSpouses.Count)
                    Helper.Print(String.Format("Patch duplicate spouse for mainHero from {0} to {1}", nb, MainHero.ExSpouses.Count), Helper.PRINT_TRACE_LOAD);
                Helper.Print(String.Format("MainHero {0}", Helper.TraceHero(Hero.MainHero)), Helper.PRINT_TRACE_LOAD);
#endif
                nbMainHero = nb;

                foreach (Hero hero in MainHero.ExSpouses)
                {
                    if (Partners != null && Partners.IndexOf(hero) >= 0)
                    {
                        needPatchPartner = true; // will be verify via logSpouse
                        if (hero.Spouse == MainHero
                            || (hero.ExSpouses != null && hero.ExSpouses.Contains(MainHero)))
                            needPatch = true;
                    }
                    else if (hero.IsAlive && !ResolveNoMoreSpouse(hero) && hero != MainHero)
                    {
                        spouses.Add(hero);

#if TRACELOAD
                        Helper.Print("Other spouse " + Helper.TraceHero(hero), Helper.PRINT_TRACE_LOAD);
#endif
                    }
                }
            }

#if TRACELOAD
            if (Partners != null)
            {
                foreach (Hero hero in Partners)
                    Helper.Print("Partner " + Helper.TraceHero(hero), Helper.PRINT_TRACE_LOAD);
            }

            Helper.Print(String.Format("patchSpouses:: needPatch ?= {0}, needPatchPartner ?= {1} ", needPatch, needPatchPartner), Helper.PRINT_TRACE_LOAD);
#endif

#if PATCHWITHLOGENTRY
            List<Hero> logSpouses = new List<Hero>();
            List<Hero> logFullSpouses = new List<Hero>();
            bool needSaveSpouses = false;

            if (needPatchPartner || needPatch || MARomanceCampaignBehavior.Instance.SaveVersionOlderThen("2.6.11"))
            {

#if TRACEPATCH
                Helper.Print(string.Format("patchSpouses:: Patch with LogEntry"), Helper.PRINT_PATCH);
#endif

                List<CharacterMarriedLogEntry> lecture = new List<CharacterMarriedLogEntry>();
                List<CharacterMarriedLogEntry> lectureFull = new List<CharacterMarriedLogEntry>();
                foreach (CharacterMarriedLogEntry characterMarriedLogEntry
                        in Campaign.Current.LogEntryHistory.GetGameActionLogs<CharacterMarriedLogEntry>(
                                new Func<CharacterMarriedLogEntry, bool>((logEntry)
                                        =>
                                { return (logEntry.MarriedHero == Hero.MainHero || logEntry.MarriedTo == Hero.MainHero); })))
                {
                    Hero otherHero = (characterMarriedLogEntry.MarriedHero == Hero.MainHero
                                        ? characterMarriedLogEntry.MarriedTo
                                        : characterMarriedLogEntry.MarriedHero);

                    if (otherHero.IsAlive)
                    {
                        if (!SpouseOfMainHero(otherHero))
                            MAPatch.LogLectureAdd(lecture, otherHero, characterMarriedLogEntry);

                        MAPatch.LogLectureAdd(lectureFull, otherHero, characterMarriedLogEntry);
                    }
                }

#if TRACEPATCH
                Helper.Print(string.Format("patchSpouses:: Nb LogEntry solved ?= {0}\r\nFullLecture ?= {1}", lecture.Count, lectureFull.Count), Helper.PRINT_PATCH);
#endif
                MAPatch.LogLectureVerify(lecture, spouses, logSpouses);
                MAPatch.LogLectureVerify(lectureFull, spouses, logFullSpouses);

                if (needPatch || MARomanceCampaignBehavior.Instance.SaveVersionOlderThen("2.6.11"))
                {
                    for (i = 0; i < spouses.Count; i++)
                    {
                        Hero spouse = spouses[i];
                        if (logFullSpouses.IndexOf(spouse) < 0)
                        {
                            String aff = String.Format("Your spouse {0} is not in the log\r\nDo you want to remove her/him ?", spouse.Name);
                            Helper.Print(aff, Helper.PrintHow.PrintToLogAndWriteAndForceDisplay);

                            InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_warning").ToString(), aff, true, true, "Remove", "Keep"
                                                                , new Action(() => { MAPatch.RemoveMainHeroSpouse(MainHero, spouse); })
                                                                , new Action(() =>
                                                                {
                                                                    if (_buggedSpouses == null)
                                                                        _buggedSpouses = new List<Hero>();
                                                                    _buggedSpouses.Add(spouse);
                                                                })), false);
                        }
                    }

                    if (logSpouses.Count > 0 || spouses.Count > 0 && needSaveSpouses)
                    {
#if TRACEPATCH
                        Helper.Print(string.Format("patchSpouses:: Apply spouses filter"), Helper.PRINT_PATCH);
#endif

                        Helper.RemoveExSpouses(MainHero, Helper.RemoveExSpousesHow.CompletelyRemove, spouses);
                    }
                }
            }
#endif

            // Parent patch
            bool hadSpouse = MainHero.Spouse != null;

            Hero mainMaleSpouse = this.Spouses.FirstOrDefault(x => !x.IsFemale);
            Hero mainFemaleSpouse = this.Spouses.FirstOrDefault(x => x.IsFemale);

            i = 0;
            while (i < Hero.MainHero.Children.Count)
            {
                Hero children = Hero.MainHero.Children[i];
                if (MAPatch.PatchParent(children, mainFemaleSpouse, mainMaleSpouse))
                    i--;
                i++;
            }

#if PATCHWITHLOGENTRY
            if (logSpouses.Count > 0)
            {
                foreach (Hero spouse in logSpouses)
                {
                    i = 0;
                    while (i < spouse.Children.Count)
                    {
                        Hero children = spouse.Children[i];
                        if (!Hero.MainHero.Children.Any(x => x == children)
                            && children.Clan == Hero.MainHero.Clan
                            && !SpouseOfMainHero(children))
                        {
                            if (MAPatch.PatchParent(children, mainFemaleSpouse, mainMaleSpouse))
                                i--;
                            else
                                Hero.MainHero.Children.Add(children);
                        }
                        i++;
                    }
                }
            }
#endif

#if TRACKTOMUCHSPOUSE
            TrackTooMuchSpouse.Instance().Initialise(Spouses);
#endif

            foreach (Clan clan in Clan.FindAll(c => c.IsClan))
            {
                if (clan.Leader != null && SpouseOfMainHero(clan.Leader))
                {
#if TRACEPATCH
                    Helper.Print(String.Format("Will try to patch Clan {0} Leader is a spouse {1}\r\n\tMainHero ?= {2}", clan.Name, clan.Leader.Name, Hero.MainHero.Name), Helper.PRINT_PATCH);
#endif
                    MAPatch.PatchClanLeader(clan);
                }
            }

            foreach (Hero hero in spouses)
            {
                if (hero.IsAlive)
                    Helper.PatchHeroPlayerClan(hero, false, true);

                int nb = 0;
#if TRACEEXSPOUSE
                if (HeroPatch.HeroExspouses(hero) != null)
                    nb = HeroPatch.HeroExspouses(hero).Count;
#else
                if (hero.ExSpouses != null)
                    nb = hero.ExSpouses.Count;
#endif

#if PATCHROMANCE
                Romance.RomanceLevelEnum romance = Romance.GetRomanticLevel(hero, Hero.MainHero);
                if (romance == Romance.RomanceLevelEnum.Ended
                    || romance == Romance.RomanceLevelEnum.Untested)
                {
                    Helpers.Util.CleanRomance(hero, Hero.MainHero, Romance.RomanceLevelEnum.Marriage);
                }
                if (nbMainHero != Hero.MainHero.ExSpouses.Count)
                {
#if TRACELOAD
                    Helper.Print(String.Format("Patch Romance with spouse {2} for mainHero from {0} to {1}\r\n\t=>{3}"
                                , nbMainHero, Hero.MainHero.ExSpouses.Count
                                , hero.Name
                                , Helper.TraceHero(Hero.MainHero)), Helper.PRINT_TRACE_LOAD);
#endif
                    Helper.RemoveExSpouses(Hero.MainHero);
                    nbMainHero = Hero.MainHero.ExSpouses.Count;
                }
#endif
                Helper.RemoveExSpouses(hero
                                        , (Helper.RemoveExSpousesHow.AddMainHero
                                            | ((needSaveSpouses || needPatchPartner) ? Helper.RemoveExSpousesHow.CompletelyRemove : Helper.RemoveExSpousesHow.RAS))
                                        , spouses);
#if TRACELOAD
#if TRACEEXSPOUSE
                if (nb != HeroPatch.HeroExspouses(hero).Count)
#else
                if (nb != hero.ExSpouses.Count || needSaveSpouses || needPatchPartner)
#endif
                    Helper.Print(String.Format("Patch duplicate spouse for {2} from {0} to {1}\r\n\t=> {3}"
                            , nb
#if TRACEEXSPOUSE
                            , HeroPatch.HeroExspouses(hero).Count
#else
                            , hero.ExSpouses.Count
#endif
                            , hero.Name
                            , Helper.TraceHero(hero)
                            ), Helper.PRINT_TRACE_LOAD);
#endif

            }

            if (Partners != null && needPatchPartner)
            {

                foreach (Hero partner in Partners)
                {
                    if (!logFullSpouses.Contains(partner))
                    {
                        // Patch
                        MAPatch.RemoveMainHeroSpouse(MainHero, partner, false);
                    }
                    else
                        while (Partners.Remove(partner)) ;
                }
            }

#if TRACEPATCHCLAN
            // Voir HeroAgentSpawnCampaignBehavior.AddPartyHero

            foreach (Hero hero in Clan.PlayerClan.Lords)
            {
                Helper.Print(String.Format("Hero {0} in Clan.PlayerClan.Lords", hero.Name.ToString()), Helper.PRINT_TRACE_LOAD);
            }
            using (IEnumerator<Hero> enumerator2 = Hero.MainHero.CompanionsInParty.GetEnumerator())
            {
                while (enumerator2.MoveNext())
                {
                    Hero companion = enumerator2.Current;
                    Helper.Print(String.Format("Hero {0} in companion via enumerator", companion.Name.ToString()), Helper.PRINT_TRACE_LOAD);
                }
            }

#endif
            Helper.Print(String.Format("patchClanLeader {0}", (bPatchExecute ? "OK SUCCESS" : "RAS")), Helper.PRINT_PATCH | (bPatchExecute ? Helper.PrintHow.PrintForceDisplay : 0));

        }
        #endregion

        #region spouse

        public bool Resolve(Hero hero)
        {
            if (MainHero == hero
                || Spouses.Contains(hero))
                return true;

            return false;
        }

        public bool SpouseRemove(Hero hero)
        {

            bool ret = false;
            if (Spouses != null)
            {
                while (Spouses.Remove(hero))
                    ret = true;
                if (Spouses.Count == 0)
                    Spouses = null;
            }
            return ret;
        }

        public bool SpouseOfMainHero(Hero spouse)
        {
            return ((MainHero.Spouse == spouse
                    || MainHero.ExSpouses.IndexOf(spouse) >= 0)
                    && !ResolveNoMoreSpouse(spouse));
        }
        #endregion

        #region NoMoreSpouse
        public bool ResolveNoMoreSpouse(Hero hero)
        {
            if (NoMoreSpouses != null && NoMoreSpouses.Contains(hero))
                return true;
            return false;
        }

        public void NoMoreSpouseAdd(Hero hero)
        {
            if (Resolve(hero) && !ResolveNoMoreSpouse(hero))
            {
                if (MainHero == hero)
                    MainHero = null;

                if (NoMoreSpouses == null)
                    NoMoreSpouses = new List<Hero>();

                NoMoreSpouses.Add(hero);
                SpouseRemove(hero);
            }
        }
        #endregion

        #region Partner

        public bool ResolvePartner(Hero hero)
        {
            if (Partners != null
                && Partners.Contains(hero))
                return true;

            return false;
        }


        public void PartnerAdd(Hero hero)
        {
            if (!Resolve(hero) && !ResolvePartner(hero))
            {
                if (Partners == null)
                    Partners = new List<Hero>();

                Partners.Add(hero);
            }
        }

        public bool PartnerRemove(Hero hero)
        {

            bool ret = false;
            if (Partners != null)
            {
                while (Partners.Remove(hero))
                    ret = true;
                if (Partners.Count == 0)
                    Partners = null;
            }
            return ret;
        }

        #endregion


        public void dispose()
        {
            Spouses = null;
            Partners = null;
            NoMoreSpouses = null;
            PreviousCheatPersuasionAttempts = null;
            _buggedSpouses = null;
        }

    }
#endif
}

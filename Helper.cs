using HarmonyLib;
using MarryAnyone.Patches;
using MarryAnyone.Patches.TaleWorlds.CampaignSystem;
using MarryAnyone.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace MarryAnyone
{
    internal static class Helper
    {

        public const String MODULE_NAME = "MarryAnyone";

#if !NOLOG
        private static FileStream? _fichier = null;
        private static StreamWriter? _sw = null;
        private static bool _needToSupprimeFichier = false;
#endif
        public static MASettings MASettings
        {
            get
            {
                if (_MASettings == null)
                    _MASettings = new MASettings();
                return _MASettings;
            }
        }

        private static MASettings? _MASettings = null;

        public static void MASettingsClean()
        {
            _MASettings = null;
        }

        public enum PrintHow // Bitwise enumération
        {
            PrintRAS = 0,
            PrintDisplay = 1,
            PrintForceDisplay = 2,
            PrintToLog = 4,
            UpdateLog = 8,
            PrintToLogAndWrite = 12,
            PrintToLogAndWriteAndDisplay = 13,
            PrintToLogAndWriteAndForceDisplay = 14,
            CanInitLogPath = 16,
            PrintToLogAndWriteAndInit = 12 | CanInitLogPath,
            PrintToLogAndWriteAndInitAndForceDisplay = PrintToLogAndWriteAndForceDisplay | CanInitLogPath
        }

        public enum Etape
        {
            EtapeInitialize = 1,
            EtapeLoad = 2,
            EtapeLoadPas2 = 4,
        }

        public static Etape MAEtape;

#if TRACEWEDDING
        public const PrintHow PRINT_TRACE_WEDDING = PrintHow.PrintToLogAndWriteAndDisplay;
#else
        public const PrintHow PRINT_TRACE_WEDDING = PrintHow.PrintDisplay;
#endif
#if TRACELOAD
        public const PrintHow PRINT_TRACE_LOAD = PrintHow.PrintToLogAndWriteAndInit;
#else
        public const PrintHow PRINT_TRACE_LOAD = PrintHow.PrintDisplay;
#endif
#if TRACEPREGNANCY
        public const PrintHow PRINT_TRACE_PREGNANCY = PrintHow.PrintToLog | PrintHow.PrintDisplay;
#else
        public const PrintHow PRINT_TRACE_PREGNANCY = PrintHow.PrintDisplay;
#endif
#if TRACEROMANCE
        public const PrintHow PRINT_TRACE_ROMANCE = PrintHow.PrintToLog | PrintHow.UpdateLog;
#else
        public const PrintHow PRINT_TRACE_ROMANCE = PrintHow.PrintRAS;
#endif
#if TRACEROMANCEISSUITABLE
        public const PrintHow PRINT_TRACE_ROMANCE_IS_SUITABLE = PrintHow.PrintToLog | PrintHow.UpdateLog;
#else
        public const PrintHow PRINT_TRACE_ROMANCE_IS_SUITABLE = PrintHow.PrintRAS;
#endif

#if TRACELOAD
        public const PrintHow PRINT_PATCH = PrintHow.PrintToLogAndWriteAndInitAndForceDisplay;
#else
        public const PrintHow PRINT_PATCH = PrintHow.PrintToLogAndWrite;
#endif

#if TRACECREATECLAN
        public const PrintHow PRINT_TRACE_CREATE_CLAN = PrintHow.PrintToLog | PrintHow.UpdateLog;
#else
        public const PrintHow PRINT_TRACE_CREATE_CLAN = PrintHow.PrintDisplay;
#endif
#if TRACECHEAT
        public const PrintHow PRINT_TRACE_CHEAT = PrintHow.PrintDisplay | PrintHow.PrintToLog | PrintHow.UpdateLog;
#else
#endif
#if TRACE_ARENA_PARTICIPANT_START
        public const PrintHow PRINT_TRACE_ARENA_PARTICIPANT = PrintHow.PrintForceDisplay | PrintHow.PrintToLog | PrintHow.UpdateLog;
#else
        public const PrintHow PRINT_TRACE_ARENA_PARTICIPANT = PrintHow.PrintDisplay;
#endif
#if TRACEBATTLERELATION
        public const PrintHow PRINT_TRACE_BATTLE_RELATION = PrintHow.PrintToLog;
#endif
#if TRACEINIT
        public const PrintHow PRINT_TRACE_INIT = PrintHow.PrintToLogAndWriteAndInit;
#endif
#if !NOLOG
        public static string? LogPath
        {
            get => _logPath;
            private set
            {
                _logPath = value;
                _needToSupprimeFichier = true;
            }
        }
        private static string? _logPath = null;

        internal static void InitLogPath(bool force)
        {
            if (force || String.IsNullOrWhiteSpace(LogPath))
            {
                var dirpath = System.IO.Path.Combine(TaleWorlds.Engine.Utilities.GetLocalOutputPath(), Helper.MODULE_NAME);
                try
                {
                    if (!Directory.Exists(dirpath))
                    {
                        Directory.CreateDirectory(dirpath);
                    }
                    Helper.Print("Output directory : " + dirpath, Helper.PrintHow.PrintForceDisplay);
                }
                catch
                {
                    Helper.Print("Failed to create config directory.  Please manually create this directory: " + dirpath, Helper.PrintHow.PrintForceDisplay);
                }

                LogPath = dirpath;
            }
        }
#endif

        public enum RemoveExSpousesHow
        {
            RAS = 0,
            CompletelyRemove = 1,
            RemoveMainHero = 2,
            AddMainHero = 4,
            OtherSpousesStrict = 8,
            RemoveOtherHero = 16,
            AddOtherHero = 32,
            RemoveOnSpouseToo = 64,
            AddOnSpouseToo = 128,
            RemoveIfDeadToo = 256

        }

        public static Version VersionGet
        {
            get { 
                if (_version == null) {
                    //_version = Assembly.GetEntryAssembly().GetCustomAttributes<Version>().ToString();
                    _version = typeof(MASubModule).Assembly.GetName().Version;
                }
                return _version;
            }

        }
        private static Version? _version = null;

        public static string ModuleNameGet
        {
            get
            {
                if (_moduleName == null)
                {
                    _moduleName = typeof(MASubModule).Assembly.GetName().Name.ToString();
                    if (_moduleName == null)
                        _moduleName = "Retrieve module name FAIL";
                }
                return _moduleName;
            }
        }
        private static string? _moduleName = null;

        public static Color yellowCollor = new Color(0, .8f, .4f);

        public static void Print(string message, PrintHow printHow = PrintHow.PrintRAS)
        {
            if ((MASettings.Debug  && (printHow & PrintHow.PrintDisplay) != 0)  || (printHow & PrintHow.PrintForceDisplay) !=0)
            {
                // Custom purple!
                Color color = new(0.6f, 0.2f, 1f);
                InformationManager.DisplayMessage(new InformationMessage(message, color));
            }

#if !NOLOG
            if ((printHow & PrintHow.PrintToLog) != 0 && (printHow & PrintHow.CanInitLogPath) != 0 && LogPath == null)
                InitLogPath(false);

            if ((printHow & PrintHow.PrintToLog) != 0 && LogPath != null)
            {
                Log(message);
            }
            if ((printHow & PrintHow.UpdateLog) != 0 && LogPath != null)
                LogClose();
#endif
        }

        public static void PrintWithColor(string message, uint color)
        {
            PrintWithColor(message, Color.FromUint(color));
        }

        public static void PrintWithColor(string message, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(message, color));
        }

#if !NOLOG
        public static void Log(string text, string? prefix = null)
        {
            if (_sw == null && !string.IsNullOrEmpty(LogPath))
            {
                try
                {
                    if (_needToSupprimeFichier)
                    {
                        _fichier = new FileStream(LogPath + "\\MarryAnyOne.log", FileMode.Create, FileAccess.Write, FileShare.Read);
                        _needToSupprimeFichier = false;
                    }
                    else
                        _fichier = new FileStream(LogPath + "\\MarryAnyOne.log", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

                    if (_fichier != null)
                    {
                        _fichier.Seek(0, SeekOrigin.End);
                        _sw = new StreamWriter(stream: (FileStream)_fichier, encoding: System.Text.Encoding.UTF8);
                    }

                }
                catch (Exception ex)
                {
                    Print("Exception during StreamWrite" + ex.ToString(), PrintHow.PrintForceDisplay);
                    //Something has gone horribly wrong.
                }
            }

            if (_sw != null)
            {
                //string version = ModuleInfo.GetModules().Where(x => x.Name == "Tournaments XPanded").FirstOrDefault().Version.ToString();
                string prefixCalcule = string.Concat(ModuleNameGet, "(", VersionGet, ") ", prefix != null ? prefix + "" : "", "[", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss"), "]::");
                _sw.WriteLine(prefixCalcule + text.Replace("\r\n", "\r\n" + prefixCalcule + "\t"));
            }
        }

        public static void LogClose()
        {
            try
            {
                try
                {
                    if (_sw != null)
                    {
                        _sw.Flush();
                        _sw.Close();
                        _sw = null;
                    }
                }
                finally
                {
                    _sw = null;
                    if (_fichier != null)
                        _fichier.Dispose();
                    _fichier = null;
                }
            }
            finally
            {
                _fichier = null;

            }
        }
#endif

        public static void Error(Exception exception)
        {
            String message = ModuleNameGet + ": " + exception.Message;
            InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
#if !NOLOG
            Log(message, "ERROR");
#endif
        }

        private static int NbSpouse(Hero hero)
        {
#if TRACEEXSPOUSE
            return (hero.Spouse != null ? 1 : 0) + HeroPatch.HeroExspouses(hero).Count;
#else
            return (hero.Spouse != null ? 1 : 0) + hero.ExSpouses.Count;
#endif
        }

        public static bool IsSpouseOrExSpouseOf(Hero hero, Hero spouse)
        {
            return (hero.Spouse == spouse || (hero.ExSpouses != null && hero.ExSpouses.Contains(spouse)));
        }

#if PATCHHOMESETTLEMENT
        public static void PatchHomeSettlement(Hero hero)
        {
            if (hero.HomeSettlement == null)
                hero.UpdateHomeSettlement();

            if (hero.HomeSettlement == null && hero.BornSettlement == null)
            {
                Settlement? bornSettlement = null;
                if (hero.Culture != null)
                    bornSettlement = Settlement.FindAll(x => x.Culture == hero.Culture).GetRandomElementInefficiently();

                if (bornSettlement == null)
                    bornSettlement = Settlement.All.GetRandomElementInefficiently();

                hero.BornSettlement = bornSettlement;
                hero.UpdateHomeSettlement();
            }

            if (hero.HomeSettlement == null)
            {
                Print(String.Format("Settlement not resolved for {0} bornSettlement ?= {1}"
                        , hero.Name
                        , (hero.BornSettlement != null ? hero.BornSettlement.Name : "NULL"))
                    , PrintHow.PrintToLogAndWriteAndForceDisplay);

                throw new Exception(String.Format("Settlement not resolved for {0}", hero.Name));
            }
        }
#endif
        // completelyRemove : remove all spouse alive
        //public static void RemoveExSpouses(Hero hero, bool completelyRemove = false, List<Hero>? otherSpouse = null, bool withMainHero = false, Hero removeHero = null)
        //public static void RemoveExSpouses(Hero hero, bool completelyRemove = false, List<Hero>? otherSpouse = null, bool withMainHero = false, Hero removeHero = null)
        public static void RemoveExSpouses(Hero hero, RemoveExSpousesHow comment = RemoveExSpousesHow.RAS, List<Hero>? otherSpouses = null, Hero? otherHero = null)
        {

            //#if TRACKTOMUCHSPOUSE
            //            String aff = String.Format("RemoveExSpouses for Hero {0} NbSpouse ?= {1} completelyRemove ?= {2} NbOtherSpouse ?= {3} withMainHero ?= {4}"
            //                                        , hero.Name.ToString()
            //                                        , NbSpouse(hero).ToString()
            //                                        , completelyRemove.ToString()
            //                                        , (otherSpouse == null ? 0 : otherSpouse.Count.ToString())
            //                                        , withMainHero);
            //#endif


#if TRACEEXSPOUSE
            HeroPatch.heroJustifie = hero;
#endif

            FieldInfo _exSpouses = AccessTools.Field(typeof(Hero), "_exSpouses");
            List<Hero> _exSpousesList = (List<Hero>)_exSpouses.GetValue(hero);
            FieldInfo ExSpouses = AccessTools.Field(typeof(Hero), "ExSpouses");

            if (otherHero != null && hero.Spouse == otherHero && (comment & RemoveExSpousesHow.AddOtherHero) == 0)
            {
                if ((comment & RemoveExSpousesHow.RemoveOnSpouseToo) != 0)
                {
                    if (otherHero.Spouse == hero)
                        SetSpouse(otherHero.Spouse, null, enuSetSpouse.JustSet);
                }

                SetSpouse(hero, null, enuSetSpouse.JustSet);

                if (Romance.GetRomanticLevel(hero, otherHero) == Romance.RomanceLevelEnum.Marriage)
                    Helpers.Util.CleanRomance(hero, otherHero, Romance.RomanceLevelEnum.Ended);
            }

            if (_exSpousesList == null)
                _exSpousesList = new List<Hero>();
            else
                _exSpousesList = _exSpousesList.Distinct().ToList(); // Get exspouse list without duplicates

            if ((comment & RemoveExSpousesHow.AddMainHero) != 0)
            {
                if (hero.Spouse != null)
                    _exSpousesList.Add(hero.Spouse);
                SetSpouse(hero, Hero.MainHero, enuSetSpouse.JustSet);
            }

            // Nettoyage
            if ((comment & RemoveExSpousesHow.CompletelyRemove) != 0 && _exSpousesList.Count > 0)
            {
                if (((comment & RemoveExSpousesHow.RemoveOnSpouseToo) != 0
                     || ((comment & RemoveExSpousesHow.RemoveIfDeadToo) == 0)))
                {
                    // Remove exspouse completely from list
                    _exSpousesList = _exSpousesList.Distinct().ToList();
                    List<Hero>? exSpouses = null;
                    if ((comment & RemoveExSpousesHow.RemoveIfDeadToo) != 0)
                        exSpouses = _exSpousesList.ToList();
                    else
                        exSpouses = _exSpousesList.Where(exSpouse => exSpouse.IsAlive).ToList();

                    foreach (Hero exSpouse in exSpouses)
                    {
                        if ((comment & RemoveExSpousesHow.RemoveOnSpouseToo) != 0 && exSpouse.Spouse == hero)
                            SetSpouse(exSpouse.Spouse, null, enuSetSpouse.JustSet);

                        while (_exSpousesList.Remove(exSpouse))
                            ;
                    }
                }
                if ((comment & RemoveExSpousesHow.RemoveIfDeadToo) != 0)
                    _exSpousesList.Clear();
            }

            if (otherHero != null && (comment & RemoveExSpousesHow.AddOtherHero) != 0)
            {
                if (hero.Spouse == null)
                {
                    SetSpouse(hero, otherHero, enuSetSpouse.JustSet);

                    if ((comment & RemoveExSpousesHow.AddOnSpouseToo) != 0 && otherHero.Spouse == null)
                        SetSpouse(otherHero, hero, enuSetSpouse.JustSet);
                }
                else
                {
                    if (_exSpousesList.IndexOf(otherHero) < 0)
                        _exSpousesList.Add(otherHero);
                }
            }

            if (otherSpouses != null)
            {
                foreach (Hero spouse in otherSpouses)
                {
                    if (spouse != hero && spouse != hero.Spouse && _exSpousesList.IndexOf(spouse) < 0)
                    {
                        if ((comment & RemoveExSpousesHow.AddOnSpouseToo) != 0 && spouse.Spouse == null)
                            SetSpouse(spouse, hero, enuSetSpouse.JustSet);

                        _exSpousesList.Add(spouse);
                    }
                }
            }

            if (hero.Spouse != null)
                while (_exSpousesList.Remove(hero.Spouse))
                    ;

            while (_exSpousesList.Remove(hero))
                    ;

            while (otherHero != null && (comment & RemoveExSpousesHow.AddOtherHero) == 0 && _exSpousesList.Remove(otherHero))
                ;

#if CANHAVESPOUSE
            if ((comment & RemoveExSpousesHow.AddMainHero) == 0 && otherSpouses == null)
            {
#if SPOUSEALLWAYSWITHYOU
                if (hero.Spouse == null && _exSpousesList.Count > 0)
                {
                    SetSpouse(hero, _exSpousesList[_exSpousesList.Count - 1], enuSetSpouse.SetReciproqueIFNullOnReciproque);
                    _exSpousesList.RemoveAt(_exSpousesList.Count - 1);
                }
#else
                if (hero.Spouse == null && _exSpousesList.Count > 0 && _exSpousesList[_exSpousesList.Count - 1].Spouse == null)
                    hero.Spouse = _exSpousesList[_exSpousesList.Count - 1];
#endif
            }
#endif

            _exSpouses.SetValue(hero, _exSpousesList);

            MBReadOnlyList<Hero> ExSpousesReadOnlyList = _exSpousesList.GetReadOnlyList<Hero>();
            ExSpouses.SetValue(hero, ExSpousesReadOnlyList);

//#if TRACKTOMUCHSPOUSE
//            Helper.Print(String.Format("RemoveExSpouses {0}\r\n\t=> Spouses {1}", aff, hero.ExSpouses?.Count), Helper.PrintHow.PrintToLogAndWrite);
//#endif

        }

#if SPOUSEALLWAYSWITHYOU

        public enum enuSetSpouse
        {
            JustSet = 0,
            SetReciproque = 1,
            TestNullReciproque = 2,
            TestNull = 4,
            SetReciproqueIFNullOnReciproque = SetReciproque | TestNullReciproque,
            UseStandartAffectation = 8

        }

        // return false if not do
        internal static bool SetSpouse(Hero hero, Hero? spouse, enuSetSpouse comment)
        {
            if ((comment & enuSetSpouse.TestNull) != 0 && hero.Spouse != null)
                return false;

            if ((comment & enuSetSpouse.UseStandartAffectation) != 0)
            {
                if ((comment & enuSetSpouse.TestNullReciproque) != 0 && spouse != null && spouse.Spouse != null)
                    return false;

                hero.Spouse = spouse;
                return true;
            }
            FieldInfo _spouse = AccessTools.Field(typeof(Hero), "_spouse");
            if (_spouse == null)
                throw new Exception("_spouse property nof found on Hero Class !");
            _spouse.SetValue(hero, spouse);

            if (spouse != null 
                && (comment & enuSetSpouse.SetReciproque) != 0
                && ((comment & enuSetSpouse.TestNullReciproque) == 0
                    || spouse.Spouse == null)
                )
                _spouse.SetValue(spouse, hero);

            return true;
        }
#endif

        public static void RemoveDuplicatedHero()
        {
            CampaignObjectManager coManager = Campaign.Current.CampaignObjectManager;

            FieldInfo field = AccessTools.Field(typeof(CampaignObjectManager), "<AliveHeroes>k__BackingField");
            if (field == null)
                throw new Exception("Property AliveHeroes not found on CampaignObjectManager instance");

            List<Hero> _alivesHero = coManager.AliveHeroes.ToList();
            _alivesHero.Sort((x, y) => {return String.Compare(x.StringId, y.StringId, StringComparison.Ordinal);});

            for(int i = 0; i < _alivesHero.Count - 1; i++)
            {
                Hero current = _alivesHero[i];
                Hero next = _alivesHero[i + 1];
#if TRACELOAD
                if (String.Equals(current.StringId, next.StringId, StringComparison.Ordinal))
                {
                    Helper.Print(String.Format("Duplicated alive hero {2}\r\n\t{0}\r\n\t{1}", TraceHero(current), TraceHero(next), i), PRINT_PATCH);
                }
                else
                    Helper.Print(String.Format("hero {2}: {0} ({1}) not duplicated", current.Name, current.StringId, i), PRINT_PATCH);
#endif
            }
            Helper.Print(String.Format("RemoveDuplicatedHero parcours {0} heroes", _alivesHero.Count), PRINT_PATCH);
        }

        public static void OccupationToLord(CharacterObject character)
        {
            if (character.Occupation != Occupation.Lord)
            {
#if V1640MORE
                Hero hero = character.HeroObject;
                if (hero != null)
                {
                    hero.SetNewOccupation(Occupation.Lord);
                }
                //    AccessTools.Property(typeof(Hero), "Occupation").SetValue(hero, Occupation.Lord);
                AccessTools.Field(typeof(CharacterObject), "_occupation").SetValue(character, Occupation.Lord);
#else
                    AccessTools.Property(typeof(CharacterObject), "Occupation").SetValue(character, Occupation.Lord);
#endif
                if (CharacterObject.PlayerCharacter != null)
                {
                    AccessTools.Field(typeof(CharacterObject), "_originCharacter").SetValue(character, CharacterObject.PlayerCharacter);
#if V1710LESS
                    AccessTools.Field(typeof(CharacterObject), "_originCharacterStringId").SetValue(character, CharacterObject.PlayerCharacter.StringId);
#endif
                }
#if TRACE
                else
                    Print("OccupationToLord not full because CharacterObject.PlayerCharacter is NULL", PRINT_PATCH);
#endif
                Print(String.Format("Swap Occupation To Lord for {0} newOccupation ?= {1}", character.Name.ToString(), character.Occupation.ToString()), PrintHow.PrintToLogAndWriteAndDisplay);
            }
        }

        public static void OccupationToCompanion(CharacterObject character)
        {
            if (character.Occupation != Occupation.Wanderer)
            {
#if V1640MORE
                Hero hero = character.HeroObject;
                if (hero != null)
                {
                    hero.SetNewOccupation(Occupation.Wanderer);
                }
                //    AccessTools.Property(typeof(Hero), "Occupation").SetValue(hero, Occupation.Lord);
                AccessTools.Field(typeof(CharacterObject), "_occupation").SetValue(character, Occupation.Wanderer);
#else
                    AccessTools.Property(typeof(CharacterObject), "Occupation").SetValue(character, Occupation.Wanderer);
#endif

                AccessTools.Field(typeof(CharacterObject), "_originCharacter").SetValue(character, CharacterObject.PlayerCharacter);
#if V1710LESS
                AccessTools.Field(typeof(CharacterObject), "_originCharacterStringId").SetValue(character, CharacterObject.PlayerCharacter.StringId);
#endif
#if TRACEPATCH
                Print(String.Format("Swap Occupation To Lord for {0} newOccupation ?= {1}", character.Name.ToString(), character.Occupation.ToString()), PrintHow.PrintToLogAndWriteAndDisplay);
#endif
            }
        }

        public static void RemoveFromClan(Hero hero, Clan fromClan, bool canPatchLeader = false)
        {
            List<Hero> lords = fromClan.Lords.ToList();
            if (lords.IndexOf(hero) >= 0)
            {
                while (lords.IndexOf(hero) >= 0)
                    lords.Remove(hero);

                FieldInfo infoLords = AccessTools.Field(typeof(Clan), "<Lords>k__BackingField");
                if (infoLords == null)
                    throw new Exception("<Lords>k__BackingField not found");
                infoLords.SetValue(fromClan, new MBReadOnlyList<Hero>(lords));
#if TRACEPATCH
                Print(String.Format("Patch Clan lords of clan {0}", fromClan.Name.ToString()), PRINT_PATCH);
#endif
            }

            List<Hero> heroes = fromClan.Heroes.ToList();
            if (heroes.IndexOf(hero) >= 0)
            {
                while (heroes.IndexOf(hero) >= 0)
                    heroes.Remove(hero);

                FieldInfo infoHeroes = AccessTools.Field(typeof(Clan), "<Lords>k__BackingField");
                if (infoHeroes == null)
                    throw new Exception("<Heroes>k__BackingField not found");
                infoHeroes.SetValue(fromClan, new MBReadOnlyList<Hero>(heroes));
#if TRACEPATCH
                Print(String.Format("Patch Clan heroes of clan {0}", fromClan.Name.ToString()), PRINT_PATCH);
#endif
            }
            if (canPatchLeader && fromClan.Leader == hero)
            {
                FieldInfo infoLeader = AccessTools.Field(typeof(Clan), "_leader");
                if (infoLeader == null)
                    throw new Exception("_leader not found");
                Print(String.Format("Patch Clan Leader of clan {0} set Leader = null", fromClan.Name.ToString()), PRINT_PATCH);
#if TRACEPATCH
                infoLeader.SetValue(fromClan, null);
#endif
            }
        }

        public static void SwapClan(Hero hero, Clan? fromClan, Clan toClan)
        {
            hero.Clan = null;
            if (hero.CharacterObject.Occupation != Occupation.Lord)
            {
                OccupationToLord(hero.CharacterObject);
            }
            hero.Clan = toClan;
#if V1640MORE
            if (toClan != null)
            {
                if (toClan.Lords.FirstOrDefault(x => x == hero) == null)
                {
                    toClan.Lords.AddItem(hero);
#if TRACEWEDDING
                    Helper.Print(String.Format("SwapClan:: Add {0} to Noble of clan {1}", hero.Name, toClan.Name), Helper.PRINT_TRACE_WEDDING);
#elif TRACECREATECLAN
                    Helper.Print(String.Format("SwapClan:: Add {0} to Noble of clan {1}", hero.Name, toClan.Name), Helper.PRINT_TRACE_CREATE_CLAN);
#endif

                }
                if (fromClan != null
                    && (fromClan.Lords.IndexOf(hero) >= 0
                        || fromClan.Heroes.IndexOf(hero) >= 0))
                    RemoveFromClan(hero, fromClan);
            }
#endif
            if (toClan != hero.Clan)
                Helper.Print(String.Format("SwapClan:: FAIL for Hero {0} to Clan {1}"
                                    , hero.Name
                                    , (toClan != null ? toClan.Name : "NULL")), Helper.PRINT_PATCH);


        }

        //public static addChildrens(Hero fromHero, Hero toHero)
        //{

        //}

        public static void FamilyJoinClan(Hero hero, Clan fromClan, Clan toClan)
        {
            if (hero.Clan == fromClan)
                SwapClan(hero, fromClan, toClan);

            //foreach (Hero child in fromClan.Lords)
            for (int i = 0; i < fromClan.Lords.Count; i++)
            {
                Hero child = fromClan.Lords[i];
                if (child.Father == hero || child.Mother == hero)
                {
#if TRACECREATECLAN
                    Helper.Print(String.Format("FamilyJoinClan for child {0}", child.Name), Helper.PRINT_TRACE_CREATE_CLAN);
#endif
                    FamilyJoinClan(child, fromClan, toClan);
                    i--;
                }
            }
            if (hero.Spouse != null && hero.Spouse.Clan == fromClan)
            {
#if TRACECREATECLAN
                Helper.Print(String.Format("FamilyJoinClan for spouse {0}", hero.Spouse.Name), Helper.PRINT_TRACE_CREATE_CLAN);
#endif
                FamilyJoinClan(hero.Spouse, fromClan, toClan);
            }
        }

        // Adoption system
        public static void FamilyAdoptChild(Hero hero, Hero toHero, Clan fromClan)
        {
            bool opositeSex = hero.IsFemale != toHero.IsFemale;
            foreach (Hero child in fromClan.Lords)
            {
                if (child.Mother == hero && hero.IsFemale)
                {
                    if (child.Father == null 
                        || (child.Father != null && (child.Father == hero || !child.Father.IsAlive)))
                    {
                        Helper.Print(String.Format("Hero {0} adopt a child {1} like father", toHero.Name, child.Name), PRINT_TRACE_WEDDING);
                        child.Father = toHero;
                    }
                }
                else if (child.Father == hero && !hero.IsFemale)
                {
                    if (child.Mother == null
                        || (child.Mother != null && (child.Mother == hero || !child.Mother.IsAlive)))
                    {
                        Helper.Print(String.Format("Hero {0} adopt a child {1} like mother", toHero.Name, child.Name), PRINT_TRACE_WEDDING);
                        child.Mother = toHero;
                    }
                }
                else if (child.Mother == hero && (child.Father == null || (child.Father != null && (child.Father == hero || child.Father.IsDead))))
                {
                    if (opositeSex)
                    {
                        child.Father = hero;
                        child.Mother = toHero;
                    }
                    else
                        child.Father = toHero;
                }
                else if (child.Father == hero && (child.Mother == null || (child.Mother != null && (child.Mother == hero || child.Mother.IsDead))))
                {
                    if (opositeSex)
                    {
                        child.Mother = hero;
                        child.Father = toHero;
                    }
                    else
                        child.Mother = toHero;
                }
            }
        }

        public static bool PatchHeroPlayerClan(Hero hero, bool canBeOtherClan = false, bool etSpouseMainHero = false)
        {

            bool ret = false;

            if ((!canBeOtherClan && hero.Clan != Clan.PlayerClan)
                || (canBeOtherClan && hero.Clan == null)
                || (!canBeOtherClan && Clan.PlayerClan != null && Clan.PlayerClan.Lords.IndexOf(hero) < 0)) // Else lost the town govenor post on hero.Clan = null !!
            {
                hero.Clan = null;
                if (hero.CharacterObject.Occupation != Occupation.Lord)
                {
                    OccupationToLord(hero.CharacterObject);
                }
                hero.Clan = Clan.PlayerClan;

#if V1640MORE
                if (Hero.MainHero.Clan.Lords.FirstOrDefault(x => x == hero) == null)
                {
                    Hero.MainHero.Clan.Lords.AddItem(hero);
                    Helper.Print("Add hero to Noble of the clan", PrintHow.PrintToLogAndWriteAndDisplay);
                }
#endif
                ret = true;
            }

            if (etSpouseMainHero && hero.Spouse == null)
            {
                hero.Spouse = Hero.MainHero;
            }

            if (ret)
            {
#if TRACELOAD
                Print(String.Format("Patch Hero {0} with PlayerClan {1} => {2}\r\n\t{3}", hero.Name.ToString()
                                , Clan.PlayerClan.Name.ToString()
                                , hero.Clan.Name.ToString()
                                , Helper.TraceHero(hero)), PrintHow.PrintToLogAndWriteAndDisplay);
#else
                Print(String.Format("Patch Hero {0} with PlayerClan {1} => {2}", hero.Name.ToString()
                                , Clan.PlayerClan.Name.ToString()
                                , hero.Clan.Name.ToString()), PrintHow.PrintForceDisplay);
#endif
            }


            return ret;
        }

        public static int TraitCompatibility(Hero hero1, Hero hero2, TraitObject trait)
        {
            int traitLevel = hero1.GetTraitLevel(trait);
            int traitLevel2 = hero2.GetTraitLevel(trait);
            if (traitLevel == 0 || traitLevel2 == 0)
                return 0;
            if (traitLevel > 0 && traitLevel2 > 0)
            {
                return traitLevel >= traitLevel2 ? traitLevel2 : traitLevel;
            }
            if (traitLevel < 0 && traitLevel2 < 0)
            {
                return traitLevel >= traitLevel2 ? -traitLevel : -traitLevel2;
            }
            if (traitLevel > 0)
            {
                return -traitLevel + traitLevel2;
            }
            else
                return traitLevel - traitLevel2;
        }

        public static bool CheatEnabled(Hero hero, Hero mainHero)
        {
            return (MASettings.Cheating
                    //&& hero.CharacterObject.Occupation == Occupation.Lord
                    && hero.IsAlive 
                    && !hero.IsTemplate
                    && (MASettings.Notable || (!MASettings.Notable && !hero.IsNotable))
                    && (MASettings.RelationLevelMinForRomance == -1
                        || hero.GetRelation(mainHero) >= MASettings.RelationLevelMinForCheating));
        }

        public static bool IsSuitableForMarriagePathMA(Hero maidenOrSuitor)
        {
#if V4
            if (!maidenOrSuitor.IsAlive || maidenOrSuitor.IsTemplate || (!MASettings.Notable && maidenOrSuitor.IsNotable))
                return false;
#else
            if (!maidenOrSuitor.IsAlive || maidenOrSuitor.IsNotable || maidenOrSuitor.IsTemplate)
            {
                return false;
            }
#endif
                return true;
        }

        public static bool FactionAtWar(Hero hero, Hero otherHero)
        {
            IFaction? factionHero = null;
            IFaction? factionOtherHero = null;

            if (hero.Clan != null)
                factionHero = hero.Clan.MapFaction;

            if (otherHero.Clan != null)
                factionOtherHero = otherHero.Clan.MapFaction;

            if (factionHero != null && factionOtherHero != null)
                return factionHero.IsAtWarWith(factionOtherHero);

            return false;
        }

#if V4
        public static bool HeroOccupiedAndCantMarried(Hero hero)
        {
            if (hero.CharacterObject.Occupation != Occupation.Lord 
                && Campaign.Current != null)
            {
                IssueBase issueBase;
                Campaign.Current.IssueManager.Issues.TryGetValue(hero, out issueBase);
                if (issueBase != null)
                    //&& (issueBase.IsSolvingWithAlternative 
                    //|| issueBase.IsSolvingWithQuest 
                    //|| issueBase.IssueQuest != null)
                    //|| issueBase. )
                    return true;
                return false;
            }
            else
                return false;
        }
#endif

        public static bool MarryEnabledPathMA(Hero hero, Hero mainHero, bool testRelationLevel = true)
        {
#if V4
            return ((hero.CharacterObject.Occupation != Occupation.Lord
//                        || (hero.CharacterObject.Occupation == Occupation.Lord && hero.Spouse != null && hero.Spouse.IsDead)
                     )
                    && hero.IsAlive 
                    && (MASettings.Notable || (!MASettings.Notable && !hero.IsNotable))
                    && (!testRelationLevel 
                        || (testRelationLevel 
                            && (MASettings.RelationLevelMinForRomance == -1
                                || hero.GetRelation(mainHero) >= MASettings.RelationLevelMinForRomance))));
#elif V2
            return Hero.OneToOneConversationHero.IsWanderer || Hero.OneToOneConversationHero.IsPlayerCompanion;
#else
            return Hero.OneToOneConversationHero.IsWanderer && Hero.OneToOneConversationHero.IsPlayerCompanion;
#endif
        }

        public static List<Hero> ListClanLord(Hero hero)
        {
            List<Hero> ret = new List<Hero>();
            ret.Add(hero);
            if (hero.Clan != null)
            {
                foreach(Hero h in hero.Clan.Lords) 
                {
                    if (h != hero)
                        ret.Add(h);
                }
            }
            return ret;
        }

#if TRACELOAD || TRACEROMANCE || TRACEWEDDING || TRACE
        public static String TraceHero(Hero hero, String? prefix = null)
        {
            String aff = (String.IsNullOrWhiteSpace(prefix) ? "" : (prefix + "::")) + hero.Name + (hero.IsFemale ? "(F)" : "(M)");

            if (!hero.IsAlive)
                aff += ", DEAD";
            else
                aff += String.Format(", Age {0:0}", hero.Age);
            if (hero.IsDead)
                aff += ", REALY DEAD";

            if (!hero.IsActive)
                aff += ", INACTIF";

            aff += ", State " + hero.HeroState;

            if (hero.IsWanderer)
                aff += ", Wanderer";

            if (hero.CharacterObject != null)
                aff += ", Occupation " + hero.CharacterObject.Occupation.ToString();

            if (hero.IsPlayerCompanion)
                aff += ", PLAYER Companion";

            if (hero.IsPrisoner)
                aff += ", PRISONER";

            if (hero.Clan != null)
                aff += ", Clan " + hero.Clan.Name;

            if (hero.MapFaction != null)
                aff += ", MAP Faction " + hero.MapFaction.Name;

            if (hero.Spouse != null)
                aff += ", Spouse " + hero.Spouse.Name;

#if TRACEEXSPOUSE
            if (HeroPatch.HeroExspouses(hero) != null)
                aff += ", ExSpouses (" + String.Join(",", HeroPatch.HeroExspouses(hero).Select<Hero, String>(x => x.Name.ToString()).ToList()) + ")";
#else
            if (hero.ExSpouses != null && hero.ExSpouses.Count > 0)
                aff += ", ExSpouses (" + String.Join(",", hero.ExSpouses.Select<Hero, String>(x => x.Name.ToString()).ToList()) + ")";
#endif
            if (hero.CurrentSettlement != null)
                aff += ", Settlement " + hero.CurrentSettlement.Name;

            if (hero.HomeSettlement != null)
                aff += ", HomeSettlement " + hero.HomeSettlement.Name;

            if (MAEtape >= Etape.EtapeLoadPas2)
            {
                if (hero.PartyBelongedTo != null)
                    aff += ", In Party " + hero.PartyBelongedTo.Name;

                if (hero.IsSpecial)
                    aff += ", IS Special";

                if (hero.IsTemplate)
                    aff += ", IS Tempalte";

                if (hero.IsPreacher)
                    aff += ", IS Preacher";
            }

            aff += ", StringID " + hero.StringId;

            return aff;

        }
#endif

        }
}
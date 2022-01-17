﻿using Newtonsoft.Json;
using System;
using System.IO;
using TaleWorlds.Library;

namespace MarryAnyone.Settings
{
    internal class MASettings : ISettingsProvider
    {


        #region constantes
        private const string CONFIG_FILE = "config.json";
        private const string USER_PATH_FOR_CONFIG = "/Mount and Blade II Bannerlord/Configs/ModSettings/" + Helper.MODULE_NAME;
        private const string GAME_PATH_CONFIG = "Modules/" + Helper.MODULE_NAME + "/" + CONFIG_FILE;

        public const String DIFFICULTY_VERY_EASY = "Very Easy";
        public const String DIFFICULTY_EASY = "Easy";
        #endregion

        public static bool UsingMCM;

        public static bool NoMCMWarning;

        public static bool NoConfigWarning;

        //public static readonly string ConfigPath = BasePath.Name + "Modules/MarryAnyone/config.json";
        public static string ConfigPath
        {
            get
            {

                string configPathUser = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + USER_PATH_FOR_CONFIG;
                string configFileUser = configPathUser + "/" + CONFIG_FILE;
                if (!File.Exists(configFileUser))
                {
                    string configGame = BasePath.Name + GAME_PATH_CONFIG;
                    if (!File.Exists(configGame))
                        throw new Exception(String.Format("File {0} not found !", configGame));

                    Directory.CreateDirectory(configPathUser);

                    File.Copy(configGame, configFileUser);

                    if (!File.Exists(configFileUser))
                        throw new Exception(String.Format("File {0} not found !", configGame));
                }
                return configFileUser;
            }
        }

        public bool Incest { get => _provider.Incest; set => _provider.Incest = value; }
        public bool Polygamy { get => _provider.Polygamy; set => _provider.Polygamy = value; }
        public bool Polyamory { get => _provider.Polyamory; set => _provider.Polyamory = value; }
        public bool Cheating { get => _provider.Cheating; set => _provider.Cheating = value; }
        public bool Notable { get => _provider.Notable; set => _provider.Notable = value; }
        public bool Debug { get => _provider.Debug; set => _provider.Debug = value; }
        public string Difficulty { get => _provider.Difficulty; set => _provider.Difficulty = value; }
        public string SexualOrientation { get => _provider.SexualOrientation; set => _provider.SexualOrientation = value;  }
        public bool Adoption { get => _provider.Adoption; set => _provider.Adoption = value; }
        public float AdoptionChance { get => _provider.AdoptionChance; set => _provider.AdoptionChance = value; }
        public bool AdoptionTitles { get => _provider.AdoptionTitles; set => _provider.AdoptionTitles = value; }
        public bool RetryCourtship { get => _provider.RetryCourtship; set => _provider.RetryCourtship = value; }
        public bool SpouseJoinArena { get => _provider.SpouseJoinArena; set => _provider.SpouseJoinArena = value; }
        public int RelationLevelMinForRomance { get => _provider.RelationLevelMinForRomance; set => _provider.RelationLevelMinForRomance = value; }
        public int RelationLevelMinForCheating { get => _provider.RelationLevelMinForCheating; set => _provider.RelationLevelMinForCheating = value; }
        public int RelationLevelMinForSex { get => _provider.RelationLevelMinForSex; set => _provider.RelationLevelMinForSex = value; }
        public bool ImproveRelation { get => _provider.ImproveRelation; set => _provider.ImproveRelation = value; }
        public bool CanJoinUpperClanThroughMAPath { get => _provider.CanJoinUpperClanThroughMAPath; set => _provider.CanJoinUpperClanThroughMAPath = value; }
        public bool NotifyRelationImprovementWithinFamily { get => _provider.NotifyRelationImprovementWithinFamily; set => _provider.NotifyRelationImprovementWithinFamily = value; }
        public bool DifficultyEasyMode { get => String.Equals(_provider.Difficulty, DIFFICULTY_EASY, StringComparison.OrdinalIgnoreCase);  }
        public bool DifficultyVeryEasyMode { get => String.Equals(_provider.Difficulty, DIFFICULTY_VERY_EASY, StringComparison.OrdinalIgnoreCase); }
        public bool DifficultyNormalMode { get => _provider.Difficulty == null || !_provider.Difficulty.EndsWith("Easy", StringComparison.OrdinalIgnoreCase); }
        public bool Patch { get => _provider.Patch; set => _provider.Patch = value; }

        public MASettings()
        {
            if (MCMSettings.Instance is { } settings)
            {
                _provider = settings;
                NoMCMWarning = NoConfigWarning = false;
                UsingMCM = true;
                return;
            }
            UsingMCM = false;
            MAConfig.Instance = new MAConfig();
            if (File.Exists(ConfigPath))
            {
                try
                {
                    MAConfig config = JsonConvert.DeserializeObject<MAConfig>(File.ReadAllText(ConfigPath));
                    MAConfig.Instance.Polygamy = config.Polygamy;
                    MAConfig.Instance.Polyamory = config.Polyamory;
                    MAConfig.Instance.Incest = config.Incest;
                    MAConfig.Instance.Cheating = config.Cheating;
                    MAConfig.Instance.Notable = config.Notable;
                    MAConfig.Instance.Debug = config.Debug;
                    MAConfig.Instance.SpouseJoinArena = config.SpouseJoinArena;
                    MAConfig.Instance.Warning = config.Warning;
                    MAConfig.Instance.Difficulty = config.Difficulty;
                    MAConfig.Instance.SexualOrientation = config.SexualOrientation;
                    MAConfig.Instance.Adoption = config.Adoption;
                    MAConfig.Instance.AdoptionChance = config.AdoptionChance;
                    MAConfig.Instance.AdoptionTitles = config.AdoptionTitles;
                    MAConfig.Instance.RetryCourtship = config.RetryCourtship;
                    MAConfig.Instance.RelationLevelMinForCheating = config.RelationLevelMinForCheating;
                    MAConfig.Instance.RelationLevelMinForRomance = config.RelationLevelMinForRomance;
                    MAConfig.Instance.RelationLevelMinForSex = config.RelationLevelMinForSex;
                    MAConfig.Instance.ImproveRelation = config.ImproveRelation;
                    MAConfig.Instance.NotifyRelationImprovementWithinFamily = config.NotifyRelationImprovementWithinFamily;
                    MAConfig.Instance.CanJoinUpperClanThroughMAPath = config.CanJoinUpperClanThroughMAPath;
                    MAConfig.Instance.Patch = config.Patch;
                    NoMCMWarning = true;
                    NoConfigWarning = false;
                }
                catch (Exception exception)
                {
                    Helper.Error(exception);
                }
            }
            else
            {
                NoConfigWarning = true;
                NoMCMWarning = false;
            }
            _provider = MAConfig.Instance;
        }

        private readonly ISettingsProvider _provider;
    }
}
﻿using HarmonyLib;
using MarryAnyone.Behaviors;
using MarryAnyone.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;
using TaleWorlds.MountAndBlade;
using System.IO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using static MarryAnyone.MAHelper;

namespace MarryAnyone
{
    public class MASubModule : MBSubModuleBase // NoHarmonyLoader
    {

        public static string ModuleFolderName { get; } = "MarryAnyone";

        public static readonly Harmony Harmony = new Harmony(ModuleFolderName);

        CampaignGameStarter? _campaignGameStarter;
        //private bool bPatchOnTick = false;

        //public override void NoHarmonyInit()
        //{
        //    Logging = false;
        //    LogFile = "MANoHarmony.txt";
        //    LogDateFormat = "MM/dd/yy HH:mm:ss.fff";
        //}

        //public override void NoHarmonyLoad()
        //{
        //    ReplaceModel<MADefaultMarriageModel, DefaultMarriageModel>();
        //    ReplaceModel<MARomanceModel, DefaultRomanceModel>();
        //}


        internal static MASubModule? Instance;

        public CampaignGameStarter GameStarter()
        {
            if (_campaignGameStarter == null)
                throw new Exception("CampaignGameStarter not referenced");

            return _campaignGameStarter;
        }


        public MASubModule() 
        {
            Instance = this;
        }


        protected override void OnSubModuleLoad()
        {
            var dirpath = System.IO.Path.Combine(TaleWorlds.Engine.Utilities.GetLocalOutputPath(), ModuleFolderName);
            try
            {
                if (!Directory.Exists(dirpath))
                {
                    Directory.CreateDirectory(dirpath);
                }
                MAHelper.Print("Output directory : " + dirpath, MAHelper.PrintHow.PrintForceDisplay);
            }
            catch
            {
                MAHelper.Print("Failed to create config directory.  Please manually create this directory: " + dirpath, MAHelper.PrintHow.PrintForceDisplay);
            }

            MAHelper.LogPath = dirpath;
            base.OnSubModuleLoad();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (game.GameType is Campaign)
            {

                MAHelper.Print("Campaign", MAHelper.PrintHow.PrintForceDisplay);

                CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarter;
                campaignGameStarter.LoadGameTexts(BasePath.Name + "Modules/MarryAnyone/ModuleData/ma_module_strings.xml");

                _campaignGameStarter = campaignGameStarter;

                AddBehaviors(campaignGameStarter);

                //gameStarter.AddModel(new MADefaultMarriageModel());
                //gameStarter.AddModel(new MARomanceModel());

                //MarriageModel oldMarriageModel = campaignGameStarter.Models.OfType<MarriageModel>().FirstOrDefault();
                //if (oldMarriageModel != null)
                //    campaignGameStarter.Models[oldMarriageModel] = new MADefaultMarriageModel();
                //else
                //    campaignGameStarter.Models.AddItem(new MADefaultMarriageModel());

                //RomanceModel romanceModel = campaignGameStarter.Models.OfType<RomanceModel>().FirstOrDefault();
                //if (romanceModel != null)
                //    campaignGameStarter.Models.[romanceModel] = new MADefaultMarriageModel();
                //else
                //    campaignGameStarter.Models.AddItem(new MARomanceModel());


            }
        }


        public override void OnGameLoaded(Game game, object initializerObject)
        {
            base.OnGameLoaded(game, initializerObject);

            MAHelper.MAEtape = Etape.EtapeLoad;
//#if TRACELOAD
//            MAHelper.Print(String.Format("Chemin output : '{0}'", MAHelper.LogPath), MAHelper.PrintHow.PrintForceDisplay);

//            if (Hero.MainHero.Spouse != null)
//                MAHelper.Print(String.Format("Main Spouse {0}", MAHelper.TraceHero(Hero.MainHero.Spouse)), MAHelper.PRINT_TRACE_LOAD);

//            foreach (Hero hero in Hero.MainHero.ExSpouses)
//                MAHelper.Print(String.Format("Other spouse {0}", MAHelper.TraceHero(hero)), MAHelper.PRINT_TRACE_LOAD);

//            foreach (Hero hero in Hero.MainHero.CompanionsInParty)
//                MAHelper.Print(String.Format("Companion in party {0}", MAHelper.TraceHero(hero)), MAHelper.PRINT_TRACE_LOAD);

//            if (Hero.MainHero.Clan != null)
//                foreach (Hero hero in Hero.MainHero.Clan.Heroes)
//                    MAHelper.Print(String.Format("Companion in clan {0}", MAHelper.TraceHero(hero)), MAHelper.PRINT_TRACE_LOAD);
            
//            MAHelper.Print("List spouse and Companions END", MAHelper.PrintHow.PrintToLogAndWrite);
//#endif

            if (Hero.MainHero.Spouse != null && Hero.MainHero.Spouse.HeroState == Hero.CharacterStates.Disabled)
            {
                Hero.MainHero.Spouse.ChangeState(Hero.CharacterStates.Active);
                MAHelper.Print(string.Format("Active {0}", Hero.MainHero.Spouse.Name), MAHelper.PRINT_PATCH);
            }
            foreach (Hero hero in Hero.MainHero.ExSpouses)
            {
                if (hero.HeroState == Hero.CharacterStates.Disabled && hero.IsAlive)
                {
                    hero.ChangeState(Hero.CharacterStates.Active);
                    MAHelper.Print(string.Format("Active {0}", hero.Name), MAHelper.PRINT_PATCH);

                }
            }

            // Parent patch
            bool hadSpouse = Hero.MainHero.Spouse != null;
            bool mainHeroIsFemale = Hero.MainHero.IsFemale;

            foreach (Hero hero in Hero.MainHero.Children)
            {
                if (hadSpouse && hero.Father == Hero.MainHero && hero.Mother == Hero.MainHero)
                {
                    if (mainHeroIsFemale)
                        hero.Father = Hero.MainHero.Spouse;
                    else
                        hero.Mother = Hero.MainHero.Father;
                    MAHelper.Print(string.Format("Patch Parent of {0}", hero.Name), MAHelper.PRINT_PATCH);
                }
                if (hero.Father == null)
                {
                    hero.Father = mainHeroIsFemale && hadSpouse ? Hero.MainHero.Spouse : Hero.MainHero;
                    MAHelper.Print(string.Format("Patch Father of {0}", hero.Name), MAHelper.PRINT_PATCH);
                }
                if (hero.Mother == null)
                {
                    hero.Mother = !mainHeroIsFemale && hadSpouse ? Hero.MainHero.Spouse : Hero.MainHero;
                    MAHelper.Print(string.Format("Patch Mother of {0}", hero.Name), MAHelper.PRINT_PATCH);
                }
            }
        }

        public override void OnGameEnd(Game game)
        {

            if (MARomanceCampaignBehavior.Instance != null)
                MARomanceCampaignBehavior.Instance.Dispose();

            Instance = null;
            _campaignGameStarter = null;

            base.OnGameEnd(game);
            MAHelper.LogClose();
        }


        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);

            if (game.GameType is Campaign)
            {
                Harmony.PatchAll();
            }
        }

        private void AddBehaviors(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddBehavior(new MAPatchBehavior());
            campaignGameStarter.AddBehavior(new MAPerSaveCampaignBehavior());
            campaignGameStarter.AddBehavior(new MARomanceCampaignBehavior());
            campaignGameStarter.AddBehavior(new MAAdoptionCampaignBehavior());
        }
    }
}
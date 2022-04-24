using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
#if V1720MORE
    using TaleWorlds.CampaignSystem.Settlements;
#endif

namespace MarryAnyone.Behaviors
{
    class MAPatchBehavior : CampaignBehaviorBase
    {

        private int _maxWanderer = 0;
        private Random _random;

        private delegate bool FiltreOk(Hero hero);

        private bool FiltreBanditClan(Hero hero)
        {
            return hero.Clan != null && hero.Clan.IsBanditFaction;
        }

        private Hero? ResolveHero(List<Hero> heroes, Settlement settlement, FiltreOk? filtreOk)
        {
            Hero? hero = null;
            int age;
            bool female;
            bool otherCulture;

            int nbCharacter = heroes.Count;
            age = (((_maxWanderer - nbCharacter) % 10) * 10) + 18;
            female = ((_maxWanderer - nbCharacter) % 2) == 1;
            otherCulture = ((_maxWanderer - nbCharacter) % 4) == 3;
            hero = heroes.Where(
                        x =>
                           (x.Age >= age && x.Age <= age + 10)
                        && x.IsFemale == female
                        && ((x.Culture == settlement.Culture && !otherCulture)
                            || (x.Culture != settlement.Culture && otherCulture))
                        && !x.IsFriend(Hero.MainHero)).Random(_random);

            if (hero == null && filtreOk != null)
            {
                hero = heroes.Where(
                                                                x =>
                                                                filtreOk(x)
                                                                && (x.Age >= age && x.Age <= age + 10)
                                                                && x.IsFemale == female
                                                                && !x.IsFriend(Hero.MainHero)).Random(_random);
            }
            if (hero == null && filtreOk != null)
            {
                hero = heroes.Where(
                                                                x =>
                                                                filtreOk(x)
                                                                && x.IsFemale == female
                                                                && !x.IsFriend(Hero.MainHero)).Random(_random);
            }
            if (hero == null && filtreOk != null)
            {
                hero = heroes.Where(
                                                                x =>
                                                                filtreOk(x)
                                                                && !x.IsFriend(Hero.MainHero)).Random(_random);
            }

            if (hero == null)
            {
                hero = heroes.Where(
                                                                x =>
                                                                    x.Clan == null
                                                                && (x.Age >= age && x.Age <= age + 10)
                                                                && x.IsFemale == female
                                                                && !x.IsFriend(Hero.MainHero)).Random(_random);
            }
            if (hero == null)
            {
                hero = heroes.Where(
                                                                x =>
                                                                    x.Clan == null
                                                                && x.IsFemale == female
                                                                && !x.IsFriend(Hero.MainHero)).Random(_random);
            }
            if (hero == null)
            {
                hero = heroes.Where(
                                                                x =>
                                                                    x.Clan == null
                                                                && !x.IsFriend(Hero.MainHero)).Random(_random);
            }

            return hero;

        }

        //private Hero ResolveHero(List<LocationCharacter> locationCharacters, Settlement settlement)
        //{
        //    Hero hero = null;
        //    int age;
        //    bool female;
        //    bool otherCulture;

        //    int nbCharacter = locationCharacters.Count;
        //    age = (((_maxWanderer - nbCharacter) % 10) * 10) + 18;
        //    female = ((_maxWanderer - nbCharacter) % 2) == 1;
        //    otherCulture = ((_maxWanderer - nbCharacter) % 4) == 3;
        //    hero = locationCharacters.Where(
        //                x =>
        //                x.Character != null
        //                && x.Character.HeroObject != null
        //                && (x.Character.Age >= age && x.Character.Age <= age + 10)
        //                && x.Character.IsFemale == female
        //                && ((x.Character.Culture == settlement.Culture && !otherCulture)
        //                    || (x.Character.Culture != settlement.Culture && otherCulture))
        //                && !x.Character.HeroObject.IsFriend(Hero.MainHero)).Random(_random).Character.HeroObject;

        //    if (hero == null)
        //    {
        //        hero = locationCharacters.Where(
        //                                                        x =>
        //                                                        x.Character != null
        //                                                        && x.Character.HeroObject != null
        //                                                        && x.Character.HeroObject.Clan == null
        //                                                        && (x.Character.Age >= age && x.Character.Age <= age + 10)
        //                                                        && x.Character.IsFemale == female
        //                                                        && !x.Character.HeroObject.IsFriend(Hero.MainHero)).Random(_random).Character.HeroObject;
        //    }
        //    if (hero == null)
        //    {
        //        hero = locationCharacters.Where(
        //                                                        x =>
        //                                                        x.Character != null
        //                                                        && x.Character.HeroObject != null
        //                                                        && x.Character.HeroObject.Clan == null
        //                                                        && x.Character.IsFemale == female
        //                                                        && !x.Character.HeroObject.IsFriend(Hero.MainHero)).Random(_random).Character.HeroObject;
        //    }
        //    if (hero == null)
        //    {
        //        hero = locationCharacters.Where(
        //                                                        x =>
        //                                                        x.Character != null
        //                                                        && x.Character.HeroObject != null
        //                                                        && x.Character.HeroObject.Clan == null
        //                                                        && !x.Character.HeroObject.IsFriend(Hero.MainHero)).Random(_random).Character.HeroObject;
        //    }

        //    return hero;

        //}

        private void OnSessionLaunched(CampaignGameStarter cgs)
        {

            Helper.MASettingsClean();
            Helper.MAEtape = Helper.Etape.EtapeLoadPas2;

            // Kingdom patch
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (!kingdom.IsEliminated && kingdom.Leader != null && kingdom.Leader.Clan.Kingdom != kingdom)
                {

                    Helper.Print(String.Format("PATCH Kingdom will destroy the kingdom {0}", kingdom.Name), Helper.PRINT_PATCH | Helper.PrintHow.PrintForceDisplay);
                    foreach(Clan clan in kingdom.Clans)
                    {
                        Helper.Print(String.Format("with the clan {0}", clan.Name), Helper.PRINT_PATCH);
                    }
                    DestroyKingdomAction.Apply(kingdom);
                    kingdom.MainHeroCrimeRating = 0;
                    //kingdom.RulingClan = null;
                }
            }

#if PATCHTOOMUCHWANDERER
            _maxWanderer = Helper.MASettings.PatchMaxWanderer;
            Helper.Print(String.Format("PatchMaxWanderer Start maxWanderer ?= {0}", _maxWanderer), Helper.PrintHow.PrintToLogAndWriteAndInit);
            if (_maxWanderer > 0) 
            {
                _random = new Random();
                foreach (Kingdom kingdom in Kingdom.All)
                {
                    foreach (Settlement settlement in kingdom.Settlements.Where(x => x.IsTown))
                    {
                        int nbCharacter = 0;
                        nbCharacter = settlement.HeroesWithoutParty.Count;
                        
                        Helper.Print(String.Format("PatchMaxWanderer {0} nbHeroWithoutParty ?= {1}", settlement.Name, nbCharacter), Helper.PrintHow.PrintToLogAndWriteAndInit);

                        if (nbCharacter > _maxWanderer)
                        {
                            List<Hero> heroes = settlement.HeroesWithoutParty.ToList();
                            while (nbCharacter > _maxWanderer)
                            {
                                Hero hero = ResolveHero(heroes, settlement, FiltreBanditClan);
                                if (hero != null)
                                {
                                    heroes.Remove(hero);
#if TRACEPATCH
                                    Helper.Print(String.Format("PatchMaxWanderer Remove hero {0} female {1} Age {2} Culture {3} Clan {4} from settlement {5}", hero.Name
                                                    , hero.IsFemale
                                                    , hero.Age
                                                    , hero.Culture
                                                    , hero.Clan == null ? "NULL" : hero.Clan.Name
                                                    , settlement.Name), Helper.PrintHow.PrintToLogAndWriteAndInit);
#endif
                                    KillCharacterAction.ApplyByRemove(hero, false, true);
                                    nbCharacter--;
                                }
                                else
                                {
#if TRACEPATCH
                                    Helper.Print(String.Format("PatchMaxWanderer No more hero for removed {0}/{1} from settlement {2}", nbCharacter, _maxWanderer, settlement.Name), Helper.PrintHow.PrintToLogAndWriteAndInit);
                                    foreach (Hero heroRestant in heroes)
                                    {
                                        Helper.Print(String.Format("PatchMaxWanderer keep {0}", Helper.TraceHero(heroRestant)), Helper.PrintHow.PrintToLogAndWriteAndInit);
                                    }
#endif
                                    break;
                                }
                            }
                        }
                        //List<Location> locations = null;
                        ////List<Location> locations = settlement.LocationComplex.GetListOfLocations().ToList();
                        ////foreach (Location location in locations)
                        ////{
                        ////    Helper.Print(String.Format("PatchMaxWanderer {0}::{1} nbCharacter ?= {2}", settlement.Name, location.Name, location.CharacterCount), Helper.PrintHow.PrintToLogAndWriteAndInit);
                        ////}

                        //List<LocationCharacter> locationCharacters = settlement.LocationComplex.GetListOfCharacters().ToList();
                        //int nbCharacter = locationCharacters.Count();
                        //if (nbCharacter > _maxWanderer)
                        //{

                        //    while (nbCharacter > _maxWanderer)
                        //    {
                        //        Hero hero = ResolveHero(locationCharacters, settlement);
                        //        if (hero != null)
                        //        {
                        //            Helper.Print(String.Format("PatchMaxWanderer Remove hero {0} from settlement {1}", hero.Name, settlement.Name), Helper.PrintHow.PrintToLogAndWriteAndInit);
                        //            settlement.LocationComplex.RemoveCharacterIfExists(hero);
                        //            KillCharacterAction.ApplyByRemove(hero, false, true);
                        //            nbCharacter--;
                        //        }
                        //        else
                        //        {
                        //            Helper.Print(String.Format("PatchMaxWanderer No more hero for removed {0}/{1} from settlement {2}", nbCharacter, _maxWanderer, settlement.Name), Helper.PrintHow.PrintToLogAndWriteAndInit);
                        //            break;
                        //        }
                        //    }
                        //}

                        ////locations = settlement.LocationComplex.FindAll(x => String.Equals(x, "tavern", StringComparison.OrdinalIgnoreCase)).ToList();
                        ////if (locations == null || locations.Count == 0)
                        ////    Helper.Print(String.Format("PatchMaxWanderer no Tavern at {0}", settlement.Name), Helper.PrintHow.PrintToLogAndWriteAndInit);

                        ////foreach (Location location in locations)
                        ////{
                        ////    nbCharacter = location.CharacterCount;
                        ////    Helper.Print(String.Format("PatchMaxWanderer {2}::{0} countain {1} characters", location.Name, nbCharacter, settlement.Name), Helper.PrintHow.PrintToLogAndWriteAndInit);

                        ////    if (nbCharacter > _maxWanderer) 
                        ////    {

                        ////        locationCharacters = location.GetCharacterList().ToList();

                        ////        while (nbCharacter > _maxWanderer)
                        ////        {
                        ////            Hero hero = ResolveHero(locationCharacters, settlement);
                        ////            if (hero != null)
                        ////            {
                        ////                Helper.Print(String.Format("PatchMaxWanderer Remove hero {0} from tavern at {1}", hero.Name, settlement.Name), Helper.PrintHow.PrintToLogAndWriteAndInit);
                        ////                location.RemoveCharacter(hero);
                        ////                KillCharacterAction.ApplyByRemove(hero, false, true);
                        ////                nbCharacter--;
                        ////            }
                        ////            else
                        ////            {
                        ////                Helper.Print(String.Format("PatchMaxWanderer No more hero for removed {0}/{1} from tavern at {2}", nbCharacter, _maxWanderer, settlement.Name), Helper.PrintHow.PrintToLogAndWriteAndInit);
                        ////                break;
                        ////            }
                        ////        }
                        ////    }
                        ////}
                    }
                }
            }
#endif
                                }


        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));
        }

        public override void SyncData(IDataStore dataStore)
        {
            // RAS
        }
    }
    public static class EnumerableHelper
    {
        public static T? Random<T>(this IEnumerable<T> input, Random random)
        {
            if (input == null || input.Count() == 0)
                return default;

            return input.ElementAt(random.Next(input.Count()));
        }
    }
}

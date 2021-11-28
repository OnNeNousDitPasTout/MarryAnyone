using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

namespace MarryAnyone.Patches
{
#if PATCHEENCYCLOPEDIA
    [HarmonyPatch(typeof(EncyclopediaHeroPageVM))]
    static class EncyclopediaHeroPageVM_allRelatedHeroesPatch
    {

        private static List<Hero>? _heroes = null;
        //private static PlayerToggleTrackSettlementFromEncyclopediaEvent
        private static FieldInfo? _accesHero = null;
        private static Hero? _hero = null;

        static public void Dispose()
        {
            _hero = null;
            _heroes = null;
            _accesHero = null;
        }

        [HarmonyPatch(typeof(EncyclopediaHeroPageVM))]
        [HarmonyPatch("_allRelatedHeroes", MethodType.Getter)]
        [HarmonyPrefix]
        //internal static bool _allRelatedHeroesPatch(EncyclopediaHeroPageVM __instance , ref IEnumerable<Hero> __result)
        //[HarmonyTranspiler]
        internal static bool _allRelatedHeroesTranspiler(EncyclopediaHeroPageVM __instance, ref IEnumerable<Hero> __result)
        {

            if (_accesHero == null)
            {
                _accesHero = AccessTools.Field(typeof(EncyclopediaHeroPageVM), "_hero");
                if (_accesHero == null)
                    throw new Exception("Field _hero inaccessible on EncyclopediaHeroPageVM");
            }

            Hero heroBase = (Hero)_accesHero.GetValue(__instance);
            if (_hero != heroBase || _heroes == null)
            {
                int nbPatch = 0;
                _hero = heroBase;
                _heroes = new List<Hero>();

                _heroes.Add(heroBase.Father);
                _heroes.Add(heroBase.Mother);
                _heroes.Add(heroBase.Spouse);

                foreach (Hero hero in _hero.Children)
                {
                    _heroes.Add(hero);
                }
                foreach (Hero hero in _hero.Siblings)
                {
                    if (_heroes.IndexOf(hero) < 0) // && (_hero.ExSpouses == null || _hero.ExSpouses.IndexOf(hero) < 0))
                        _heroes.Add(hero);
                    else
                        nbPatch++;
                }
                foreach (Hero hero in _hero.ExSpouses)
                {
                    if (_heroes.IndexOf(hero) < 0)
                        _heroes.Add(hero);
                    else
                        nbPatch++;
                }

                //List<String> l = _heroes.Select<Hero, String>(x => x.Name.ToString()).ToList();
                //MAHelper.Print(String.Format("The List {0}", String.Join(", ", l)), MAHelper.PrintHow.PrintToLogAndWrite);
                Helper.Print(String.Format("_allRelatedHeroesPatch Work on Hero {0} nb Patch applies : {1}", (heroBase == null ? "NULL" : heroBase.Name), nbPatch), Helper.PrintHow.PrintToLogAndWrite);
            }

            __result = _heroes;
            return false;

            //__result = MonoNativeFunctionWrapperAttribute 
            //yield return _hero.Father;
            //yield return _hero.Mother;
            //yield return _hero.Spouse;
            //foreach (Hero hero in _hero.Children)
            //{
            //    yield return hero;
            //}
            //foreach (Hero hero in _hero.Siblings)
            //{
            //    yield return hero;
            //}
            //foreach (Hero hero in _hero.ExSpouses)
            //{
            //    yield return hero;
            //}
            //yield break;
        }
    }
#endif
}

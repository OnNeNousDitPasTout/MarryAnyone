using MarryAnyone.Behaviors;
using MarryAnyone.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace MarryAnyone.MA
{
    internal class MATeam : IDisposable
    {
        protected Team _team;
        internal List<Tuple<Hero, Agent>> _heroes = new List<Tuple<Hero, Agent>>();

        public bool withMainHero = false;
        public bool withHeroOfPlayerTeam = false;
        int _resolu = -1;

        public MATeam(Team team)
        {
            _team = team;
            foreach (Agent agent in team.TeamAgents.Where(x => x.IsHero))
            {
                Hero? hero = Hero.FindFirst(x => x.StringId == agent.Character.StringId);
                if (hero!= null)
                {
                    _heroes.Add(new Tuple<Hero, Agent>(hero, agent));
                    if (hero == Hero.MainHero)
                        withMainHero = true;

                    if (MARomanceCampaignBehavior.Instance.IsPlayerTeam(hero))
                        withHeroOfPlayerTeam = true;
                }
            }
#if TRACEBATTLERELATION
            Helper.Print(String.Format("Create MATeam {0} nb Heroes {1}/nbAgent {2}"
                            , team.ToString()
                            , _heroes.Count()
                            , team.TeamAgents.Count), Helper.PrintHow.PrintToLogAndWrite);
            if (team.TeamAgents.Count == 0)
            {
                Helper.Print(String.Format("Team {0}", HelperReflection.Properties(team, " - ", BindingFlags.Instance)), Helper.PrintHow.PrintToLogAndWrite);
            }
#endif

        }

        public Hero? CurrentHero()
        {
            if (_resolu >= 0)
                return _heroes[_resolu].Item1;
            return null;
        }

        public int Resolve(String stringID)
        {
            _resolu = _heroes.FindIndex(x => x.Item1.StringId == stringID);
            return _resolu;
        }

        public override string ToString()
        {
            return _team != null ? String.Format("MATeam leader {0} Attacker {1}"
                                    , _team.Leader != null ? _team.Leader.Name : "NULL"
                                    , _team.IsAttacker) 
                                : "NULL";
        }

        public void Dispose()
        {
#if TRACEBATTLERELATION
            Helper.Print(String.Format("Dispose MATeam {0}", _team.ToString()), Helper.PrintHow.PrintToLogAndWrite);
#endif
            _team.Clear();
            _heroes.Clear();
        }
    }
}

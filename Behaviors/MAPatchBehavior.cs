using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace MarryAnyone.Behaviors
{
    class MAPatchBehavior : CampaignBehaviorBase
    {

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
}

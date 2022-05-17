using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
#if V1720MORE
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
#endif

namespace MarryAnyone.Behaviors
{
#if MAINTERACTION

    internal class MAInteractionBehavior : CampaignBehaviorBase
    {
        public MAInteractionBehavior()
        {
        }

#region vie de l'objet
        public override void RegisterEvents()
        {
            CampaignEvents.LocationCharactersSimulatedEvent.AddNonSerializedListener(this, OnLocationCharactersSimulated);
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, OnLocationCharactersAreReadyToSpawn);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        private void OnSettlementEntered(MobileParty arg1, Settlement arg2, Hero arg3)
        {
            throw new NotImplementedException();
        }

        private void OnLocationCharactersAreReadyToSpawn(Dictionary<string, int> obj)
        {
            throw new NotImplementedException();
        }

#endregion

        private void OnLocationCharactersSimulated()
        {
            throw new NotImplementedException();
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
#endif
}

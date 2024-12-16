using LevelGeneration;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers.Markers
{
    public class LG_PowerGeneratorMarker : ItemMarkerBase
    {
        public override void SetupNavMarker(Component comp)
        {
            m_gene = comp.Cast<LG_PowerGenerator_Core>();
            m_gene.add_OnSyncStatusChanged((Action<ePowerGeneratorStatus>)OnSyncStateChange);

            m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(comp.gameObject);
            m_markerColor = new Color(1f, 0.647f, 0f);
            m_markerVisibleUpdateMode = ItemMarkerVisibleUpdateModeType.Manual;
            m_markerStyle = eNavMarkerStyle.PlayerPingGenerator;
            m_terminalItem = m_gene.m_terminalItem.Cast<LG_GenericTerminalItem>();
            m_markerTitle = m_terminalItem.TerminalItemKey;

            base.SetupNavMarker(comp);
        }

        private void OnSyncStateChange(ePowerGeneratorStatus status)
        {
            if (status == ePowerGeneratorStatus.Powered)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
            }
            else if (status == ePowerGeneratorStatus.UnPowered)
            {
                AttemptInteract(eNavMarkerInteractionType.Show);
            }
        }

        public override void OnManualUpdate()
        {
            if (!IsDiscovered)
                return;

            if (m_gene.m_graphics.m_gfxSlot.active)
                AttemptInteract(eNavMarkerInteractionType.Show);
            else
                AttemptInteract(eNavMarkerInteractionType.Hide);
        }

        private LG_PowerGenerator_Core m_gene;
    }
}

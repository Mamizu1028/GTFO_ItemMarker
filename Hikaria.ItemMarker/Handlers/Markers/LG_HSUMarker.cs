using LevelGeneration;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers.Markers
{
    public class LG_HSUMarker : ItemMarkerBase
    {
        public override void SetupNavMarker(Component comp)
        {
            m_hsu = comp.Cast<LG_HSU>();
            m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(comp.gameObject);
            m_markerColor = new Color(0.467f, 0.098f, 1f);
            m_markerVisibleUpdateMode = ItemMarkerVisibleUpdateModeType.Zone;
            m_markerStyle = eNavMarkerStyle.PlayerPingHSU;
            m_terminalItem = m_hsu.m_terminalItem.Cast<LG_GenericTerminalItem>();
            m_markerTitle = m_terminalItem.TerminalItemKey;

            base.SetupNavMarker(comp);
        }

        public override void OnManualUpdate()
        {
            if (!IsDiscovered)
                return;

            if (m_hsu.m_pickupSampleInteraction.IsActive)
                AttemptInteract(eNavMarkerInteractionType.Show);
            else
                AttemptInteract(eNavMarkerInteractionType.Hide);
        }

        private LG_HSU m_hsu;
    }
}

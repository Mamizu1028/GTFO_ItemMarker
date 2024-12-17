using LevelGeneration;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers.Markers
{
    public class LG_HSUActivator_Marker : ItemMarkerBase
    {
        public override void SetupNavMarker(Component comp)
        {
            m_hsuActivator = comp.Cast<LG_HSUActivator_Core>();
            m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(comp.gameObject);
            m_markerColor = new Color(0.467f, 0.098f, 1f);
            m_markerVisibleUpdateMode = ItemMarkerVisibleUpdateModeType.Manual;
            m_markerStyle = eNavMarkerStyle.PlayerPingGenerator;
            m_terminalItem = m_hsuActivator.m_terminalItem.Cast<LG_GenericTerminalItem>();
            m_markerTitle = m_terminalItem.TerminalItemKey;

            base.SetupNavMarker(comp);
        }

        protected override void OnManualUpdate()
        {
            if (m_hsuActivator.m_insertHSUInteraction.Cast<LG_GenericCarryItemInteractionTarget>().isActiveAndEnabled)
                AttemptInteract(eNavMarkerInteractionType.Show);
            else
                AttemptInteract(eNavMarkerInteractionType.Hide);
        }

        protected override void OnDevUpdate()
        {
            OnManualUpdate();
        }

        private LG_HSUActivator_Core m_hsuActivator;
    }
}

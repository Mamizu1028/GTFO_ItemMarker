using LevelGeneration;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers.Markers
{
    public class LG_ComputerTerminalMarker : ItemMarkerBase
    {
        public override void SetupNavMarker(Component comp)
        {
            m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(comp.gameObject);
            m_markerColor = Color.green;
            m_markerVisibleUpdateMode = ItemMarkerVisibleUpdateModeType.Zone;
            m_terminal = comp.Cast<LG_ComputerTerminal>();
            m_terminalItem = m_terminal.m_terminalItem.Cast<LG_GenericTerminalItem>();
            m_markerTitle = m_terminalItem.TerminalItemKey;

            foreach (var collider in comp.GetComponentsInChildren<Collider>(true))
            {
                if (collider.GetComponent<PlayerPingTarget>() == null)
                    collider.gameObject.AddComponent<PlayerPingTarget>().m_pingTargetStyle = eNavMarkerStyle.PlayerPingTerminal;
            }

            base.SetupNavMarker(comp);
        }

        private LG_ComputerTerminal m_terminal;
    }
}

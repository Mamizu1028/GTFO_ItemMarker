using LevelGeneration;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers.Markers
{
    public class LG_ComputerTerminal_Marker : ItemMarkerBase
    {
        public override void SetupNavMarker(Component comp)
        {
            m_marker = GuiManager.NavMarkerLayer.PrepareGenericMarker(comp.gameObject);
            m_markerColor = Color.green;
            m_markerVisibleUpdateMode = ItemMarkerVisibleUpdateModeType.Zone;
            m_terminal = comp.Cast<LG_ComputerTerminal>();
            m_terminalItem = m_terminal.m_terminalItem.Cast<LG_GenericTerminalItem>();
            m_markerTitle = m_terminalItem.TerminalItemKey;
            m_markerStyle = eNavMarkerStyle.PlayerPingTerminal;
            m_markerAlwaysShowTitle = true;
            m_markerAlwaysShowDistance = true;

            foreach (var collider in comp.GetComponentsInChildren<Collider>(true))
            {
                if (collider.GetComponent<PlayerPingTarget>() == null)
                    collider.gameObject.AddComponent<PlayerPingTarget>().m_pingTargetStyle = eNavMarkerStyle.PlayerPingTerminal;
            }

            base.SetupNavMarker(comp);
        }

        protected override void OnDevUpdate()
        {
            var zone = LocalPlayerAgent?.CourseNode?.m_zone;
            if (zone == null)
            {
                AttemptInteract(eNavMarkerInteractionType.Hide);
                return;
            }
            if (CourseNode.m_zone.ID == zone.ID)
                AttemptInteract(eNavMarkerInteractionType.Show);
            else
                AttemptInteract(eNavMarkerInteractionType.Hide);
        }

        private LG_ComputerTerminal m_terminal;
    }
}

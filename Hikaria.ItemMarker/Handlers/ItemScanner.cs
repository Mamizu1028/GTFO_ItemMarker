using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers
{
    public class ItemScanner : MonoBehaviour
    {
        private bool m_isLocal;
        private PlayerAgent m_playerAgent;
        private Il2CppStructArray<RaycastHit> m_hits = new(new RaycastHit[30]);
        private int s_collCount;

        private void Awake()
        {
            m_playerAgent = GetComponent<PlayerAgent>();
            m_isLocal = m_playerAgent.IsLocallyOwned;
        }

        private void LateUpdate()
        {
            if (m_isLocal)
            {
                s_collCount = Physics.SphereCastNonAlloc(m_playerAgent.FPSCamera.Position, 3f, m_playerAgent.FPSCamera.CameraRayDir, m_hits, 0f, LayerManager.MASK_PLAYER_INTERACT_SPHERE);
            }
            else
            {
                s_collCount = Physics.SphereCastNonAlloc(m_playerAgent.EyePosition, 2.5f, m_playerAgent.transform.forward, m_hits, 0.5f, LayerManager.MASK_PLAYER_INTERACT_SPHERE);
            }

            if (s_collCount == 0)
                return;
            for (int i = 0; i < s_collCount; i++)
            {
                var marker = m_hits[i].collider.GetComponentInParent<ItemNavMarkerWrapper>();
                if (marker != null && marker.AllowDiscoverCheck && !marker.IsDiscovered)
                {
                    marker.IsDiscovered = true;
                }
            }
        }
    }
}

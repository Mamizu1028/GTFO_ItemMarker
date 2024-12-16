using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace Hikaria.ItemMarker.Handlers
{
    public class ItemMarkerTag : MonoBehaviour
    {
        [HideFromIl2Cpp]
        public ItemMarkerBase ItemMarker { get; private set; }

        [HideFromIl2Cpp]
        public void Setup(ItemMarkerBase itemMarker)
        {
            ItemMarker = itemMarker;
        }
    }
}

using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;

namespace Hikaria.ResourceMarker
{
    [ArchiveDependency(Core.PluginInfo.GUID)]
    [ArchiveModule(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class EntryPoint : IArchiveModule
    {
        public bool ApplyHarmonyPatches => false;

        public bool UsesLegacyPatches => false;

        public ArchiveLegacyPatcher Patcher { get; set; }

        public string ModuleGroup => FeatureGroups.GetOrCreateModuleGroup("Resource Marker", new()
        {
            { Language.English, "Resource Marker" },
            { Language.Chinese, "资源标记" }
        });

        public void Init()
        {
            Logs.LogMessage("OK");
        }

        public void OnExit()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
        }
    }
}

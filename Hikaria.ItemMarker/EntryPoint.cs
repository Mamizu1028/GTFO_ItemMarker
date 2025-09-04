using Hikaria.Core;
using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using TheArchive.Interfaces;

namespace Hikaria.ItemMarker
{
    [ArchiveDependency(CoreGlobal.GUID)]
    [ArchiveDependency(DropItem.PluginInfo.GUID)]
    [ArchiveModule(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class EntryPoint : IArchiveModule
    {
        public string ModuleGroup => FeatureGroups.GetOrCreateModuleGroup(PluginInfo.GUID);
        public ILocalizationService LocalizationService { get; set; }
        public IArchiveLogger Logger { get; set; }

        public void Init()
        {
            Logs.Setup(Logger);
        }
    }
}

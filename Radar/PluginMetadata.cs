using System.Reflection;

[assembly: AssemblyVersion(Radar.PluginMetadata.AssemblyVersion)]
[assembly: AssemblyFileVersion(Radar.PluginMetadata.AssemblyVersion)]
[assembly: AssemblyInformationalVersion(Radar.PluginMetadata.Version)]

namespace Radar
{
    /// <summary>Single source for the plugin identity and version.</summary>
    internal static class PluginMetadata
    {
        public const string Guid = "com.leonana69.radar";
        public const string Name = "Leonana69-Radar";

        // Change only this value when releasing a new plugin version.
        public const string Version = "1.3.4";
        public const string AssemblyVersion = Version + ".0";
    }
}

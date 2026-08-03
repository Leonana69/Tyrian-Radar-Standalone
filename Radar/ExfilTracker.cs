using EFT;
using EFT.Interactive;
using System.Collections.Generic;

namespace Radar
{
    /// <summary>
    /// Pins the extracts available to this player, plus every transit, to the rim of the radar.
    /// </summary>
    internal sealed class ExfilTracker
    {
        private readonly Player _player;
        private readonly List<BlipOther> _blips = new List<BlipOther>();

        public ExfilTracker(Player player)
        {
            _player = player;
        }

        public void Rebuild()
        {
            Clear();

            if (!RadarConfig.ExfilEnabled.Value)
                return;

            foreach (ExfiltrationPoint point in LocationScene.GetAllObjects<ExfiltrationPoint>())
            {
                // Extracts are side and spawn specific; only show the ones this player can actually use.
                if (!point.InfiltrationMatch(_player))
                    continue;

                RadarPlugin.Log.LogDebug($"Exfil {point.Settings.Name} ({point.Status}) at {point.transform.position}");
                _blips.Add(new BlipOther(point.Id, point.transform, false, BlipKind.Exfil));
            }

            foreach (TransitPoint transit in LocationScene.GetAllObjects<TransitPoint>())
            {
                RadarPlugin.Log.LogDebug($"Transit {transit.name} (enabled: {transit.Enabled}) at {transit.transform.position}");
                _blips.Add(new BlipOther(transit.name, transit.transform, false, BlipKind.Transit));
            }
        }

        public void Clear()
        {
            foreach (BlipOther blip in _blips)
                blip.DestroyBlip();

            _blips.Clear();
        }

        public void Render()
        {
            foreach (BlipOther blip in _blips)
                blip.Update();
        }
    }
}

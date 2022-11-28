using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmSettings
{
    public const int DefaultMatchFoundWaitSeconds = 20;

    public string MatchFoundSoundFile { get; set; }

    public List<string> AllowedLadders { get; set; } = new();

    public int MatchFoundWaitSeconds { get; set; } = DefaultMatchFoundWaitSeconds;

    public IDictionary<string, Texture2D> HeaderLogos = new Dictionary<string, Texture2D>();

    public Texture2D GetLadderHeaderLogo(string ladder)
        => !HeaderLogos.ContainsKey(ladder) ? null : HeaderLogos[ladder];
}
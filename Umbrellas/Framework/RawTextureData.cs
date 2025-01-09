using Microsoft.Xna.Framework;

namespace StardewMods.Umbrellas.Framework;

internal class RawTextureData : IRawTextureData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Color[] Data { get; set; }

    internal RawTextureData(Color[] data, int width, int height)
    {
        this.Width = width;
        this.Height = height;
        this.Data = data;
    }
}

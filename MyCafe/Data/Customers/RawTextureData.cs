using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace MyCafe.Data.Customers;
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

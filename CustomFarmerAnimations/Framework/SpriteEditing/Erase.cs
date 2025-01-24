using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.CustomFarmerAnimations.Framework.SpriteEditing;

public class Erase : EditOperation
{
    public Rectangle Target { get; set; }
    public Erase(Rectangle target)
    {
        this.Target = target;
    }

    public override void Execute(IAssetDataForImage imageData)
    {
        Color[] transparentData = ArrayPool<Color>.Shared.Rent(this.Target.Width * this.Target.Height);
        try
        {
            RawTextureData sourceData = new RawTextureData(transparentData, this.Target.Width, this.Target.Height);
            for (int i = 0; i < sourceData.Data.Length; i++)
            {
                sourceData.Data[i] = Color.Transparent;
            }
            imageData.PatchImage(sourceData, null, this.Target, PatchMode.Replace);
        }
        finally
        {
            ArrayPool<Color>.Shared.Return(transparentData);
        }

    }

    internal static Erase? Parse(string[] op)
    {
        Rectangle? rect = ParseRectangle(op[1]);

        if (rect == null)
        {
            return null;
        }

        return new Erase(rect.Value);
    }
}

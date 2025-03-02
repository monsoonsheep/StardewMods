using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.CustomFarmerAnimations.Framework.Data;

namespace StardewMods.CustomFarmerAnimations.Framework.SpriteEditing.Patching;

public class ImagePatch
{
    public Source Source { get; init; } = null!;

    public Rectangle Target { get; init; } = Rectangle.Empty;

    public bool Overlay { get; init; } = false;

    internal void Execute(IAssetDataForImage imageData)
    {
        Texture2D sourceTex = Game1.content.Load<Texture2D>(this.Source.Texture);

        Rectangle target = this.Target;

        if (target.Width == 0 && target.Height == 0)
        {
            target.Width = this.Source.Region.Width;
            target.Height = this.Source.Region.Height;
        }
        imageData.PatchImage(
            sourceTex,
            this.Source.Region,
            target,
            this.Overlay ? PatchMode.Overlay : PatchMode.Replace
            );
    }
}

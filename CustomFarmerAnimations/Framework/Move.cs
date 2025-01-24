using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.CustomFarmerAnimations.Framework
{
    public class Move : EditOperation
    {
        public Rectangle Source { get; set; }

        public Rectangle Target { get; set; }

        public Move(Rectangle source, Rectangle target)
        {
            this.Source = source;
            this.Target = target;
        }

        public override void Execute(IAssetDataForImage imageData)
        {
            imageData.PatchImage(imageData.Data, this.Source, this.Target, PatchMode.Replace);

            Color[] transparentData = ArrayPool<Color>.Shared.Rent(this.Target.Width * this.Target.Height);
            try
            {
                RawTextureData sourceData = new RawTextureData(transparentData, this.Source.Width, this.Source.Height);
                for (int i = 0; i < sourceData.Data.Length; i++)
                {
                    sourceData.Data[i] = Color.Transparent;
                }
                imageData.PatchImage(sourceData, null, this.Source, PatchMode.Replace);
            }
            finally
            {
                ArrayPool<Color>.Shared.Return(transparentData);
            }
        }

        internal static Move? Parse(string[] op)
        {
            Rectangle? source = ParseRectangle(op[1]);
            Rectangle? target = ParseRectangle(op[2]);

            if (source == null || target == null)
            {
                return null;
            }
            if (target.Value.Width == -1)
            {
                target = new Rectangle(target.Value.X, target.Value.Y, source.Value.Width, source.Value.Height);
            }
            return new Move(source.Value, target.Value);
        }
    }

}

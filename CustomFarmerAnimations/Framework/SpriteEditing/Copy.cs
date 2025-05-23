namespace StardewMods.CustomFarmerAnimations.Framework.SpriteEditing;

public class Copy : EditOperation
{
    public Rectangle Source { get; set; }

    public Rectangle Target { get; set; }

    public Copy(Rectangle source, Rectangle target)
    {
        this.Source = source;
        this.Target = target;
    }

    public override void Execute(IAssetDataForImage imageData)
    {
        imageData.PatchImage(imageData.Data, this.Source, this.Target, PatchMode.Replace);
    }

    internal static Copy? Parse(string[] op)
    {
        Rectangle? source = ModUtility.ParseRectangle(op[1]);
        Rectangle? target = ModUtility.ParseRectangle(op[2]);

        if (source == null || target == null)
        {
            return null;
        }

        if (target.Value.Width == -1)
        {
            target = new Rectangle(target.Value.X, target.Value.Y, source.Value.Width, source.Value.Height);
        }
        return new Copy(source.Value, target.Value);
    }
}

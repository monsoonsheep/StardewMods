using System;
using System.Buffers;


namespace StardewMods.CustomFarmerAnimations.Framework.SpriteEditing;

public abstract class EditOperation
{
    public abstract void Execute(IAssetDataForImage imageData);

    internal static EditOperation? ParseOperation(string operation)
    {
        string[] split = operation.Split(' ');        

        EditOperation? parsed = split[0] switch
        {
            "move" => Move.Parse(split),
            "copy" => Copy.Parse(split),
            "erase" => Erase.Parse(split),
            _ => null
        };

        if (parsed == null)
            throw new InvalidOperationException($"Edit Operation {operation} could not be parsed for model");

        return parsed;
    }
}

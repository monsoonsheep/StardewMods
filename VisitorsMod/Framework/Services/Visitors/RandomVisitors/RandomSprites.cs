using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.VisitorsMod.Framework.Data;
using StardewMods.VisitorsMod.Framework.Models.Appearances;

namespace StardewMods.VisitorsMod.Framework.Services.Visitors.RandomVisitors;

internal sealed class RandomSprites : Service
{
    private readonly IModContentHelper modContent;
    private readonly NetState netState;
    private readonly ContentPacks contentPacks;
    private readonly Colors colors;

    public RandomSprites(
        ContentPacks contentPacks,
        NetState netState,
        Colors colors,
        IModEvents events,
        IModContentHelper modContent,
        ILogger logger,
        IManifest manifest)
        : base(logger, manifest)
    {
        this.modContent = modContent;

        this.contentPacks = contentPacks;
        this.netState = netState;
        this.colors = colors;

        events.Content.AssetRequested += this.OnAssetRequested;
    }

    internal void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        // Random generated sprite (with a GUID after the initial asset name)
        if (e.NameWithoutLocale.StartsWith(Values.GENERATED_SPRITE_PREFIX))
        {
            if (this.netState.GeneratedSprites.TryGetValue(e.NameWithoutLocale.Name[(Values.GENERATED_SPRITE_PREFIX.Length + 1)..], out GeneratedSpriteData data))
            {
                e.LoadFrom(() => data.Sprite.Value, AssetLoadPriority.Medium);
            }
            else
            {
                this.Log.Error($"Couldn't find generate sprite data for guid {e.NameWithoutLocale.Name}");
                // Either provide premade error texture or just load null and let the NPC.draw method handle it TODO test the error
                //e.LoadFrom(() => Game1.content.Load<Texture2D>(FarmAnimal.ErrorTextureName), AssetLoadPriority.Medium);
                e.LoadFrom(() => null!, AssetLoadPriority.Medium);
            }
        }
    }

    
    /// <summary>
    /// Create a randomized <see cref="GeneratedSpriteData"/> containing info for appearance parts, to be added to a netsynced list
    /// </summary>
    /// <returns></returns>
    internal GeneratedSpriteData GenerateRandomSpriteData()
    {
        GeneratedSpriteData sprite = new();

        string guid = Guid.NewGuid().ToString();
        sprite.Guid = guid;

        Gender gender = ModUtility.GetRandomGender();
        sprite.Gender = gender;

        int[] skin = this.colors.SkinTones.PickRandom()!;
        Color skinTone = new Color(skin[0], skin[1], skin[2]);
        sprite.SkinTone.Set(skinTone);

        int[] eyes = this.colors.EyeColors.PickRandom()!;
        Color eyeColor = new Color(eyes[0], eyes[1], eyes[2]);
        sprite.EyeColor.Set(eyeColor);

        // Either shirts + pants or outfit
        if (Game1.random.Next(2) >= 0)
        {
            sprite.SetAppearance<ShirtModel>(
                this.GetRandomAppearance<ShirtModel>(gender).Id,
                this.colors.ShirtColors.PickRandom()!.GetRandomPermutation());

            sprite.SetAppearance<PantsModel>(
                this.GetRandomAppearance<PantsModel>(gender).Id,
                this.colors.PantsColors.PickRandom()!.GetRandomPermutation());
        }
        else
        {
            sprite.SetAppearance<OutfitModel>(
                this.GetRandomAppearance<OutfitModel>(gender).Id,
                this.colors.OutfitColors.PickRandom()!.GetRandomPermutation());
        }

        // Hair
        sprite.SetAppearance<HairModel>(
            this.GetRandomAppearance<HairModel>(gender).Id,
            this.colors.HairColors.PickRandom()!.GetRandomPermutation());

        return sprite;
    }

    /// <summary>
    /// This is called by both the host and clients. Composites the appearance model textures available in the <see cref="GeneratedSpriteData"/> object (synced by net fields) into a <see cref="Texture2D"/>
    /// </summary>
    internal Texture2D GenerateTexture(GeneratedSpriteData sprite)
    {
        IRawTextureData body = this.colors.PaintBody(sprite.SkinTone.Value);
        IRawTextureData eyes = this.colors.PaintEyes(sprite.EyeColor.Value);

        IRawTextureData? hair = this.GetTextureDataForPart<HairModel>(sprite);

        IRawTextureData? shirt = this.GetTextureDataForPart<ShirtModel>(sprite);
        IRawTextureData? pants = this.GetTextureDataForPart<PantsModel>(sprite);

        IRawTextureData? outfit = this.GetTextureDataForPart<OutfitModel>(sprite);

        IRawTextureData? shoes = this.GetTextureDataForPart<ShoesModel>(sprite);
        IRawTextureData? accessory = this.GetTextureDataForPart<AccessoryModel>(sprite);

        List<Color[]> layers = [body.Data, eyes.Data];

        if (shoes != null)
            layers.Add(shoes.Data);

        if (pants != null)
            layers.Add(pants.Data);

        if (shirt != null)
            layers.Add(shirt.Data);

        if (outfit != null)
            layers.Add(outfit.Data);

        if (hair != null)
            layers.Add(hair.Data);

        if (accessory != null)
            layers.Add(accessory.Data);

        Color[] finalData = CompositeTextures(layers);

        Texture2D finalTexture = new Texture2D(Game1.graphics.GraphicsDevice, body.Width, body.Height);
        finalTexture.SetData(finalData);
        return finalTexture;
    }

    
    /// <summary>
    /// Returns the texture data set in this object for the given type of appearance model. For example, Calling with HairModel will return the hair texture for this object
    /// </summary>
    /// <typeparam name="TAppearance">The type of model inheriting from <see cref="AppearanceModel"/></typeparam>
    private IRawTextureData? GetTextureDataForPart<TAppearance>(GeneratedSpriteData sprite) where TAppearance : AppearanceModel
    {
        string id = sprite.GetModelId<TAppearance>().Value;
        AppearanceModel? model = this.contentPacks.GetModelCollection<TAppearance>().Values.FirstOrDefault(c => c.Id == id);
        IList<Color>? colors = sprite.GetPaint<TAppearance>();

        if (model == null)
            return null;

        if (colors is { Count: > 0 })
            return model.GetTexture(colors[0]);

        return model.GetRawTexture();
    }

    private TAppearance GetRandomAppearance<TAppearance>(Gender gender = Gender.Undefined) where TAppearance : AppearanceModel
    {
        ICollection<TAppearance> collection = this.contentPacks.GetModelCollection<TAppearance>().Values;
        return collection.Where(m => m.MatchesGender(gender)).MinBy(_ => Game1.random.Next())!;
    }

    /// <summary>
    /// Alpha-blend the given raw texture data arrays over each other in order
    /// </summary>
    internal static Color[] CompositeTextures(List<Color[]> textures)
    {
        Color[] finalData = new Color[textures[0].Length];

        for (int i = 0; i < textures[0].Length; i++)
        {
            Color below = textures[0][i];

            int r = below.R;
            int g = below.G;
            int b = below.B;
            int a = below.A;

            foreach (Color[] layer in textures)
            {
                Color above = layer[i];

                float alphaBelow = 1 - above.A / 255f;

                r = (int)(above.R + r * alphaBelow);
                g = (int)(above.G + g * alphaBelow);
                b = (int)(above.B + b * alphaBelow);
                a = Math.Max(a, above.A);
            }

            finalData[i] = new Color(r, g, b, a);
        }

        return finalData;
    }
}

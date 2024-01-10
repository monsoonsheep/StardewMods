using CharGen.Patching;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace CharGen;
public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance;

    internal new static IMonitor Monitor;
    internal static IModHelper ModHelper;
    internal new static IManifest ModManifest;

    private static Texture2D bodyTex;

    private static Texture2D hairTex;
    private static Texture2D shirtTex;
    private static Texture2D pantsTex;

    private static Texture2D characterSprite;

    

    public override void Entry(IModHelper helper)
    {
        Instance = this;

        Monitor = base.Monitor;
        ModHelper = helper;
        ModManifest = base.ModManifest;
        Log.Monitor = Monitor;

        // Harmony patches
        try
        {
            var harmony = new Harmony(ModManifest.UniqueID);
            new List<PatchCollection>()
            {
                new ExamplePatches(),
            }.ForEach(l => l.ApplyAll(harmony));
        }
        catch (Exception e)
        {
            Log.Error($"Couldn't patch methods - {e}");
            return;
        }

        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
#if DEBUG
        helper.Events.Input.ButtonPressed += Debug.ButtonPress;
#endif
    }

    private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
    {
        if (characterSprite != null)
            e.SpriteBatch.Draw(characterSprite, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, new Vector2(4, 4), SpriteEffects.None, 0.5f);
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        bodyTex = ModHelper.ModContent.Load<Texture2D>("assets/base_male.png");
        hairTex = ModHelper.ModContent.Load<Texture2D>("assets/hair1.png");
        pantsTex = ModHelper.ModContent.Load<Texture2D>("assets/jeans1.png");
        shirtTex = ModHelper.ModContent.Load<Texture2D>("assets/shirt1.png");
    }

    private Texture2D GetCharacterSpriteFromParts(Texture2D body, Texture2D hair, Texture2D pants, Texture2D shirt)
    {
        Texture2D sprite = new Texture2D(Game1.graphics.GraphicsDevice, 64, 160);

        RenderTarget2D target = new RenderTarget2D(Game1.graphics.GraphicsDevice, 64, 160);
        SpriteBatch sb = new SpriteBatch(Game1.graphics.GraphicsDevice);

        RenderTargetBinding[] orig = new RenderTargetBinding[] { };
        Game1.graphics.GraphicsDevice.GetRenderTargets(orig);
        Game1.graphics.GraphicsDevice.SetRenderTarget(target);
        sb.Begin();
        sb.Draw(body, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.1f);
        sb.Draw(hair, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.2f);
        sb.Draw(pants, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.3f);
        sb.Draw(shirt, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.4f);
        sb.End();
        Game1.graphics.GraphicsDevice.SetRenderTargets(orig);

        Color[] data = new Color[64 * 160];
        target.GetData(data, 0, 64*160);
        sprite.SetData(data);
        
        return sprite;
    }

    private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {

    }
}

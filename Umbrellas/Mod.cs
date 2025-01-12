global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using HarmonyLib;
global using Microsoft.Xna.Framework;
global using StardewModdingAPI;
global using StardewModdingAPI.Events;
global using StardewValley;
using Microsoft.Xna.Framework.Graphics;
using StardewMods.Umbrellas.Framework;


namespace StardewMods.Umbrellas;
public class Mod : StardewModdingAPI.Mod
{
    internal static Mod Instance = null!;

    internal Dictionary<string, UmbrellaModel> Umbrellas = [];
    internal HashSet<string> CurrentUmbrellaHolders = [];

    public Mod()
        => Instance = this;

    internal static Harmony Harmony { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        this.Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        this.Helper.Events.World.NpcListChanged += this.OnNpcListChanged;
        this.Helper.Events.Player.Warped += this.OnPlayerWarped;
        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
        this.Helper.Events.Content.AssetReady += this.OnAssetReady;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Harmony = new Harmony(this.ModManifest.UniqueID);
        Harmony.Patch(
            original: AccessTools.Method(typeof(NPC), nameof(NPC.draw), [typeof(SpriteBatch), typeof(float)]),
            postfix: new HarmonyMethod(AccessTools.Method(this.GetType(), nameof(After_NpcDraw)))
        );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.Umbrellas = Game1.content.Load<Dictionary<string, UmbrellaModel>>("Mods/MonsoonSheep.Umbrellas/Umbrellas");
    }

    private void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
    {
        if (e.Location != Game1.currentLocation)
            return;

        if (e.Location.IsRainingHere() && e.Location.IsOutdoors)
        {
            foreach (NPC npc in e.Added)
            {
                this.NpcRainCheck(npc);
            }
        }

        if (!e.Location.IsRainingHere() || !e.Location.IsOutdoors)
        {
            foreach (NPC npc in e.Added)
            {
                this.NpcRainUncheck(npc);
            }
        }
    }

    private void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
            return;

        if (e.NewLocation.IsRainingHere() && e.NewLocation.IsOutdoors)
        {
            foreach (NPC npc in e.NewLocation.characters)
            {
                this.NpcRainCheck(npc);
            }
        }
        else if (!e.NewLocation.IsRainingHere() || !e.NewLocation.IsOutdoors)
        {
            foreach (NPC npc in e.NewLocation.characters)
            {
                this.NpcRainUncheck(npc);
            }
        }
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/MonsoonSheep.Umbrellas/Umbrellas"))
        {
            e.LoadFrom(() => new Dictionary<string, UmbrellaModel>(), AssetLoadPriority.Low);
        }

        // Edit their spritesheet according to the data given in their UmbrellaModel. (Moving hands etc)
        else if (e.NameWithoutLocale.IsDirectlyUnderPath("Characters"))
        {
            string npcName = e.NameWithoutLocale.BaseName.Split('/')[1];
            if (this.CurrentUmbrellaHolders.Contains(npcName) && this.Umbrellas.TryGetValue(npcName, out var umbrellaData))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsImage();

                    // Deletes
                    foreach (Rectangle deleteArea in umbrellaData.Deletes)
                    {
                        Color[] emptyData = new Color[deleteArea.Width * deleteArea.Height];
                        Array.Fill(emptyData, Color.Transparent);
                        RawTextureData empty = new RawTextureData(emptyData, deleteArea.Width, deleteArea.Height);

                        editor.PatchImage(empty, null, deleteArea, PatchMode.Replace);
                    }

                    // Copies
                    foreach (SpriteEdit edit in umbrellaData.Copies)
                    {
                        foreach (var target in edit.Targets)
                        {
                            Color[] sourceData = new Color[edit.Source.Width * edit.Source.Height];
                            editor.PatchImage(editor.Data, edit.Source, new Rectangle((int) target.X, (int) target.Y, edit.Source.Width, edit.Source.Height), PatchMode.Replace);
                        }
                    }
                });
            }
        }
    }

    private void OnAssetReady(object? sender, AssetReadyEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/MonsoonSheep.Umbrellas/Umbrellas"))
        {
            this.Umbrellas = Game1.content.Load<Dictionary<string, UmbrellaModel>>("Mods/MonsoonSheep.Umbrellas/Umbrellas");
        }
    }

    public static void After_NpcDraw(NPC __instance, SpriteBatch b, float alpha)
    {
        if (!Instance.CurrentUmbrellaHolders.Contains(__instance.Name))
            return;

        Texture2D texture = Game1.content.Load<Texture2D>(Instance.Umbrellas[__instance.Name].Texture);
        Point customOffset = Instance.Umbrellas[__instance.Name].Offset;

        int verticalIndex = (__instance.FacingDirection) switch
        {
            0 => 2,
            1 => 1,
            2 => 0,
            3 => 3,
            _ => 0
        };

        Vector2 offset = Vector2.Zero;

        // Bob up and down with walking
        offset.Y += (__instance.Sprite.CurrentFrame % 2 != 0) ? 4f : 0f;

        // Umbrella positioning
        offset += new Vector2(-6, -7f) * 4f;

        // Custom offset from model
        offset.X += customOffset.X;
        offset.Y += customOffset.Y;


        Vector2 position = __instance.getLocalPosition(Game1.viewport)
            + new Vector2(__instance.GetSpriteWidthForPositioning() * 4 / 2, __instance.GetBoundingBox().Height / 2)
            + offset;

        b.Draw(
            texture,
            position,
            new Rectangle(28, verticalIndex * 32, 28, 32),
            Color.White,
            0f,
            new Vector2(__instance.Sprite.SpriteWidth / 2, __instance.Sprite.SpriteHeight * 3f / 4f),
            4f,
            SpriteEffects.None,
            (__instance.StandingPixel.Y / 10000f) - 0.001f
        );

        b.Draw(
            texture,
            position,
            new Rectangle(0, verticalIndex * 32, 28, 32),
            Color.White,
            0f,
            new Vector2(__instance.Sprite.SpriteWidth / 2, __instance.Sprite.SpriteHeight * 3f / 4f),
            4f,
            SpriteEffects.None,
            (__instance.StandingPixel.Y / 10000f) + 0.002f
        );
    }

    internal void NpcRainCheck(NPC npc)
    {
        // if not in current holders list and we have an umbrella model for them, add them to holders
        if (!this.CurrentUmbrellaHolders.Contains(npc.Name) && this.Umbrellas.ContainsKey(npc.Name))
        {
            this.CurrentUmbrellaHolders.Add(npc.Name);
            this.Helper.GameContent.InvalidateCache($"Characters/{npc.Name}");
        }
    }

    internal void NpcRainUncheck(NPC npc)
    {
        if (this.CurrentUmbrellaHolders.Contains(npc.Name))
        {
            this.CurrentUmbrellaHolders.Remove(npc.Name);
            this.Helper.GameContent.InvalidateCache($"Characters/{npc.Name}");
        }
    }
}

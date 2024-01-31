using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace MyCafe.CharGen;
internal class CharacterGenerator
{
    private Texture2D bodyTex;
    private Texture2D hairTex;
    private Texture2D shirtTex;
    private Texture2D pantsTex;
    private Texture2D characterSprite;

    internal void LoadTextures(IModContentHelper modContent)
    {
        this.bodyTex = modContent.Load<Texture2D>("assets/base_male.png");
        this.hairTex = modContent.Load<Texture2D>("assets/hair1.png");
        this.pantsTex = modContent.Load<Texture2D>("assets/jeans1.png");
        this.shirtTex = modContent.Load<Texture2D>("assets/shirt1.png");
    }

    private Texture2D GetCharacterSpriteFromParts(Texture2D body, Texture2D hair, Texture2D pants, Texture2D shirt)
    {
        Texture2D sprite = new Texture2D(Game1.graphics.GraphicsDevice, 64, 160);
        RenderTarget2D target = new RenderTarget2D(Game1.graphics.GraphicsDevice, 64, 160);
        SpriteBatch sb = new SpriteBatch(Game1.graphics.GraphicsDevice);

        RenderTargetBinding[] orig = [];
        Game1.graphics.GraphicsDevice.GetRenderTargets(orig);
        Game1.graphics.GraphicsDevice.SetRenderTarget(target);
        sb.Begin(SpriteSortMode.Immediate);
        sb.Draw(body, Vector2.Zero, Color.White);
        sb.Draw(hair, Vector2.Zero, Color.White);
        sb.Draw(pants, Vector2.Zero, Color.White);
        sb.Draw(shirt, Vector2.Zero, Color.White);
        sb.End();
        Game1.graphics.GraphicsDevice.SetRenderTargets(orig);
        Color[] data = new Color[64 * 160];
        target.GetData(data, 0, 64 * 160);
        sprite.SetData(data);

        sb.Dispose();
        target.Dispose();
        return sprite;
    }

}

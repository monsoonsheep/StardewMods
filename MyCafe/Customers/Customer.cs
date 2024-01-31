using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonsoonSheep.Stardew.Common;
using MyCafe.Enums;
using MyCafe.Locations.Objects;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;

namespace MyCafe.Customers;

public class Customer : NPC
{
    private Seat? NetReservedSeat;

    internal Seat? ReservedSeat
    {
        get => this.NetReservedSeat ??= this.Group.ReservedTable?.Seats.FirstOrDefault(s => s.ReservingCustomer == this);
        set => this.NetReservedSeat = value;
    }

    internal NetRef<Item?> ItemToOrder = new NetRef<Item?>(null);
    internal NetBool DrawName = new NetBool(false);
    internal NetBool DrawItemOrder = new NetBool(false);

    internal bool IsSittingDown;
    internal CustomerGroup Group = null!;

    private Vector2 LerpStartPosition;
    private Vector2 LerpEndPosition;
    private float LerpPosition = -1f;
    private float LerpDuration = -1f;

    public Customer()
    {
    }

    public Customer(string name, Vector2 position, string location, AnimatedSprite sprite, Texture2D portrait) : base(sprite, position, location, 2, name, portrait, eventActor: true)
    {
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        this.NetFields.AddField(this.ItemToOrder).AddField(this.DrawName).AddField(this.DrawItemOrder);
    }

    public override void update(GameTime gameTime, GameLocation location)
    {
        base.update(gameTime, location);

        if (!Context.IsMainPlayer)
            return;

        if (this.controller != null && !this.freezeMotion && !this.currentLocation.farmers.Any() && this.currentLocation.Name.Equals("BusStop"))
        {
            while (this.currentLocation.Name.Equals("BusStop") && this.controller.pathToEndPoint?.Count > 2)
            {
                this.controller.pathToEndPoint.Pop();
                this.controller.handleWarps(new Rectangle(this.controller.pathToEndPoint.Peek().X * 64, this.controller.pathToEndPoint.Peek().Y * 64, 64, 64));
                this.Position = new Vector2(this.controller.pathToEndPoint.Peek().X * 64, this.controller.pathToEndPoint.Peek().Y * 64 + 16);
            }
        }

        this.speed = 4;

        if (this.LerpPosition >= 0f)
        {
            this.LerpPosition += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (this.LerpPosition >= this.LerpDuration)
            {
                this.LerpPosition = this.LerpDuration;
            }

            this.Position = new Vector2(StardewValley.Utility.Lerp(this.LerpStartPosition.X, this.LerpEndPosition.X, this.LerpPosition / this.LerpDuration), StardewValley.Utility.Lerp(this.LerpStartPosition.Y, this.LerpEndPosition.Y, this.LerpPosition / this.LerpDuration));
            if (this.LerpPosition >= this.LerpDuration)
            {
                this.LerpPosition = -1f;
            }
        }
    }

    public override void draw(SpriteBatch b, float alpha = 1)
    {
        float mainLayerDepth = Math.Max(0f, this.StandingPixel.Y / 10000f);

        Vector2 localPosition = this.getLocalPosition(Game1.viewport);

        b.Draw(this.Sprite.Texture,
            localPosition + new Vector2((float)this.GetSpriteWidthForPositioning() * 4 / 2, (float)this.GetBoundingBox().Height / 2) + (this.shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), this.Sprite.SourceRect,
            Color.White * alpha, this.rotation,
            new Vector2((float)this.Sprite.SpriteWidth / 2, (float)this.Sprite.SpriteHeight * 3f / 4f),
            Math.Max(0.2f, this.Scale) * 4f,
            (this.flip || (this.Sprite.CurrentAnimation != null && this.Sprite.CurrentAnimation[this.Sprite.currentAnimationIndex].flip)) ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            mainLayerDepth
            );

        if (this.DrawName.Value)
        {
            Vector2 p = localPosition - new Vector2(40, 64);
            b.DrawString(
                Game1.dialogueFont,
                this.displayName,
                p,
                Color.White * 0.75f,
                0f,
                Vector2.Zero,
                new Vector2(0.3f, 0.3f),
                SpriteEffects.None,
                mainLayerDepth + 0.001f
                );
        }

        if (this.DrawItemOrder.Value && this.ItemToOrder.Value != null)
        {
            Vector2 offset = new Vector2(0,
                (float) Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

            Vector2 pos = this.getLocalPosition(Game1.viewport);
            pos.Y -= 32 + this.Sprite.SpriteHeight * 3;

            b.Draw(
                Mod.Sprites,
                pos + offset,
                new Rectangle(0, 16, 16, 16),
                Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None,
                0.99f);

            this.ItemToOrder.Value.drawInMenu(b, pos + offset, 0.35f, 1f, 0.992f);
        }

        base.DrawBreathing(b, alpha);
        base.DrawGlow(b);
        base.DrawEmote(b);
    }

    public override bool tryToReceiveActiveObject(Farmer who, bool probe = false)
    {
        if (!probe)
        {
            Table? table = this.Group.ReservedTable;

            if (table != null && Mod.PlayerInteractWithTable(table, who))
                return true;
        }

        return base.tryToReceiveActiveObject(who, probe);
    }

    internal void SitDown(int direction)
    {
        Vector2 sitPosition = this.Position + CommonHelper.DirectionIntToDirectionVector(direction) * 64f;
        this.LerpMove(this.Position, sitPosition, 0.2f);
    }

    private void LerpMove(Vector2 startPosition, Vector2 endPosition, float duration)
    {
        this.LerpStartPosition = startPosition;
        this.LerpEndPosition = endPosition;
        this.LerpPosition = 0f;
        this.LerpDuration = duration;
    }

    public static PathFindController.endBehavior SitDownBehavior = delegate (Character c, GameLocation loc)
    {
        if (c is Customer customer)
        {
            int direction = CommonHelper.DirectionIntFromVectors(customer.Tile, customer.ReservedSeat!.Position.ToVector2());
            customer.SitDown(direction);
            customer.faceDirection(customer.ReservedSeat.SittingDirection);

            customer.IsSittingDown = true;
            if (customer.Group.Members.Any(other => !other.IsSittingDown))
                return;
            customer.ReservedSeat!.Table!.State.Set(TableState.CustomersThinkingOfOrder);
        }
    };
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using System.Linq;
using MonsoonSheep.Stardew.Common;
using MyCafe.Locations.Objects;

namespace MyCafe.Customers;

public class Customer : NPC
{
    private Seat? _reservedSeat;

    internal Seat? ReservedSeat
    {
        get => _reservedSeat ??= Group.ReservedTable?.Seats.FirstOrDefault(s => s.ReservingCustomer == this);
        set => _reservedSeat = value;
    }

    internal NetRef<Item?> ItemToOrder = new NetRef<Item?>(null);
    internal NetBool DrawName = new NetBool(false);
    internal NetBool DrawItemOrder = new NetBool(false);

    internal bool IsSittingDown;
    internal CustomerGroup Group = null!;
    private Vector2 _lerpStartPosition;
    private Vector2 _lerpEndPosition;
    private float _lerpPosition = -1f;
    private float _lerpDuration = -1f;

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

    public Customer()
    {
    }

    public Customer(string name, Vector2 position, string location, AnimatedSprite sprite, Texture2D portrait) : base(sprite, position, location, 2, name, portrait, eventActor: true)
    {
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields.AddField(ItemToOrder).AddField(DrawName).AddField(DrawItemOrder);
    }

    public override void update(GameTime gameTime, GameLocation location)
    {
        base.update(gameTime, location);

        if (!Context.IsMainPlayer)
            return;

        if (controller != null && !freezeMotion && !currentLocation.farmers.Any() && currentLocation.Name.Equals("BusStop"))
        {
            while (currentLocation.Name.Equals("BusStop") && controller.pathToEndPoint?.Count > 2)
            {
                controller.pathToEndPoint.Pop();
                controller.handleWarps(new Rectangle(controller.pathToEndPoint.Peek().X * 64, controller.pathToEndPoint.Peek().Y * 64, 64, 64));
                base.Position = new Vector2(controller.pathToEndPoint.Peek().X * 64, controller.pathToEndPoint.Peek().Y * 64 + 16);
            }
        }

        speed = 4;

        if (_lerpPosition >= 0f)
        {
            _lerpPosition += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_lerpPosition >= _lerpDuration)
            {
                _lerpPosition = _lerpDuration;
            }
            Position = new Vector2(StardewValley.Utility.Lerp(_lerpStartPosition.X, _lerpEndPosition.X, _lerpPosition / _lerpDuration), StardewValley.Utility.Lerp(_lerpStartPosition.Y, _lerpEndPosition.Y, _lerpPosition / _lerpDuration));
            if (_lerpPosition >= _lerpDuration)
            {
                _lerpPosition = -1f;
            }
        }
    }

    public override void draw(SpriteBatch b, float alpha = 1)
    {
        float mainLayerDepth = Math.Max(0f, base.StandingPixel.Y / 10000f);

        Vector2 localPosition = getLocalPosition(Game1.viewport);

        b.Draw(
            Sprite.Texture, 
            localPosition + new Vector2((float) GetSpriteWidthForPositioning() * 4 / 2, (float) GetBoundingBox().Height / 2) + (shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), 
            Sprite.SourceRect, 
            Color.White * alpha, 
            rotation, 
            new Vector2((float) Sprite.SpriteWidth / 2, (float) Sprite.SpriteHeight * 3f / 4f), 
            Math.Max(0.2f, scale.Value) * 4f, 
            (flip || (Sprite.CurrentAnimation != null && Sprite.CurrentAnimation[Sprite.currentAnimationIndex].flip)) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 
            mainLayerDepth
            );

        if (DrawName.Value)
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

        if (DrawItemOrder.Value && ItemToOrder.Value != null)
        {
            Vector2 offset = new Vector2(0,
                (float) Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

            Vector2 pos = getLocalPosition(Game1.viewport) ;
            pos.Y -= 32 + Sprite.SpriteHeight * 3;

            b.Draw(
                Mod.Sprites,
                pos + offset,
                new Rectangle(0, 16, 16, 16),
                Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 
                0.99f);

            ItemToOrder.Value.drawInMenu(b, pos + offset, 0.35f, 1f, 0.992f);
        }

        base.DrawBreathing(b, alpha);
        base.DrawGlow(b);
        base.DrawEmote(b);
    }

    public override bool tryToReceiveActiveObject(Farmer who, bool probe = false)
    {
        if (!probe)
        {
            Table? table = Group.ReservedTable;
            
            if (table != null && Mod.PlayerInteractWithTable(table, who))
                return true;
        }
        
        return base.tryToReceiveActiveObject(who, probe);
    }

    internal void SitDown(int direction)
    {
        Vector2 sitPosition = Position + CommonHelper.DirectionIntToDirectionVector(direction) * 64f;
        LerpPosition(Position, sitPosition, 0.2f);
    }

    private void LerpPosition(Vector2 startPosition, Vector2 endPosition, float duration)
    {
        _lerpStartPosition = startPosition;
        _lerpEndPosition = endPosition;
        _lerpPosition = 0f;
        _lerpDuration = duration;
    }
}
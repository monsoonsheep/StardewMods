using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.ChairsAndTables;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using System.Linq;

namespace MyCafe.Customers;

internal class Customer : NPC
{
    private Seat _reservedSeat;

    internal Seat ReservedSeat
    {
        get => _reservedSeat ??= Group?.ReservedTable.Seats.FirstOrDefault(s => s.ReservingCustomer.Equals(this));
        set => _reservedSeat = value;
    }
    internal NetRef<Item> ItemToOrder = [];
    internal NetBool DrawName = new NetBool(false);
    internal NetBool DrawItemOrder = new NetBool(false);

    internal bool IsSittingDown;
    internal CustomerGroup Group;
    private Vector2 _lerpStartPosition;
    private Vector2 _lerpEndPosition;
    private float _lerpPosition = -1f;
    private float _lerpDuration = -1f;

    public static PathFindController.endBehavior SitDownBehavior = delegate (Character c, GameLocation loc)
    {
        if (c is Customer p)
        {
            int direction = Utility.DirectionIntFromVectors(p.Tile, p.ReservedSeat.Position.ToVector2());
            p.SitDown(direction);
            p.faceDirection(p.ReservedSeat.SittingDirection);

            p.IsSittingDown = true;
            if (p.Group.Members.Any(other => !other.IsSittingDown))
                return;

            Game1.delayedActions.Add(new DelayedAction(500, delegate ()
            {
                p.ReservedSeat.Table.State.Set(TableState.CustomersThinkingOfOrder);
            }));
        }
    };

    public Customer()
    {

    }

    public Customer(string name, Vector2 position, string location, AnimatedSprite sprite, Texture2D portrait) : base(sprite, position, location, 2, name, portrait, eventActor: false)
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

        if (!currentLocation.farmers.Any() && currentLocation.Name.Equals("BusStop") && controller != null)
        {
            while (currentLocation.Name.Equals("BusStop"))
            {
                controller.pathToEndPoint.Pop();
                GameLocation loc = currentLocation;
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
        int standingY = base.StandingPixel.Y;
        float mainLayerDepth = Math.Max(0f, standingY / 10000f);

        b.Draw(
            Sprite.Texture, 
            getLocalPosition(Game1.viewport) + new Vector2((float) GetSpriteWidthForPositioning() * 4 / 2, (float) GetBoundingBox().Height / 2) + (shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), 
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
            Vector2 p = getLocalPosition(Game1.viewport) - new Vector2(40, 64);
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

        if (DrawItemOrder.Value)
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
            Table table = Group.ReservedTable;
            
            if (Mod.Cafe.ClickTable(table, who))
                return false;
        }
        
        return base.tryToReceiveActiveObject(who, probe);
    }

    internal void SitDown(int direction)
    {
        Vector2 sitPosition = Position + Utility.DirectionIntToDirectionVector(direction) * 64f;
        LerpPosition(Position, sitPosition, 0.2f);
    }

    public void LerpPosition(Vector2 startPosition, Vector2 endPosition, float duration)
    {
        _lerpStartPosition = startPosition;
        _lerpEndPosition = endPosition;
        _lerpPosition = 0f;
        _lerpDuration = duration;
    }
}
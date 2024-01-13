using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.ChairsAndTables;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using Object = StardewValley.Object;

namespace MyCafe.Customers;

internal class Customer(string name, Vector2 position, string location, AnimatedSprite sprite, Texture2D portrait)
    : NPC(sprite, position, location, 2, name, portrait, eventActor: true)
{
    internal readonly NetRef<Seat> ReservedSeat = new NetRef<Seat>();
    internal NetRef<Item> ItemToOrder = new NetRef<Item>();
    internal NetBool DrawName = new NetBool(false);

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
            int direction = Utility.DirectionIntFromVectors(p.Tile, p.ReservedSeat.Value.Position.ToVector2());
            p.SitDown(direction);
            p.faceDirection(p.ReservedSeat.Value.SittingDirection);

            p.IsSittingDown = true;
            if (p.Group.Members.Any(other => !other.IsSittingDown))
                return;

            Game1.delayedActions.Add(new DelayedAction(500, delegate ()
            {
                p.ReservedSeat.Value.Table.State.Set(TableState.CustomersThinkingOfOrder);
            }));
        }
    };

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields.AddField(ReservedSeat).AddField(ItemToOrder).AddField(DrawName);
    }

    public override void update(GameTime gameTime, GameLocation location)
    {
        base.update(gameTime, location);

        if (!Context.IsMainPlayer)
            return;

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
        base.draw(b, alpha);
        if (DrawName.Value)
        {
            Vector2 pos = getLocalPosition(Game1.viewport) - new Vector2(40, 64);
            b.DrawString(
                Game1.dialogueFont, 
                this.displayName,
                pos, 
                Color.White * 0.75f, 
                0f, 
                Vector2.Zero,
                new Vector2(0.3f, 0.3f), 
                SpriteEffects.None, 
                base.StandingPixel.Y / 10000f + 0.001f
                );

        }
    }

    internal void SitDown(int direction)
    {
        Vector2 sitPosition = Position + Utility.DirectionIntToDirectionVector(direction) * 64f;
        LerpPosition(Position, sitPosition, 0.2f);
    }

    public void LerpPosition(Vector2 start_position, Vector2 end_position, float duration)
    {
        _lerpStartPosition = start_position;
        _lerpEndPosition = end_position;
        _lerpPosition = 0f;
        _lerpDuration = duration;
    }
}
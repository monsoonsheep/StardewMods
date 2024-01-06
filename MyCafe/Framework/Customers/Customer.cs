using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.Framework.ChairsAndTables;
using StardewValley;
using StardewValley.Pathfinding;

namespace MyCafe.Framework.Customers;

internal class Customer : NPC
{
    internal Seat ReservedSeat = null;
    internal string ItemToOrder = null;

    private Vector2 _lerpStartPosition;
    private Vector2 _lerpEndPosition;
    private float _lerpPosition = -1f;
    private float _lerpDuration = -1f;

    public static PathFindController.endBehavior SitDownBehavior = delegate(Character c, GameLocation loc)
    {
        if (c is Customer p)
        {
            int direction = Utility.DirectionIntFromVectors(p.Tile, p.ReservedSeat.Position);
            p.SitDown(direction);
            p.faceDirection(p.ReservedSeat.SittingDirection);
        }
    };

    public Customer(string name, Vector2 position, string location, AnimatedSprite sprite, Texture2D portrait)
        : base(sprite, position, location, 2, name, portrait, eventActor: true)
    {
    }

    public override void update(GameTime gameTime, GameLocation location)
    {
        base.update(gameTime, location);
        speed = 5;

        if (_lerpPosition >= 0f)
        {
            _lerpPosition += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_lerpPosition >= _lerpDuration)
            {
                _lerpPosition = _lerpDuration;
            }
            base.Position = new Vector2(StardewValley.Utility.Lerp(_lerpStartPosition.X, _lerpEndPosition.X, _lerpPosition / _lerpDuration), StardewValley.Utility.Lerp(_lerpStartPosition.Y, _lerpEndPosition.Y, _lerpPosition / _lerpDuration));
            if (_lerpPosition >= _lerpDuration)
            {
                _lerpPosition = -1f;
            }
        }
    }

    internal Vector2 GetSeatPosition()
    {
        if (base.modData.TryGetValue("MonsoonSheep.MyCafe_ModDataSeatPos", out var result))
        {
            var split = result.Split(' ');
            Vector2 v = new Vector2(int.Parse(split[0]), int.Parse(split[1]));
            return v;
        }


        return Vector2.Zero;
    }

    internal string GetOrderItem()
    {
        return base.modData.TryGetValue("MonsoonSheep.MyCafe_ModDataOrderItem", out var result) ? result : null;
    }

    internal void SitDown(int direction)
    {
        Vector2 sitPosition = base.Position + (Utility.DirectionIntToDirectionVector(direction) * 64f);
        LerpPosition(base.Position, sitPosition, 0.2f);
    }

    public void LerpPosition(Vector2 start_position, Vector2 end_position, float duration)
    {
        _lerpStartPosition = start_position;
        _lerpEndPosition = end_position;
        _lerpPosition = 0f;
        _lerpDuration = duration;
    }
}
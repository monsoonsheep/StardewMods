using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyCafe.ChairsAndTables;
using Netcode;
using StardewValley;
using StardewValley.Pathfinding;
using Object = StardewValley.Object;

namespace MyCafe.Customers;

internal class Customer : NPC
{
    internal enum CustomerState
    {
        GoingToTable, WaitingToOrder, WaitingForFood, Eating, Leaving, Unknown
    }

    internal CustomerState State = CustomerState.GoingToTable;
    internal readonly NetRef<Seat> ReservedSeat = new NetRef<Seat>();
    internal Object ItemToOrder = null;

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
            Game1.delayedActions.Add(new DelayedAction(500, delegate ()
            {
                p.ReservedSeat.Value.Table.IsReadyToOrder.Set(true);
            }));
        }
    };

    public Customer(string name, Vector2 position, string location, AnimatedSprite sprite, Texture2D portrait)
        : base(sprite, position, location, 2, name, portrait, eventActor: true)
    {
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields.AddField(ReservedSeat);
    }

    public override void update(GameTime gameTime, GameLocation location)
    {
        base.update(gameTime, location);
        speed = 4;

        switch (State)
        {
            case CustomerState.GoingToTable:
                break;
            case CustomerState.WaitingToOrder:
                break;
            case CustomerState.WaitingForFood:
                break;
            case CustomerState.Eating:
                break;
            case CustomerState.Leaving:
                break;
        }

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

    internal Vector2 GetSeatPosition()
    {
        if (modData.TryGetValue("MonsoonSheep.MyCafe_ModDataSeatPos", out var result))
        {
            var split = result.Split(' ');
            Vector2 v = new Vector2(int.Parse(split[0]), int.Parse(split[1]));
            return v;
        }


        return Vector2.Zero;
    }

    internal string GetOrderItem()
    {
        return modData.TryGetValue(ModKeys.MODDATA_ORDERITEM, out var result) ? result : null;
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
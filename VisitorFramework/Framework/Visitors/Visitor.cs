#region Usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using VisitorFramework.Framework.Visitors.Activities;

#endregion

namespace VisitorFramework.Framework.Visitors
{
    public class Visitor : NPC
    {
        internal VisitAction CurrentAction;

        internal delegate void ActionCompleteHandler(Visitor sender);
        internal event ActionCompleteHandler ActionComplete;

        public Visitor() { }

        public Visitor(string name, Vector2 position, string location, AnimatedSprite sprite, Texture2D portrait)
        : base(sprite, position, location, 3, name, false, portrait)
        {
            eventActor = true;
            willDestroyObjectsUnderfoot = true;
            collidesWithOtherCharacters.Set(false);
            speed = 3;

            followSchedule = false;
            ignoreScheduleToday = true;
            Position = position;
        }

        public override bool shouldCollideWithBuildingLayer(GameLocation location) => true;

        public override bool canPassThroughActionTiles() => true;

        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);

            if (!Context.IsWorldReady || !Context.IsMainPlayer) return;
            speed = 5; // For debug
        }

        public void DoAction()
        {
            CurrentAction.Start(this);
        }

        internal void FinishAction()
        {
            ActionComplete?.Invoke(this);
        }
    }
}
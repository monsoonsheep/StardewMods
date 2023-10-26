using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FarmCafe.Framework.Characters;
using FarmCafe.Framework.Managers;
using FarmCafe.Framework.Objects;
using FarmCafe.Framework.Multiplayer;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using xTile.Dimensions;
using Rectangle = xTile.Dimensions.Rectangle;
using Sickhead.Engine.Util;

namespace FarmCafe.Patching
{
    internal class GameLocationPatches : PatchList
    {
        public GameLocationPatches()
        {
            Patches = new List<Patch>
            {
                new (
                    typeof(GameLocation),
                    "cleanupBeforeSave",
                    null,
                    postfix: nameof(CleanupBeforeSavePostfix)),
                new (
                    typeof(GameLocation),
                    "checkAction",
                    new [] { typeof(Location), typeof(Rectangle), typeof(Farmer) },
                    postfix: nameof(CheckActionPostfix)),
                new (
                    typeof(BusStop),
                    "UpdateWhenCurrentLocation",
                    null,
                    prefix: nameof(BusStopUpdateWhenCurrentLocationPostfix)),
            };
        }

        private static void BusStopUpdateWhenCurrentLocationPostfix(BusStop __instance, GameTime time)
        {
            if (ModEntry.BusManager == null || __instance == null)
                return;

            if (ModEntry.BusManager.BusLeaving && !ModEntry.BusManager.BusGone)
            {
                FieldInfo busMotion = typeof(BusStop).GetField("busMotion", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo busPosition = typeof(BusStop).GetField("busPosition", BindingFlags.NonPublic | BindingFlags.Instance);

                if (busMotion == null || busPosition == null)
                {
                    ModEntry.BusManager.BusGone = true;
                    return;
                }

                if (__instance.farmers.Count == 0)
                {
                    busPosition.SetValue(__instance, new Vector2(-1000f, ((Vector2) busPosition.GetValue(__instance)).Y));
                    ModEntry.BusManager.BusGone = true;
                    return;
                }

                Vector2 busMotionValue = (Vector2) busMotion.GetValue(__instance);
                Vector2 busPositionValue = (Vector2) busPosition.GetValue(__instance);

                busMotion.SetValue(__instance,  new Vector2(busMotionValue.X - 0.075f, busMotionValue.Y));
                if (busPositionValue.X + 512f < 0f)
                {
                    ModEntry.BusManager.BusGone = true;
                    busMotion.SetValue(__instance, Vector2.Zero);
                }
            }

            if (ModEntry.BusManager.BusReturning)
            {
                
                FieldInfo busMotion = typeof(BusStop).GetField("busMotion", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo busPosition = typeof(BusStop).GetField("busPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                if (busMotion == null || busPosition == null)
                {
                    busPosition.SetValue(__instance, new Vector2(704f, ((Vector2) busPosition.GetValue(__instance)).Y));
                    ModEntry.BusManager.BusReturning = false;
                    return;
                }

                Vector2 busMotionValue = (Vector2) busMotion.GetValue(__instance);
                Vector2 busPositionValue = (Vector2) busPosition.GetValue(__instance);

                // Instantly teleport to position if no farmer is watching
                if (__instance.farmers.Count == 0)
                {
                    busPosition.SetValue(__instance, new Vector2(704f, busPositionValue.Y));
                    busMotion.SetValue(__instance, Vector2.Zero);
                    typeof(BusStop).GetField("busDoor", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1311, 16, 38), busPositionValue + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
                    {
                        interval = 999999f,
                        animationLength = 6,
                        holdLastFrame = true,
                        layerDepth = (busPositionValue.Y + 192f) / 10000f + 1E-05f,
                        scale = 4f
                    });
                    ModEntry.BusManager.BusReturning = false;
                    return;
                }

                // Decelerate after getting to X=15 from the right
                if (busPositionValue.X - 704f < 256f)
                {
                    busMotion.SetValue(__instance, new Vector2(Math.Min(-1f, busMotionValue.X * 0.98f), busMotionValue.Y));
                }

                if (Math.Abs(busPositionValue.X - 704f) <= Math.Abs(((Vector2) busMotion.GetValue(__instance)).X * 1.5f))
                {
                    busPosition.SetValue(__instance, new Vector2(704f, busPositionValue.Y));
                    busMotion.SetValue(__instance, Vector2.Zero);

                    TemporaryAnimatedSprite busDoor = (TemporaryAnimatedSprite) typeof(BusStop).GetField("busDoor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);

                    if (busDoor != null)
                    {
                        busDoor.Position = busPositionValue + new Vector2(16f, 26f) * 4f;
                        busDoor.pingPong = true;
                        busDoor.interval = 70f;
                        busDoor.currentParentTileIndex = 5;
                        busDoor.endFunction = delegate
                        {
                            typeof(BusStop).GetField("busDoor", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(288, 1311, 16, 38), busPositionValue + new Vector2(16f, 26f) * 4f, flipped: false, 0f, Color.White)
                            {
                                interval = 999999f,
                                animationLength = 6,
                                holdLastFrame = true,
                                layerDepth = (busPositionValue.Y + 192f) / 10000f + 1E-05f,
                                scale = 4f
                            });
                        }; // TODO move this delegate to busmanager

                        __instance.localSound("trashcanlid");

                    }
                    ModEntry.BusManager.BusReturning = false;

                }
            }
        }

        private static void CleanupBeforeSavePostfix(GameLocation __instance)
        {
            for (int i = __instance.characters.Count - 1; i >= 0; i--)
            {
                if (__instance.characters[i] is Customer)
                {
                    Logger.Log("Removing character before saving");
                    __instance.characters.RemoveAt(i);
                }
            }
        }

        private static void CheckActionPostfix(GameLocation __instance, Location tileLocation, Rectangle viewport, Farmer who, ref bool __result)
        {
            if (!ModEntry.CafeManager.CafeLocations.Contains(__instance)) return;

            foreach (MapTable table in ModEntry.CafeManager.Tables.OfType<MapTable>())
            {
                if (table.BoundingBox.Contains(tileLocation.X * 64, tileLocation.Y * 64))
                {
                    if (!Context.IsMainPlayer)
                    {
                        Sync.SendTableClick(table, who);
                    }
                    else
                    {
                        ModEntry.CafeManager.FarmerClickTable(table, who);
                    }

                    __result = true;
                    return;
                }
            }
        }
    }
}

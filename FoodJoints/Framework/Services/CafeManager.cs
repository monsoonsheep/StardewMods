using Microsoft.Xna.Framework.Graphics;
using StardewMods.FoodJoints.Framework.Enums;
using StardewMods.FoodJoints.Framework.Game;
using StardewMods.FoodJoints.Framework.Inventories;
using StardewMods.FoodJoints.Framework.Objects;
using StardewMods.FoodJoints.Framework.UI;

namespace StardewMods.FoodJoints.Framework.Services;

internal class CafeManager
{
    internal static CafeManager Instance = null!;
    private TableManager tables = null!;
    internal LocationManager locations = null!;
    internal CustomerManager customers = null!;

    internal int LastTimeCustomersArrived;
    internal int MoneyForToday;
    internal int OpeningTime;
    internal int ClosingTime;

    internal bool Enabled
    {
        get => Mod.NetState.CafeEnabled.Value;
        set => Mod.NetState.CafeEnabled.Set(value);
    }

    internal bool Open
    {
        get => Mod.NetState.CafeOpen.Value;
        set => Mod.NetState.CafeOpen.Set(value);
    }

    internal FoodMenuInventory Menu
        => Mod.NetState.Menu.Value;

    internal CafeManager()
        => Instance = this;

    internal void Initialize()
    {
        this.tables = Mod.Tables;
        this.locations = Mod.Locations;
        this.customers = Mod.Customers;

        Mod.Events.GameLoop.DayStarted += this.OnDayStarted;
        Mod.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        Mod.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        Mod.Events.Display.RenderedWorld += this.OnRenderedWorld;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.Menu.Initialize();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.LastTimeCustomersArrived = 0;
        this.customers.Groups.Clear();
        this.MoneyForToday = 0;
        this.UpdateLocations();
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!this.Enabled)
            return;

        if (Game1.timeOfDay >= this.OpeningTime && Game1.timeOfDay <= this.ClosingTime)
        {
            this.Open = true;
            Mod.Customers.ScheduleArrivals();
        }
        else
        {
            this.Open = false;
        }

        // Customers waiting for too long are forced to leave
        for (int i = this.customers.Groups.Count - 1; i >= 0; i--)
        {
            this.customers.Groups[i].MinutesSitting += 10;
            Table? table = this.customers.Groups[i].ReservedTable;
            if (table is { State.Value: not TableState.CustomersEating } && this.customers.Groups[i].MinutesSitting > Mod.Config.MinutesBeforeCustomersLeave)
            {
                this.customers.EndCustomerGroup(this.customers.Groups[i]);
            }
        }

        // Update unbreakability of signboard when the cafe opens and closes
        if (this.locations.Signboard != null)
        {
            // Fragility 2 is unbreakable, we set it when cafe is operating
            this.locations.Signboard.Fragility = this.Open ? 2 : 0;
        }

        if (this.Open)
        {
            if (Game1.activeClickableMenu is CafeMenu cafeMenu)
                cafeMenu.Locked = true;

            // If cafe open, try spawn customers
            this.customers.CustomerSpawningUpdate();
        }
        else
        {
            if (Game1.activeClickableMenu is CafeMenu cafeMenu)
                cafeMenu.Locked = false;
        }
    }

    internal void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!this.Enabled)
            return;

        // Get list of reserved tables with center coords
        foreach (Table table in Mod.NetState.Tables)
        {
            if (Game1.currentLocation.NameOrUniqueName.Equals(table.Location))
            {
                // Table status
                Vector2 offset = new Vector2(0, (float)Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                switch (table.State.Value)
                {
                    case TableState.CustomersDecidedOnOrder:
                        // Exclamation mark
                        e.SpriteBatch.Draw(
                            Game1.mouseCursors,
                            Game1.GlobalToLocal(table.Center + new Vector2(-8, -64)) + offset,
                            new Rectangle(402, 495, 7, 16),
                            Color.White,
                            0f,
                            new Vector2(1f, 4f),
                            4f,
                            SpriteEffects.None,
                            1f);

                        break;

                    case TableState.Free:
                        // Display number of seats on table
                        if (this.Enabled && !this.Open)
                        {
                            e.SpriteBatch.DrawString(Game1.tinyFont,
                                                     table.Seats.Count.ToString(),
                                                     Game1.GlobalToLocal(table.Center + new Vector2(-12, -112)) + offset,
                                                     Color.LightBlue,
                                                     0f,
                                                     Vector2.Zero,
                                                     5f,
                                                     SpriteEffects.None,
                                                     0.99f);
                            e.SpriteBatch.DrawString(Game1.tinyFont,
                                                     table.Seats.Count.ToString(),
                                                     Game1.GlobalToLocal(table.Center + new Vector2(-10, -96)) + offset,
                                                     Color.Black,
                                                     0f,
                                                     Vector2.Zero,
                                                     4f,
                                                     SpriteEffects.None,
                                                     1f);
                        }

                        break;
                }
            }
        }

        foreach (NPC c in this.customers.Groups.SelectMany(g => g.Members))
        {
            float layerDepth = Math.Max(0f, c.StandingPixel.Y / 10000f);
            Vector2 drawPosition = c.getLocalPosition(Game1.viewport);

            if (c.get_DrawName().Value == true)
            {
                e.SpriteBatch.DrawString(
                    Game1.dialogueFont,
                    c.displayName,
                    drawPosition - new Vector2(40, 64),
                    Color.White * 0.75f,
                    0f,
                    Vector2.Zero,
                    new Vector2(0.3f, 0.3f),
                    SpriteEffects.None,
                    layerDepth + 0.001f
                );
            }

            Item item;
            if (c.get_DrawOrderItem().Value == true && (item = c.get_OrderItem().Value) != null)
            {
                Vector2 offset = new Vector2(0,
                    (float)Math.Round(4f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0)));

                drawPosition.Y -= 32 + c.Sprite.SpriteHeight * 3;

                // Draw bubble
                e.SpriteBatch.Draw(
                    Mod.Sprites,
                    drawPosition + offset,
                    new Rectangle(0, 16, 16, 16),
                    Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None,
                    0.99f);

                // Item inside the bubble
                item.drawInMenu(e.SpriteBatch, drawPosition + offset, 0.40f, 1f, 0.992f, StackDrawType.Hide, Color.White, drawShadow: false);

                // Draw item name if hovering over bubble
                Vector2 mouse = new Vector2(Game1.getMouseX(), Game1.getMouseY());
                if (Vector2.Distance(drawPosition, mouse) <= Game1.tileSize)
                {
                    Vector2 size = Game1.dialogueFont.MeasureString(item.DisplayName) * 0.75f;
                    e.SpriteBatch.DrawString(Game1.dialogueFont, item.DisplayName, drawPosition + new Vector2(32 - size.X / 2f, -10f), Color.Black, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 1f);
                }
            }
        }
    }

    /// <summary>
    /// This is called at start of day, when signboard is placed, and when signboard is removed
    /// </summary>
    internal void UpdateLocations()
    {
        if (this.locations.UpdateSignboard())
        {
            if (this.locations.Signboard?.Location.ParentBuilding?.parentLocationName?.Value == "Farm")
                Mod.Pathfinding.AddRoutesToBuildingInFarm(this.locations.Signboard.Location);

            this.Enabled = true;
            this.Open = false;
            this.tables.PopulateTables();
        }
        else
        {

            this.Enabled = false;
            this.Open = false;
            Mod.NetState.Tables.Clear();
        }
    }
}

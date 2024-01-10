namespace MyCafe.UI;

/*
public sealed class CafeConfigPage : IClickableMenu
{
    private readonly List<ClickableComponent> optionSlots = new();
    private readonly List<OptionsElement> options = new();

    private int optionsSlotHeld = -1;
    private int currentItemIndex;
    private bool isScrolling;
    private Rectangle scrollbarRunner;

    private const int ItemsPerPage = 5;

    private bool scrolling;

    public CafeConfigPage(int x, int y, int width, int height) : base(x, y, width, height)
    {
        this.optionSlots.Clear();
        for (int i = 0; i < ItemsPerPage; i++)
            this.optionSlots.Add(new ClickableComponent(
                new Rectangle(
                    this.xPositionOnScreen + Game1.tileSize / 4,
                    this.yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * ((this.height - Game1.tileSize * 2) / ItemsPerPage),
                    this.width - Game1.tileSize / 2,
                    (this.height - Game1.tileSize * 2) / ItemsPerPage + Game1.pixelZoom),
                i.ToString()));


        this.options.Add(new OptionTimeSet(I18n.Menu_OpeningTime(), CafeManager.Instance.OpeningTime, 0700, 1800, (v) => CafeManager.Instance.OpeningTime = v));
        this.options.Add(new OptionTimeSet(I18n.Menu_ClosingTime(), CafeManager.Instance.ClosingTime, 1100, 2500, (v) => CafeManager.Instance.ClosingTime = v));
    }


    public string FormatTime(int time)
    {
        return Game1.getTimeOfDayString(SUtility.ConvertMinutesToTime(time*10)) + ", " + time.ToString();
    }

    public void SetTimeFromMinutes(int time, out int target)
    {
        target = SUtility.ConvertMinutesToTime(time) * 10;
    }


    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        currentItemIndex = Math.Max(0, Math.Min(options.Count - ItemsPerPage, currentItemIndex));

        UnsubscribeFromSelectedTextbox();
        for (var index = 0; index < optionSlots.Count; ++index)
            if (optionSlots[index].bounds.Contains(x, y) && currentItemIndex + index < options.Count && options[currentItemIndex + index].bounds
                    .Contains(x - optionSlots[index].bounds.X, y - optionSlots[index].bounds.Y))
            {
                options[currentItemIndex + index].receiveLeftClick(x - optionSlots[index].bounds.X, y - optionSlots[index].bounds.Y);
                optionsSlotHeld = index;
                break;
            }
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (GameMenu.forcePreventClose)
            return;
        base.releaseLeftClick(x, y);
        if (optionsSlotHeld != -1 && optionsSlotHeld + currentItemIndex < options.Count)
            options[currentItemIndex + optionsSlotHeld].leftClickReleased(x - optionSlots[optionsSlotHeld].bounds.X,
                y - optionSlots[optionsSlotHeld].bounds.Y);
        optionsSlotHeld = -1;
        scrolling = false;
    }

    public override void leftClickHeld(int x, int y)
    {
        if (GameMenu.forcePreventClose)
            return;
        base.leftClickHeld(x, y);
        //if (scrolling)
        //{
        //    var y1 = scrollBar.bounds.Y;
        //    scrollBar.bounds.Y = Math.Min(yPositionOnScreen + height - 64 - 12 - scrollBar.bounds.Height,
        //        Math.Max(y, yPositionOnScreen + upArrow.bounds.Height + 20));
        //    currentItemIndex = Math.Min(options.Count - 7,
        //        Math.Max(0, (int)(options.Count * (double)((y - scrollBarRunner.Y) / (float)scrollBarRunner.Height))));
        //    setScrollBarToCurrentIndex();
        //    var y2 = scrollBar.bounds.Y;
        //    if (y1 == y2)
        //        return;
        //    Game1.playSound("shiny4");
        //}
        //else
        {
            if (optionsSlotHeld == -1 || optionsSlotHeld + currentItemIndex >= options.Count)
                return;
            options[currentItemIndex + optionsSlotHeld].leftClickHeld(x - optionSlots[optionsSlotHeld].bounds.X,
                y - optionSlots[optionsSlotHeld].bounds.Y);
        }
    }


    public bool IsDropdownActive()
    {
        return optionsSlotHeld != -1 && optionsSlotHeld + currentItemIndex < options.Count &&
               options[currentItemIndex + optionsSlotHeld] is OptionsDropDown;
    }


    public void UnsubscribeFromSelectedTextbox()
    {
        if (Game1.keyboardDispatcher.Subscriber == null)
            return;
        foreach (var option in options)
            if (option is OptionsTextEntry && Game1.keyboardDispatcher.Subscriber == (option as OptionsTextEntry).textBox)
            {
                Game1.keyboardDispatcher.Subscriber = null;
                break;
            }
    }


    public override void draw(SpriteBatch spriteBatch)
    {
        base.draw(spriteBatch);

        //Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);
        for (int index = 0; index < this.optionSlots.Count; ++index)
        {
            if (this.currentItemIndex >= 0 && this.currentItemIndex + index < this.options.Count)
                this.options[this.currentItemIndex + index].draw(spriteBatch, this.optionSlots[index].bounds.X, this.optionSlots[index].bounds.Y);
        }

        if (!GameMenu.forcePreventClose)
        {
            if (this.options.Count > ItemsPerPage)
            {
                IClickableMenu.drawTextureBox(spriteBatch, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), this.scrollbarRunner.X, this.scrollbarRunner.Y, this.scrollbarRunner.Width, this.scrollbarRunner.Height, Color.White, Game1.pixelZoom, false);
            }
        }
    }
}
*/
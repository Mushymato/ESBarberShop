using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace ESBarberShop;

public sealed class SpecialDrawsComponent(
    Rectangle bounds,
    Texture2D texture,
    Rectangle sourceRect,
    float scale,
    bool drawShadow = false
) : ClickableTextureComponent(bounds, texture, sourceRect, scale, drawShadow)
{
    private readonly string title = Game1.content.LoadString($"{ModEntry.Asset_Text}:Title");
    private readonly string question = Game1.content.LoadString($"{ModEntry.Asset_Text}:Question");

    public override void draw(
        SpriteBatch b,
        Color c,
        float layerDepth,
        int frameOffset = 0,
        int xOffset = 0,
        int yOffset = 0
    )
    {
        b.Draw(texture, new Vector2(sourceRect.X - (texture.Width - Game1.daybg.Width) / 2, sourceRect.Y), Color.White);

        SpriteText.drawStringWithScrollCenteredAt(b, title, bounds.X + sourceRect.Width / 2, sourceRect.Y - 128);

        Vector2 questionSize = Game1.dialogueFont.MeasureString(question);
        Utility.drawTextWithShadow(
            b,
            question,
            Game1.dialogueFont,
            new Vector2(bounds.X + sourceRect.Width / 2 - questionSize.X / 2, sourceRect.Y - 64),
            Game1.textColor,
            layerDepth: layerDepth
        );
    }
}

public sealed class BarberShopMenu : CharacterCustomization
{
    public BarberShopMenu(Source source)
        : base(source, false)
    {
        // make menu shorter
        height -= 128;
        UnResetComponents();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        source = Source.Wizard;
        base.gameWindowSizeChanged(oldBounds, newBounds);
        UnResetComponents();
    }

    public override void draw(SpriteBatch b)
    {
        base.draw(b);
    }

    public void UnResetComponents()
    {
        // remove gender buttons
        genderButtons.Clear();
        // remove skin selection
        leftSelectionButtons.RemoveAll(cc => cc.name == "Skin");
        rightSelectionButtons.RemoveAll(cc => cc.name == "Skin");

        // remove eye color picker
        eyeColorPicker = null;
        // magical knowledge about the label order
        labels.RemoveRange(0, 5);
        // magical knowledge about removing eye color picker
        colorPickerCCs.RemoveRange(0, 3);
        // hide name farmname fav
        source = Source.Dresser;
        nameBoxCC.visible = false;
        favThingBoxCC.visible = false;
        farmnameBoxCC.visible = false;
        // hide random button
        randomButton.visible = false;
        // move the portrait box
        portraitBox.X = xPositionOnScreen + width / 2 - portraitBox.Width / 2;
        portraitBox.Y += 64;
        // move the first pair of direction buttons
        ClickableComponent leftBtn = leftSelectionButtons.First();
        ClickableComponent rightBtn = rightSelectionButtons.First();
        int xDelta = rightBtn.bounds.X - leftBtn.bounds.X;
        leftBtn.bounds.X = xPositionOnScreen + width / 2 - leftBtn.bounds.Width - (xDelta - leftBtn.bounds.Width) / 2;
        leftBtn.bounds.Y += 64;
        rightBtn.bounds.X = leftBtn.bounds.X + xDelta;
        rightBtn.bounds.Y += 64;
        // move OK button up
        okButton.bounds.Y -= 128;

        if (Game1.options.snappyMenus && Game1.options.gamepadControls)
        {
            populateClickableComponentList();
            snapToDefaultClickableComponent();
        }

        // hack: insert an extra functionless ClickableTextureComponent to leftSelectionButtons and perform some draws inside
        leftSelectionButtons.Insert(
            0,
            new SpecialDrawsComponent(
                new(xPositionOnScreen, yPositionOnScreen, 0, 0),
                ModEntry.BarberBG,
                new(portraitBox.X, portraitBox.Y, width, height),
                4
            )
        );
    }
}

public sealed class ModEntry : Mod
{
    public const string ModId = "ES.BarberShop";
    public const string Asset_BarberBG = $"{ModId}/BarberBG";
    public const string Asset_Text = $"{ModId}/Text";

    public static Texture2D BarberBG => Game1.content.Load<Texture2D>(Asset_BarberBG);

    public override void Entry(IModHelper helper)
    {
        GameLocation.RegisterTileAction(ModId, TileAction);
        helper.ConsoleCommands.Add(ModId, "test barber menu", ConsoleESCustomize);
        helper.Events.Content.AssetRequested += OnAssetRequested;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(Asset_BarberBG))
        {
            e.LoadFromModFile<Texture2D>("assets/barber-bg.png", AssetLoadPriority.Low);
        }
        if (e.Name.IsEquivalentTo(Asset_Text))
        {
            e.LoadFromModFile<Dictionary<string, string>>("assets/text.json", AssetLoadPriority.Exclusive);
        }
    }

    private bool TileAction(GameLocation location, string[] arg2, Farmer farmer, Point point)
    {
        Game1.activeClickableMenu = new BarberShopMenu(CharacterCustomization.Source.Wizard);
        return true;
    }

    private void ConsoleESCustomize(string arg1, string[] arg2)
    {
        if (!Context.IsWorldReady)
            return;

        if (arg2.Length == 0)
        {
            Game1.activeClickableMenu = new BarberShopMenu(CharacterCustomization.Source.Wizard);
        }
        else
        {
            Game1.activeClickableMenu = new CharacterCustomization(CharacterCustomization.Source.Wizard);
        }
    }
}

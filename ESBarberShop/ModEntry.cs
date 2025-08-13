using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace ESBarberShop;

/// <summary>
/// A special 0x0 sized (un)clickable component to be inserted in front of leftSelectionButtons
/// This is a small no-harmony hack used to draw stuff just above background but beneath most other things
/// </summary>
public sealed class SpecialDrawsComponent(
    Rectangle bounds, // new(xPositionOnScreen, yPositionOnScreen, 0, 0),
    Texture2D texture,
    Rectangle sourceRect, // new(portraitBox.X, portraitBox.Y, width, height)
    float scale
) : ClickableTextureComponent(bounds, texture, sourceRect, scale, false)
{
    public override void draw(
        SpriteBatch b,
        Color c,
        float layerDepth,
        int frameOffset = 0,
        int xOffset = 0,
        int yOffset = 0
    )
    {
        // bg texture
        b.Draw(texture, new Vector2(sourceRect.X - (texture.Width - Game1.daybg.Width) / 2, sourceRect.Y), Color.White);

        // 'Barber Shop'
        SpriteText.drawStringWithScrollCenteredAt(
            b,
            I18n.Menu_Title(),
            bounds.X + sourceRect.Width / 2,
            sourceRect.Y - 128
        );

        // 'What style would you like today?'
        Vector2 questionSize = Game1.dialogueFont.MeasureString(I18n.Menu_Question());
        Utility.drawTextWithShadow(
            b,
            I18n.Menu_Question(),
            Game1.dialogueFont,
            new Vector2(bounds.X + sourceRect.Width / 2 - questionSize.X / 2, sourceRect.Y - 64),
            Game1.textColor,
            layerDepth: layerDepth
        );
    }
}

/// <summary>Barber shop flavored CharacterCustomization menu</summary>
public sealed class BarberShopMenu : CharacterCustomization
{
    private static readonly MethodInfo optionButtonClickMethod = typeof(CharacterCustomization).GetMethod(
        "optionButtonClick",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    private (int, int, Color) PriorState;

    public BarberShopMenu(Source source)
        : base(source, false)
    {
        // make menu shorter
        height -= 160;
        // move OK button up
        okButton.bounds.Y -= 160;
        UnResetComponents();
        PriorState = new(Game1.player.accessory.Value, Game1.player.hair.Value, Game1.player.hairstyleColor.Value);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        source = Source.Wizard;
        base.gameWindowSizeChanged(oldBounds, newBounds);
        UnResetComponents();
    }

    /// <summary>
    /// Vanilla method CharacterCustomization.ResetComponents arranges all the components of a menu.
    /// This method rearranges them afterwards.
    /// It is to be called in the ctor and gameWindowSizeChanged just like ResetComponents
    /// </summary>
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

    private void ConfirmChoice(Farmer who)
    {
        if (
            who.accessory.Value != PriorState.Item1
            || who.hair.Value != PriorState.Item2
            || who.hairstyleColor.Value != PriorState.Item3
        )
        {
            Game1.player.Money -= ModEntry.BarberCost;
        }
        Game1.nextClickableMenu.Add(new DialogueBox(I18n.Menu_Finished()));
        optionButtonClickMethod?.Invoke(this, [okButton.name]);
    }

    private void CancelChoice(Farmer who)
    {
        who.changeAccessory(PriorState.Item1);
        who.changeHairStyle(PriorState.Item2);
        who.changeHairColor(PriorState.Item3);
        Game1.nextClickableMenu.Add(new DialogueBox(I18n.Menu_Canceled()));
        optionButtonClickMethod?.Invoke(this, [okButton.name]);
    }

    /// <summary>When exiting, require 500 gold for hairstyle change</summary>
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (okButton.containsPoint(x, y) && canLeaveMenu() && optionButtonClickMethod != null)
        {
            okButton.scale -= 0.25f;
            okButton.scale = Math.Max(0.75f, okButton.scale);
            SetChildMenu(new ConfirmationDialog(I18n.Menu_Confirm(), ConfirmChoice, CancelChoice));
            return;
        }
        base.receiveLeftClick(x, y, playSound);
    }
}

public sealed class ModEntry : Mod
{
    public const string ModId = "ES.BarberShop";
    public const string Asset_BarberBG = $"{ModId}/BarberBG";
    public const int BarberCost = 500;

    public static Texture2D BarberBG => Game1.content.Load<Texture2D>(Asset_BarberBG);

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
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
    }

    private bool TileAction(GameLocation location, string[] args, Farmer farmer, Point point)
    {
        if (Game1.player.Money < BarberCost)
        {
            Game1.drawObjectDialogue(I18n.Menu_NotEnoughMoney());
            return false;
        }
        Game1.activeClickableMenu = new BarberShopMenu(CharacterCustomization.Source.Wizard);
        if (!ArgUtility.TryGetDirection(args, 1, out int direction, out string error, name: "string facingDirection"))
            direction = 2;
        DelayedAction.functionAfterDelay(() => farmer.faceDirection(direction), 1);
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

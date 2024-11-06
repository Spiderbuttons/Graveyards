using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;

namespace Graveyards;

public class InstrumentMenu : IClickableMenu
{
    public InstrumentMenu() : base(Game1.uiViewport.Width, Game1.uiViewport.Height, Game1.uiViewport.Width,
        Game1.uiViewport.Height, showUpperRightCloseButton: false)
    {
        Game1.player.CanMove = false;
        Game1.freezeControls = true;
        Game1.displayHUD = false;
        Game1.playSound("smallSelect");
        Game1.showGlobalMessage(i18n.PressEsc());
        Game1.showGlobalMessage(i18n.UseKeyboard());

        ModEntry.lastNotePlayed = 0;
    }

    protected override void cleanupBeforeExit()
    {
        Game1.player.forceCanMove();
        Game1.freezeControls = false;
        Game1.displayHUD = true;
    }
}
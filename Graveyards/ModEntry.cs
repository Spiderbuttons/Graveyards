using System;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using SkeletonWar.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using Graveyards.Helpers;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace Graveyards
{
    internal sealed class ModEntry : Mod
    {
        internal static IModHelper ModHelper { get; set; } = null!;
        internal static IMonitor ModMonitor { get; set; } = null!;
        internal static ModConfig Config { get; set; } = null!;
        internal static Harmony Harmony { get; set; } = null!;

        public override void Entry(IModHelper helper)
        {
            i18n.Init(helper.Translation);
            ModHelper = helper;
            ModMonitor = Monitor;
            Config = helper.ReadConfig<ModConfig>();
            Harmony = new Harmony(ModManifest.UniqueID);

            Harmony.PatchAll();

            Helper.Events.Content.AssetRequested += this.OnAssetRequested;
            Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.BaseName.Contains("Maps/Mines/"))
            {
                Log.Debug("Found mine level: " + e.NameWithoutLocale.BaseName);
                e.Edit(asset =>
                {
                    IAssetDataForMap editor = asset.AsMap();
                    Map sourceMap = this.Helper.ModContent.Load<Map>("assets/Graveyard1.tmx");
                    editor.ReplaceWith(sourceMap);
                });
            }
        }

        private Tile GetTile(Map map, string layerName, int tileX, int tileY)
        {
            Layer layer = map.GetLayer(layerName);
            Location pixelPosition = new Location(tileX * Game1.tileSize, tileY * Game1.tileSize);

            return layer.PickTile(pixelPosition, Game1.viewport.Size);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null) Config.SetupConfig(configMenu, ModManifest, Helper);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button is SButton.F5)
            {
                Tile tile = GetTile(Game1.currentLocation.Map, "Buildings", 42, 90);
                if (tile is null)
                {
                    Log.Debug("Tile is null");
                    return;
                }

                foreach (var prop in tile.Properties)
                {
                    Log.Debug($"{prop.Key}: {prop.Value}");
                }
            }

            if (e.Button is SButton.F6)
            {
                Helper.GameContent.InvalidateCache("Maps/Mines");
            }
        }
    }
}
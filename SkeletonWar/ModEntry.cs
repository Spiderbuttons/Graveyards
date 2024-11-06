using System;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using SkeletonWar.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using SkeletonWar.Helpers;

namespace SkeletonWar
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

            Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
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
        }
    }
}
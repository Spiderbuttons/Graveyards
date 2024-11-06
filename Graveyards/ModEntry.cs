using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework;
using SkeletonWar.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using Graveyards.Helpers;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Audio;
using StardewValley.GameData;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Network;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;
using Object = StardewValley.Object;

namespace Graveyards
{
    internal sealed class ModEntry : Mod
    {
        internal static IModHelper ModHelper { get; set; } = null!;
        internal static IMonitor ModMonitor { get; set; } = null!;
        internal static ModConfig Config { get; set; } = null!;
        internal static Harmony Harmony { get; set; } = null!;

        internal static List<int> graveLevels = new();

        internal static Random graveRandom = null!;

        internal static Dictionary<SButton, int> Pitches = new()
        {
            { SButton.Q, 1600 },
            { SButton.W, 1700 },
            { SButton.E, 1800 },
            { SButton.R, 1900 },
            { SButton.T, 2000 },
            { SButton.Y, 2100 },
            { SButton.U, 2200 },
            { SButton.I, 2300 },
            { SButton.O, 2400 },
            { SButton.P, 2500 },
            { SButton.A, 700 },
            { SButton.S, 800 },
            { SButton.D, 900 },
            { SButton.F, 1000 },
            { SButton.G, 1100 },
            { SButton.H, 1200 },
            { SButton.J, 1300 },
            { SButton.K, 1400 },
            { SButton.L, 1500 },
            { SButton.Z, 0 },
            { SButton.X, 100 },
            { SButton.C, 200 },
            { SButton.V, 300 },
            { SButton.B, 400 },
            { SButton.N, 500 },
            { SButton.M, 600 },
        };

        internal static double lastNotePlayed;

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
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.Player.Warped += OnWarped;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void ChooseGraveLevels()
        {
            graveLevels.Clear();
            while (graveLevels.Count < 1)
            {
                int level = graveRandom.Next(5, 40);
                if (level % 10 == 0) continue;
                if (!graveLevels.Contains(level))
                {
                    graveLevels.Add(level);
                }
            }
            Log.Debug($"Chosen levels: {string.Join(", ", graveLevels)}");
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            graveRandom = Utility.CreateDaySaveRandom();
            ChooseGraveLevels();
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.Graveyards/1"))
            {
                e.LoadFromModFile<Map>("assets/Graveyard1.tmx", AssetLoadPriority.Exclusive);
            }
            
            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.Graveyards/2"))
            {
                e.LoadFromModFile<Map>("assets/Graveyard2.tmx", AssetLoadPriority.Exclusive);
            }
            
            if (e.NameWithoutLocale.BaseName.StartsWith("Maps/Mines/"))
            {
                Log.Debug("Found mine level: " + e.NameWithoutLocale.BaseName);
                if (!int.TryParse(e.NameWithoutLocale.BaseName.Split('/').Last(), out int lvl)) return;
                if (!graveLevels.Contains(lvl)) return;
                e.Edit(asset =>
                {
                    try
                    {
                        IAssetDataForMap editor = asset.AsMap();
                        int mapNum = graveRandom.Next(1, 3);
                        Map sourceMap = Helper.GameContent.Load<Map>($"Spiderbuttons.Graveyards/{mapNum}");
                        editor.ReplaceWith(sourceMap);
                    }
                    catch (Exception ex)
                    {
                        return;
                    }
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsDictionary<string, AudioCueData>();
                    editor.Data[$"{ModManifest.UniqueID}_Xylo"] = new AudioCueData()
                    {
                        Id = $"{ModManifest.UniqueID}_Xylo",
                        FilePaths = [Path.Combine(Helper.DirectoryPath, "assets", "xylo.ogg")],
                        Category = "Sound",
                        StreamedVorbis = true,
                        Looped = false,
                        UseReverb = false
                    };
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.Graveyards/Objects"))
            {
                e.LoadFromModFile<Texture2D>("assets/objects.png", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                Log.Debug("Adding bone items...");
                e.Edit(asset =>
                {
                    var editor = asset.AsDictionary<string, ObjectData>();
                    editor.Data[$"{ModManifest.UniqueID}_SkeletonSkull"] = new ObjectData()
                    {
                        Name = $"{ModManifest.UniqueID}_SkeletonSkull",
                        DisplayName = "Skeleton Skull",
                        Description = "The Bone Lord may be interested in these.",
                        Category = 0,
                        Price = 13,
                        Edibility = -300,
                        Type = "Basic",
                        Texture = "Spiderbuttons.Graveyards/Objects",
                        SpriteIndex = 0,
                        ContextTags = ["Spiderbuttons.Graveyards_Drop"],
                    };
                    editor.Data[$"{ModManifest.UniqueID}_MageSkull"] = new ObjectData()
                    {
                        Name = $"{ModManifest.UniqueID}_MageSkull",
                        DisplayName = "Mage Skull",
                        Description =
                            "A skull from a stronger variety of skeleton. Collect enough of these and the Bone Lord may bestow upon you the ultimate reward...",
                        Category = 0,
                        Price = 31,
                        Edibility = -300,
                        Type = "Basic",
                        Texture = "Spiderbuttons.Graveyards/Objects",
                        SpriteIndex = 1,
                        ContextTags = ["Spiderbuttons.Graveyards_Drop"],
                    };
                    editor.Data[$"{ModManifest.UniqueID}_Xylobone"] = new ObjectData()
                    {
                        Name = $"{ModManifest.UniqueID}_Xylobone",
                        DisplayName = "Xylobone",
                        Description = "For playing spooky tunes!",
                        Category = 0,
                        Price = 0,
                        Edibility = -300,
                        Type = "Basic",
                        Texture = "Spiderbuttons.Graveyards/Objects",
                        SpriteIndex = 2,
                        ContextTags = ["Spiderbuttons.Graveyards_Instrument"],
                    };
                });
            }
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (e.NewLocation is Mine && !MineShaft.activeMines.Any())
            {
                ChooseGraveLevels();
            }
            
            if (e.NewLocation is not MineShaft mine ||
                !mine.Map.Properties.TryGetValue("Spiderbuttons.Graveyards", out _))
                return;
            
            var rng = Utility.CreateDaySaveRandom(Game1.hash.GetDeterministicHashCode(mine.NameOrUniqueName));
            mine.characters.Clear();
            mine.characters.OnValueRemoved += BoneDrops;
            mine.isMonsterArea = true;
            mine.Objects.Clear();
            mine.largeTerrainFeatures.Clear();
            mine.terrainFeatures.Clear();

            mine.mapImageSource.Value = "Maps\\Mines\\mine_quarryshaft";

            mine.fogColor = Color.White * 0.6f;
            mine.ambientFog = true;
            mine.lighting = Color.DimGray;
            Game1.currentLightSources.Clear();

            Layer backLayer = mine.Map.RequireLayer("Back");
            guaranteeSkeletons:
            for (int x = 0; x < backLayer.LayerWidth; x++)
            {
                for (int y = 0; y < backLayer.LayerHeight; y++)
                {
                    var monsterToAdd = rng.NextDouble() < 0.9
                        ? (Monster)new Skeleton(Vector2.Zero, rng.NextDouble() < 0.1)
                        : (Monster)new Bat(Vector2.Zero, 77377);

                    if (mine.getDistanceFromStart(x, y) > 5f && rng.NextDouble() < 0.05)
                    {
                        mine.tryToAddMonster(monsterToAdd, x, y);
                    }

                    if (mine.characters.Count >= 5) return;
                }
            }

            if (mine.characters.Count < 3)
                goto guaranteeSkeletons;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            
            if ((!e.IsMultipleOf(45) && !e.IsMultipleOf(15)) || !Game1.currentLocation.characters.Any(npc => npc is Skeleton))
                return;

            if (Game1.activeClickableMenu is not InstrumentMenu) return;
            
            var rng = Utility.CreateRandom(Game1.currentGameTime.TotalGameTime.TotalMilliseconds);

            foreach (var monster in Game1.currentLocation.characters)
            {
                monster.shakeTimer = 0;
                if (Game1.currentGameTime.TotalGameTime.TotalSeconds - lastNotePlayed < 1.75)
                {
                    if (e.IsMultipleOf(45))
                    {
                        monster.faceDirection(rng.Next(0, 4));
                        monster.Sprite.CurrentFrame += rng.Next(0, 2);
                    }
                    monster.Sprite.CurrentFrame++;
                    if (monster.Sprite.CurrentFrame >= 16) monster.Sprite.CurrentFrame = 0;
                }
                else
                {
                    if (e.IsMultipleOf(120) && rng.NextBool()) monster.faceDirection(rng.Next(0, 4));
                }
            }
        }

        private void BoneDrops(NPC skele)
        {
            if (skele is not Skeleton skeleton) return;
            var position = skele.Tile;
            string itemId = skeleton.isMage.Value
                ? $"(O){ModManifest.UniqueID}_MageSkull"
                : $"(O){ModManifest.UniqueID}_SkeletonSkull";
            Game1.createObjectDebris(itemId, (int)position.X, (int)position.Y, Game1.currentLocation);
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

            if (Game1.activeClickableMenu is InstrumentMenu)
            {
                e.Button.TryGetKeyboard(out var input);
                if (Game1.options.doesInputListContain(Game1.options.menuButton, input))
                {
                    // Game1.activeClickableMenu = null;
                    Game1.player.forceCanMove();
                }

                if (Pitches.TryGetValue(e.Button, out var pitch))
                {
                    if (!Helper.Input.IsDown(SButton.RightShift))
                    {
                        pitch -= 2400;
                    }

                    Helper.Input.Suppress(e.Button);
                    Game1.sounds.PlayLocal($"{ModManifest.UniqueID}_Xylo", Game1.currentLocation, Game1.player.Tile,
                        null,
                        SoundContext.Default, out ICue xylo);
                    xylo.Pitch = pitch / 2400f;
                    lastNotePlayed = Game1.currentGameTime.TotalGameTime.TotalSeconds;
                }

                return;
            }

            if (e.Button is SButton.MouseRight && Game1.player.CurrentItem is not null &&
                Game1.player.CurrentItem.QualifiedItemId.Equals($"(O){ModManifest.UniqueID}_Xylobone",
                    StringComparison.OrdinalIgnoreCase))
            {
                Game1.activeClickableMenu = new InstrumentMenu();
            }


            if (e.Button is SButton.F5)
            {
                foreach (var mon in Game1.currentLocation.characters)
                {
                    if (mon is Skeleton skele)
                    {
                        // foreach (var anim in skele.Sprite.animation)
                    }
                }
            }

            if (e.Button is SButton.F6)
            {
                Helper.GameContent.InvalidateCache("Maps/Mines");
                Helper.GameContent.InvalidateCache("Data/Objects");
            }
        }
    }
}
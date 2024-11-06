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
using StardewValley.GameData.Characters;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
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

        internal static Dictionary<string, string> randomNames = new();

        internal static readonly Dictionary<SButton, int> Pitches = new()
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
            Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;

            GameLocation.RegisterTileAction("Spiderbuttons.Graveyards_Headstone", RandomTombstone);
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            //
        }
        
        private bool RandomTombstone(GameLocation location, string[] args, Farmer player, Point point)
        {
            if (!randomNames.TryGetValue(point.ToString(), out var name))
            {
                var randomName = Dialogue.randomName();
                randomNames[point.ToString()] = randomName;
                name = randomName;
            }
            
            Game1.drawDialogueNoTyping($"Here Lies {name}");
            return true;
        }

        private void ChooseGraveLevels()
        {
            randomNames.Clear();
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

            if (Game1.season == Season.Fall && Game1.dayOfMonth == 22)
            {
                if (Game1.player.mailReceived.Contains($"{ModManifest.UniqueID}_ArrivalDwarvish"))
                {
                    Game1.player.mailReceived.Remove($"{ModManifest.UniqueID}_ArrivalDwarvish");
                }
                if (Game1.player.mailReceived.Contains($"{ModManifest.UniqueID}_Arrival"))
                {
                    Game1.player.mailReceived.Remove($"{ModManifest.UniqueID}_Arrival");
                }

                if (Game1.player.canUnderstandDwarves)
                {
                    Game1.addMail($"{ModManifest.UniqueID}_Arrival");
                }
                else
                {
                    Game1.addMail($"{ModManifest.UniqueID}_ArrivalDwarvish");
                }
            }

            if (Game1.season == Season.Fall && Game1.dayOfMonth >= 22 && Game1.dayOfMonth <= 28)
            {
                var dwarf = Game1.getCharacterFromName("Dwarf");
                dwarf.IsInvisible = true;
                dwarf.daysUntilNotInvisible = 1;

                Map map = Game1.getLocationFromName("Mine").Map;
                Layer layer = map.GetLayer("Buildings");
                Tile tile = GetTile(map, "Buildings", 43, 6);
                if (tile is not null)
                {
                    tile = GetTile(map, "Buildings", 43, 6);
                    tile.Properties["Action"] = $"OpenShop {ModManifest.UniqueID}_BoneLord down";
                }
                else
                {
                    layer.Tiles[43, 6] = new StaticTile(
                        layer: layer,
                        tileSheet: map.GetTileSheet("untitled tile sheet"),
                        tileIndex: 256,
                        blendMode: BlendMode.Alpha
                    );

                    tile = GetTile(map, "Buildings", 43, 6);
                    tile.Properties["Action"] = $"OpenShop {ModManifest.UniqueID}_BoneLord down";
                }
            }
            else
            {
                var boneLord = Game1.getCharacterFromName($"{ModManifest.UniqueID}_BoneLord");
                boneLord.IsInvisible = true;
                boneLord.daysUntilNotInvisible = 1;

                Tile tile = GetTile(Game1.getLocationFromName("Mine").Map, "Buildings", 43, 6);
                tile?.Properties.Remove("Action");
            }
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.Graveyards/1"))
            {
                e.LoadFromModFile<Map>("assets/Maps/Graveyard1.tmx", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.Graveyards/2"))
            {
                e.LoadFromModFile<Map>("assets/Maps/Graveyard2.tmx", AssetLoadPriority.Exclusive);
            }
            
            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.Graveyards/3"))
            {
                e.LoadFromModFile<Map>("assets/Maps/Graveyard3.tmx", AssetLoadPriority.Exclusive);
            }
            
            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.Graveyards/4"))
            {
                e.LoadFromModFile<Map>("assets/Maps/Graveyard4.tmx", AssetLoadPriority.Exclusive);
            }
            
            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.Graveyards/5"))
            {
                e.LoadFromModFile<Map>("assets/Maps/Graveyard5.tmx", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"Characters/{ModManifest.UniqueID}_BoneLord"))
            {
                e.LoadFromModFile<Texture2D>("assets/BoneLord/BoneLord_Spritesheet.png", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"Portraits/{ModManifest.UniqueID}_BoneLord"))
            {
                e.LoadFromModFile<Texture2D>("assets/BoneLord/BoneLord_Portrait.png", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Characters"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsDictionary<string, CharacterData>();
                    editor.Data[$"{ModManifest.UniqueID}_BoneLord"] = new CharacterData()
                    {
                        DisplayName = "Bone Lord",
                        CanSocialize = "FALSE",
                        CanReceiveGifts = false,
                        CanGreetNearbyCharacters = false,
                        Calendar = CalendarBehavior.HiddenAlways,
                        SocialTab = SocialTabBehavior.HiddenAlways,
                        IntroductionsQuest = false,
                        ItemDeliveryQuests = "FALSE",
                        PerfectionScore = false,
                        EndSlideShow = EndSlideShowBehavior.Hidden,
                        WinterStarParticipant = "FALSE",
                        Home = new List<CharacterHomeData>()
                        {
                            new CharacterHomeData()
                            {
                                Id = "HalloweenSeason",
                                Location = "Mine",
                                Tile = new Point(43, 6),
                                Direction = "down",
                            },
                        },
                        Breather = false,
                    };
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"Characters/Dialogue/{ModManifest.UniqueID}_BoneLord"))
            {
                e.LoadFromModFile<Dictionary<string, string>>("assets/BoneLord/BoneLord_Dialogue.json",
                    AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo($"Data/NPCGiftTastes"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsDictionary<string, string>();
                    editor.Data[$"{ModManifest.UniqueID}_BoneLord"] =
                        "Hey, I really love this stuff. You can find great things in the mines./554 60 62 64 66 68 70 749 162/Ah, this reminds me of home./78 82 84 86 96 97 98 99 121 122/Hmm... Is this what humans like?/-5 16 -81 330/I don't care what species you are. This is worthless garbage.//An offering! Thank you./-6 -28/";
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsDictionary<string, ShopData>();
                    editor.Data[$"{ModManifest.UniqueID}_BoneLord"] = new ShopData()
                    {
                        Currency = 0,
                        OpenSound = "skeletonDie",
                        PurchaseSound = "skeletonHit",
                        Owners = new List<ShopOwnerData>()
                        {
                            new ShopOwnerData()
                            {
                                Id = $"{ModManifest.UniqueID}_BoneLord",
                                Name = $"{ModManifest.UniqueID}_BoneLord",
                                Dialogues = new List<ShopDialogueData>()
                                {
                                    new ShopDialogueData()
                                    {
                                        Id = "Default",
                                        Dialogue = "Has my kind been giving you trouble?",
                                    }
                                }
                            }
                        },
                        Items = new List<ShopItemData>()
                        {
                            new ShopItemData()
                            {
                                TradeItemId = $"{ModManifest.UniqueID}_SkeletonSkull",
                                TradeItemAmount = 5,
                                AvailableStockLimit = LimitedStockMode.Player,
                                Id = "RandomBones",
                                ItemId = "RANDOM_ITEMS (O)",
                                MaxItems = 5,
                                PerItemCondition = "ITEM_CONTEXT_TAG Target bone_item"
                            },
                            new ShopItemData()
                            {
                                TradeItemId = $"{ModManifest.UniqueID}_SkeletonSkull",
                                TradeItemAmount = 20,
                                AvailableStockLimit = LimitedStockMode.Player,
                                Id = "RandomArtifacts",
                                ItemId = "RANDOM_ITEMS (O)",
                                MaxItems = 2,
                                AvoidRepeat = true,
                                PerItemCondition = "ITEM_OBJECT_TYPE Target Arch"
                            },
                            new ShopItemData()
                            {
                                TradeItemId = $"{ModManifest.UniqueID}_MageSkull",
                                TradeItemAmount = 10,
                                AvailableStock = 1,
                                AvailableStockLimit = LimitedStockMode.Player,
                                Id = "MageTrade",
                                ItemId = $"(O){ModManifest.UniqueID}_Xylobone",
                                MaxItems = 1,
                            },
                        }
                    };
                });
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
                        int mapNum = graveRandom.Next(1, 6);
                        Map sourceMap = Helper.GameContent.Load<Map>($"Spiderbuttons.Graveyards/{mapNum}");
                        editor.ReplaceWith(sourceMap);
                    }
                    catch (Exception ex)
                    {
                        //
                    }
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Maps/Mine") && Game1.season is Season.Fall &&
                Game1.dayOfMonth >= 22 && Game1.dayOfMonth <= 28)
            {
                Log.Debug("Changing mine");
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    Map map = editor.Data;
                    Layer layer = map.GetLayer("Buildings");
                    layer.Tiles[43, 6] = new StaticTile(
                        layer: layer,
                        tileSheet: map.GetTileSheet("untitled tile sheet"),
                        tileIndex: 256,
                        blendMode: BlendMode.Alpha
                    );

                    Tile tile = GetTile(map, "Buildings", 43, 6);
                    tile.Properties["Action"] = $"OpenShop {ModManifest.UniqueID}_BoneLord down";
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

            if (e.NameWithoutLocale.IsEquivalentTo("Data/mail"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsDictionary<string, string>();
                    editor.Data[$"{ModManifest.UniqueID}_Arrival"] =
                        "A friend will be taking my place in the mines this week. Please treat him as you've treated me: like a friend.[#]The Bone Lord Cometh";
                    editor.Data[$"{ModManifest.UniqueID}_ArrivalDwarvish"] =
                        "O hteup yenn du nomel mol notu e doo meus nhes yuum. Rnuosu ntuon hem os olai'xu ntuonup mu nemu o hteup.[#]Vhu Dau Natp Eamunh";
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Spiderbuttons.Graveyards/Objects"))
            {
                e.LoadFromModFile<Texture2D>("assets/objects.png", AssetLoadPriority.Exclusive);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
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

            if ((!e.IsMultipleOf(45) && !e.IsMultipleOf(15)) ||
                !Game1.currentLocation.characters.Any(npc => npc is Skeleton))
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
                string test =
                    "A friend will be taking my place in the mines this week. Please treat him as you've treated me: like a friend. The Bone Lord Cometh";
                Log.Debug(Dialogue.convertToDwarvish(test));
            }

            if (e.Button is SButton.F6)
            {
                Helper.GameContent.InvalidateCache("Data/Shops");
                Helper.GameContent.InvalidateCache("Data/Characters");
            }
        }
    }
}
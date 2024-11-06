using GenericModConfigMenu;
using StardewModdingAPI;

namespace SkeletonWar.Config;

public sealed class ModConfig
{
    public bool Enabled { get; set; } = true;

    public ModConfig()
    {
        Init();
    }

    private void Init()
    {
        this.Enabled = true;
    }

    public void SetupConfig(IGenericModConfigMenuApi configMenu, IManifest ModManifest, IModHelper Helper)
    {
        configMenu.Register(
            mod: ModManifest,
            reset: Init,
            save: () => Helper.WriteConfig(this)
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Enabled",
            tooltip: () => "Enable or disable this mod.",
            getValue: () => this.Enabled,
            setValue: value => this.Enabled = value
        );
    }
}
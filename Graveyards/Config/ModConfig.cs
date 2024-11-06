using GenericModConfigMenu;
using Graveyards;
using StardewModdingAPI;

namespace Graveyards.Config;

public sealed class ModConfig
{
    public int SkeletonMinimum { get; set; } = 3;
    public int SkeletonMaximum { get; set; } = 5;
    public int BoneItemPrice { get; set; } = 5;
    public int ArtifactPrice { get; set; } = 20;
    public int XylobonePrice { get; set; } = 10;

    public ModConfig()
    {
        Init();
    }

    private void Init()
    {
        this.BoneItemPrice = 5;
        this.ArtifactPrice = 20;
        this.XylobonePrice = 10;
    }

    public void SetupConfig(IGenericModConfigMenuApi configMenu, IManifest ModManifest, IModHelper Helper)
    {
        configMenu.Register(
            mod: ModManifest,
            reset: Init,
            save: () => Helper.WriteConfig(this)
        );
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: i18n.SkeletonMinName,
            tooltip: i18n.SkeletonMinTooltip,
            getValue: () => this.SkeletonMinimum,
            setValue: value =>
            {
                this.SkeletonMinimum = value > this.SkeletonMaximum ? this.SkeletonMaximum : value;
            }
        );
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: i18n.SkeletonMaxName,
            tooltip: i18n.SkeletonMaxTooltip,
            getValue: () => this.SkeletonMaximum,
            setValue: value =>
            {
                this.SkeletonMaximum = value < this.SkeletonMinimum ? this.SkeletonMinimum : value;
            }
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: i18n.BoneItemName,
            tooltip: i18n.BoneItemTooltip,
            getValue: () => this.BoneItemPrice,
            setValue: value => this.BoneItemPrice = value
        );
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: i18n.ArtifactName,
            tooltip: i18n.ArtifactTooltip,
            getValue: () => this.ArtifactPrice,
            setValue: value => this.ArtifactPrice = value
        );
        
        configMenu.AddNumberOption(
            mod: ModManifest,
            name: i18n.XylobonePriceName,
            tooltip: i18n.XylobonePriceTooltip,
            getValue: () => this.XylobonePrice,
            setValue: value => this.XylobonePrice = value
        );
    }
}
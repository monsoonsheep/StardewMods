using StardewMods.VisitorsMod.Framework.Data.Models;
using StardewMods.VisitorsMod.Framework.Data.Models.Activities;
using StardewMods.VisitorsMod.Framework.Data.Models.Appearances;

namespace StardewMods.VisitorsMod.Framework.Services;

internal class Content
{
    internal static Content Instance = null!;

    // Custom visitors
    internal Dictionary<string, VisitorModel> visitorModels = [];

    // Activities
    internal Dictionary<string, ActivityModel> activities = [];

    // Appearance parts
    internal Dictionary<string, HairModel> Hairstyles = [];
    internal Dictionary<string, ShirtModel> Shirts = [];
    internal Dictionary<string, PantsModel> Pants = [];
    internal Dictionary<string, ShoesModel> Shoes = [];
    internal Dictionary<string, AccessoryModel> Accessories = [];
    internal Dictionary<string, OutfitModel> Outfits = [];

    internal Content()
        => Instance = this;

    internal void Initialize()
    {
        IContentPack defaultContent = Mod.Helper.ContentPacks.CreateTemporary(
                   Path.Combine(Mod.Helper.DirectoryPath, "assets", "DefaultContent"),
                   $"{Mod.Manifest.Author}.DefaultContent",
                   "VisitorsMod Fake Content Pack",
                   "Default content for VisitorsMod",
                   Mod.Manifest.Author,
                   Mod.Manifest.Version
                );

        // Load default content pack included in assets folder
        this.LoadContentPack(defaultContent);

        // Load content packs
        foreach (IContentPack contentPack in Mod.Helper.ContentPacks.GetOwned())
            this.LoadContentPack(contentPack);
    }

    private void LoadContentPack(IContentPack contentPack)
    {
        Log.Info($"Loading content pack {contentPack.Manifest.Name} by {contentPack.Manifest.Author}");

        DirectoryInfo visitorsRoot = new(Path.Combine(contentPack.DirectoryPath, "Visitors"));

        DirectoryInfo activitiesRoot = new(Path.Combine(contentPack.DirectoryPath, "Activities"));

        DirectoryInfo hairsRoot = new(Path.Combine(contentPack.DirectoryPath, "Hairstyles"));
        DirectoryInfo shirtsRoot = new(Path.Combine(contentPack.DirectoryPath, "Shirts"));
        DirectoryInfo pantsRoot = new(Path.Combine(contentPack.DirectoryPath, "Pants"));
        DirectoryInfo shoesRoot = new(Path.Combine(contentPack.DirectoryPath, "Shoes"));
        DirectoryInfo accessoriesRoot = new(Path.Combine(contentPack.DirectoryPath, "Accessories"));
        DirectoryInfo outfitsRoot = new(Path.Combine(contentPack.DirectoryPath, "Outfits"));

        if (visitorsRoot.Exists)
        {
            DirectoryInfo[] visitorFolders = visitorsRoot.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load visitor models
            foreach (DirectoryInfo modelFolder in visitorFolders)
            {
                string relativePathOfModel = Path.Combine("Visitors", modelFolder.Name);
                VisitorModel? model = contentPack.ReadJsonFile<VisitorModel>(Path.Combine(relativePathOfModel, "visitor.json"));
                if (model == null)
                {
                    Log.Debug("Couldn't read visitor.json for content pack");
                    continue;
                }
                model.Name = $"{contentPack.Manifest.UniqueID}/{model.Name}";
                model.Spritesheet = contentPack.ModContent.GetInternalAssetName(Path.Combine(relativePathOfModel, "visitor.png")).Name;

                string portraitPath = Path.Combine(relativePathOfModel, "portrait.png");

                model.Portrait = contentPack.HasFile(portraitPath)
                    ? contentPack.ModContent.GetInternalAssetName(portraitPath).Name
                    : Mod.Helper.ModContent.GetInternalAssetName(Path.Combine("assets", "CharGen", "Portraits", (string.IsNullOrEmpty(model.Portrait) ? "cat" : model.Portrait) + ".png")).Name;

                Log.Trace($"Visitor model added: {model.Name}");
                this.visitorModels[model.Name] = model;
            }
        }

        if (activitiesRoot.Exists)
        {
            DirectoryInfo[] activityFolders = activitiesRoot.GetDirectories("*", SearchOption.TopDirectoryOnly);

            // Load visitor models
            foreach (DirectoryInfo folder in activityFolders)
            {
                string relativePathOfModel = Path.Combine("Activities", folder.Name);

                ActivityModel? activity = contentPack.ReadJsonFile<ActivityModel>(Path.Combine(relativePathOfModel, "activity.json"));
                if (activity == null)
                {
                    Log.Debug("Couldn't read activity.json for content pack");
                    continue;
                }
                activity.Id = folder.Name;

                Log.Trace($"Activity model added: {activity.Id}");
                this.activities[activity.Id] = activity;
            }
        }

        if (hairsRoot.Exists)
            ReadAppearanceFolder<HairModel>(contentPack, hairsRoot);
        if (shirtsRoot.Exists)
            ReadAppearanceFolder<ShirtModel>(contentPack, shirtsRoot);
        if (pantsRoot.Exists)
            ReadAppearanceFolder<PantsModel>(contentPack, pantsRoot);
        if (shoesRoot.Exists)
            ReadAppearanceFolder<ShoesModel>(contentPack, shoesRoot);
        if (accessoriesRoot.Exists)
            ReadAppearanceFolder<AccessoryModel>(contentPack, accessoriesRoot);
        if (outfitsRoot.Exists)
            ReadAppearanceFolder<OutfitModel>(contentPack, outfitsRoot);

        void ReadAppearanceFolder<TAppearance>(IContentPack contentPack, DirectoryInfo rootFolder) where TAppearance : AppearanceModel
        {
            DirectoryInfo[] modelFolders = rootFolder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo modelFolder in modelFolders)
            {
                AppearanceModel? model = LoadAppearanceModel<TAppearance>(contentPack, modelFolder.Name);
                if (model != null)
                    this.GetModelCollection<TAppearance>()[model.Id] = (TAppearance)model;
            }
        }

        TAppearance? LoadAppearanceModel<TAppearance>(IContentPack contentPack, string modelName) where TAppearance : AppearanceModel
        {
            string filename = typeof(TAppearance).Name switch
            {
                nameof(HairModel) => "hair",
                nameof(ShirtModel) => "shirt",
                nameof(PantsModel) => "pants",
                nameof(OutfitModel) => "outfit",
                nameof(ShoesModel) => "shoes",
                nameof(AccessoryModel) => "accessory",
                _ => throw new ArgumentOutOfRangeException()
            };

            string folderName = typeof(TAppearance).Name switch
            {
                nameof(HairModel) => "Hairstyles",
                nameof(ShirtModel) => "Shirts",
                nameof(PantsModel) => "Pants",
                nameof(OutfitModel) => "Outfits",
                nameof(ShoesModel) => "Shoes",
                nameof(AccessoryModel) => "Accessories",
                _ => throw new ArgumentOutOfRangeException()
            };

            string relativePathOfModel = Path.Combine(folderName, modelName);
            TAppearance? model = contentPack.ReadJsonFile<TAppearance>(Path.Combine(relativePathOfModel, $"{filename}.json"));
            if (model == null)
            {
                Log.Debug($"Couldn't read {filename}.json for content pack {contentPack.Manifest.UniqueID}");
                return null;
            }

            model.Id = $"{contentPack.Manifest.UniqueID}/{modelName}";
            model.TexturePath = contentPack.ModContent.GetInternalAssetName(Path.Combine(relativePathOfModel, $"{filename}.png")).Name;
            model.ContentPack = contentPack;

            Log.Trace($"{folderName} model added: {model.Id}");

            return model;
        }
    }

    internal IDictionary<string, TAppearance> GetModelCollection<TAppearance>() where TAppearance : AppearanceModel
    {
        return (typeof(TAppearance).Name switch
        {
            nameof(HairModel) => this.Hairstyles as IDictionary<string, TAppearance>,
            nameof(ShirtModel) => this.Shirts as IDictionary<string, TAppearance>,
            nameof(PantsModel) => this.Pants as IDictionary<string, TAppearance>,
            nameof(OutfitModel) => this.Outfits as IDictionary<string, TAppearance>,
            nameof(ShoesModel) => this.Shoes as IDictionary<string, TAppearance>,
            nameof(AccessoryModel) => this.Accessories as IDictionary<string, TAppearance>,
            _ => throw new ArgumentOutOfRangeException(nameof(TAppearance), "Bad type given. How has this occurred?")
        })!;
    }
}

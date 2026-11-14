using System.IO;
using DevInterface;
using ModPathPair = (string modName, string basePath);

namespace RegionKit.Modules.DevUIMisc;

internal static class DecalPreview
{
	private static Dictionary<string, string> decalSources = new Dictionary<string, string>();

	public static string GetDecalSource(string decal) => decalSources.TryGetValue(decal.ToLowerInvariant().Replace('/', Path.DirectorySeparatorChar), out string source) ? source : "UNKNOWN";

	public static void Enable()
	{
		On.DevInterface.Panel.Update += Panel_Update;
		On.DevInterface.ObjectsPage.ctor += ObjectsPage_ctor;

		// Fixes exception when trying to load decal that doesn't exist, tought i could throw that in
		On.CustomDecal.LoadFile += CustomDecal_LoadFile;
	}

	public static void Disable()
	{
		On.DevInterface.Panel.Update -= Panel_Update;
		On.DevInterface.ObjectsPage.ctor -= ObjectsPage_ctor;

		On.CustomDecal.LoadFile -= CustomDecal_LoadFile;
	}

	private static void Panel_Update(On.DevInterface.Panel.orig_Update orig, Panel self)
	{
		orig(self);

		if (self is not CustomDecalRepresentation.SelectDecalPanel) return;
		if ((self as CustomDecalRepresentation.SelectDecalPanel)!.parentNode is not CustomDecalRepresentation.CustomDecalControlPanel) return;

		foreach (var subNode in self.subNodes)
		{
			if (subNode is Button hoveredButton && hoveredButton.MouseOver)
			{
				string decalName = hoveredButton.IDstring;

				if (decalName == "BackPage99289..?/~") continue;
				if (decalName == "NextPage99289..?/~") continue;

				var decalPreviewOverlay = self.Page.subNodes.Find(x => x is DecalPreviewOverlay) as DecalPreviewOverlay;
				if (decalPreviewOverlay == null) return;

				decalPreviewOverlay.SetDecal(decalName);
				decalPreviewOverlay.SetVisible();

				return;
			}
		}
	}

	private static void ObjectsPage_ctor(On.DevInterface.ObjectsPage.orig_ctor orig, ObjectsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
	{
		orig(self, owner, IDstring, parentNode, name);

		self.subNodes.Add(new DecalPreviewOverlay(owner, "DecalPreviewOverlay", self));

		GetDecalSources();
	}

	private static void CustomDecal_LoadFile(On.CustomDecal.orig_LoadFile orig, CustomDecal self, string fileName)
	{
		if (Futile.atlasManager.GetAtlasWithName(fileName) == null && !AnimatedDecals.VideoManager.IsVideoFile(fileName))
		{
			string decalPath = AssetManager.ResolveFilePath("Decals" + Path.DirectorySeparatorChar.ToString() + fileName + ".png");

			if (!File.Exists(decalPath))
			{
				(self.placedObject.data as PlacedObject.CustomDecalData)!.imageName = "ph";
				fileName = "ph";
			}
		}

		orig(self, fileName);
	}

	private static void GetDecalSources()
	{
		decalSources.Clear();

		List<ModPathPair> modsToFolders = [];
		for (int i = ModManager.ActiveMods.Count - 1; i >= 0; i--)
		{
			string modName = ModManager.ActiveMods[i].name;
			if (ModManager.ActiveMods[i].hasTargetedVersionFolder)
			{
				modsToFolders.Add((modName, ModManager.ActiveMods[i].TargetedPath));
			}
			if (ModManager.ActiveMods[i].hasNewestFolder)
			{
				modsToFolders.Add((modName, ModManager.ActiveMods[i].NewestPath));
			}
			modsToFolders.Add((modName, ModManager.ActiveMods[i].path));
		}
		modsToFolders.Add(("Vanilla", Custom.RootFolderDirectory()));

		foreach ((string modName, string basePath) in modsToFolders)
		{
			string decalFolder = Path.Combine(basePath, "decals");
			if (Directory.Exists(decalFolder))
			{
				RecursivelyAddDecals(decalFolder, "", modName);
			}
		}

		// Helper
		static void RecursivelyAddDecals(string directory, string basePath, string modName)
		{
			basePath = basePath.ToLowerInvariant();
			foreach (var subdir in Directory.EnumerateDirectories(directory))
			{
				RecursivelyAddDecals(subdir, basePath + Path.GetFileName(subdir) + Path.DirectorySeparatorChar.ToString(), modName);
			}
			foreach (var file in Directory.EnumerateFiles(directory))
			{
				if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
				{
					decalSources[(basePath + Path.GetFileNameWithoutExtension(file)).ToLowerInvariant()] = modName;
				}
				else if (AnimatedDecals.VideoManager.IsVideoFile(file))
				{
					decalSources[(basePath + Path.GetFileName(file)).ToLowerInvariant()] = modName;
				}
			}
		}
	}

	public class DecalPreviewOverlay : DevUINode
	{
		private string? decalName;

		private FSprite overlaySprite;
		private FSprite decalSizeSprite;
		private FSprite decalSprite;
		private FLabel infoLabel;

		// Kinda hacky solution but should work
		private int visabilityTimer;
		private bool isVisible
		{
			get
			{
				return visabilityTimer > 0;
			}
			set
			{
				if (value)
				{
					visabilityTimer = 5;
				}
				else
				{
					visabilityTimer = 0;
				}
			}
		}

		public DecalPreviewOverlay(DevUI owner, string IDstring, DevUINode parentNode) : base(owner, IDstring, parentNode)
		{
			// Overlay
			overlaySprite = new FSprite("pixel")
			{
				anchorX = 0f,
				anchorY = 0f,
				scaleX = 400f,
				scaleY = 400f,
				color = new Color(0f, 0f, 0f),
				alpha = 0.5f
			};

			fSprites.Add(overlaySprite);
			Futile.stage.AddChild(overlaySprite);


			// Size display ig?
			decalSizeSprite = new FSprite("pixel")
			{
				anchorX = 0.5f,
				anchorY = 0.5f,
				x = 200f,
				y = 200f,
				scaleX = 1f,
				scaleY = 1f,
				color = new Color(1f, 1f, 1f),
				alpha = 0.2f
			};

			fSprites.Add(decalSizeSprite);
			Futile.stage.AddChild(decalSizeSprite);


			// Decal image
			decalSprite = new FSprite("pixel")
			{
				anchorX = 0.5f,
				anchorY = 0.5f,
				x = 200, 
				y = 200
			};

			fSprites.Add(decalSprite);
			Futile.stage.AddChild(decalSprite);


			// Info label
			infoLabel = new FLabel(GetFont(), "")
			{
				anchorX = 0f,
				anchorY = 0f,
				x = 10.01f,
				y = 10.01f
			};

			fLabels.Add(infoLabel);
			Futile.stage.AddChild(infoLabel);
		}

		public override void Update()
		{
			base.Update();

			if (decalName != null && isVisible)
			{
				if (Futile.atlasManager.GetAtlasWithName(decalName) != null)
				{
					decalSprite.SetElementByName(decalName);

					float longestSide = Math.Max(decalSprite.textureRect.width, decalSprite.textureRect.height);
					decalSprite.scale = 300f / longestSide;

					decalSizeSprite.scaleX = decalSprite.width;
					decalSizeSprite.scaleY = decalSprite.height;

					infoLabel.text = $"Source: {GetDecalSource(decalName)}    Size: {decalSprite.textureRect.width}x{decalSprite.textureRect.height}";
				}
				else
				{
					infoLabel.text = $"Source: {GetDecalSource(decalName)}";
					isVisible = false; // todo: what should it look like if we can't load the decal
				}
			}

			overlaySprite.isVisible = isVisible;
			decalSizeSprite.isVisible = isVisible;
			decalSprite.isVisible = isVisible;
			infoLabel.isVisible = isVisible;

			if (isVisible)
			{
				overlaySprite.MoveToFront();
				decalSizeSprite.MoveToFront();
				decalSprite.MoveToFront();
				infoLabel.MoveToFront();

				visabilityTimer--;
			}
		}

		public void SetVisible()
		{
			visabilityTimer = 5;
		}

		public void SetDecal(string decalName) 
		{
			LoadFile(decalName);
			this.decalName = decalName;
		}

		// Code "borrowed" from CustomDecal.LoadFile
		public void LoadFile(string fileName)
		{
			if (Futile.atlasManager.GetAtlasWithName(fileName) != null)
			{
				return;
			}

			if (AnimatedDecals.VideoManager.IsVideoFile(fileName))
			{
				AnimatedDecals.VideoManager.LoadAndCacheVideo(fileName, AssetManager.ResolveFilePath(Path.Combine("Decals", fileName)));
				return;
			}

			string str = AssetManager.ResolveFilePath(Path.Combine("Decals", fileName + ".png"));
			if (!File.Exists(str)) return;
			var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + str, true, true);
			HeavyTexturesCache.LoadAndCacheAtlasFromTexture(fileName, texture, false);
		}
	}
}

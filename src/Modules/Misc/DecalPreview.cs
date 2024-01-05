using System.IO;
using DevInterface;
using Newtonsoft.Json.Linq;

namespace RegionKit.Modules.Misc;

internal static class DecalPreview
{
	private static Dictionary<string, string> decalSources = new Dictionary<string, string>();

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

		foreach (var subNode in self.subNodes)
		{
			if (subNode is Button hoveredButton && hoveredButton.MouseOver)
			{
				string decalName = hoveredButton.IDstring;

				if (decalName == "BackPage99289..?/~") continue;
				if (decalName == "NextPage99289..?/~") continue;

				DecalPreviewOverlay? decalPreviewOverlay = self.Page.subNodes.Find(x => x is DecalPreviewOverlay) as DecalPreviewOverlay;
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
		if (Futile.atlasManager.GetAtlasWithName(fileName) == null)
		{
			string decalPath = AssetManager.ResolveFilePath("Decals" + Path.DirectorySeparatorChar.ToString() + fileName + ".png");

			if (!File.Exists(decalPath))
			{
				(self.placedObject.data as PlacedObject.CustomDecalData).imageName = "ph";
				fileName = "ph";
			}
		}

		orig(self, fileName);
	}

	private static void GetDecalSources()
	{
		decalSources.Clear();
		string[] array = AssetManager.ListDirectory("decals", false, false);

		HashSet<string> decalDirectories = new HashSet<string>();

		for (int i = 0; i < array.Length; i++)
		{
			if (Directory.GetParent(array[i]).Parent.Name == "streamingassets")
			{
				decalSources[Path.GetFileNameWithoutExtension(array[i])] = "Vanilla";
				continue;
			}

			decalDirectories.Add(Directory.GetParent(array[i]).Parent.FullName);
		}

		Dictionary<string, string> pathToModName = new Dictionary<string, string>();
		foreach (var directory in decalDirectories)
		{
			JObject modinfoJson = JObject.Parse(File.ReadAllText(Path.Combine(directory, "modinfo.json")));
			pathToModName[directory] = (string)modinfoJson["name"];
		}

		for (int i = 0; i < array.Length; i++)
		{
			if (!decalSources.ContainsKey(Path.GetFileNameWithoutExtension(array[i])))
			{
				decalSources[Path.GetFileNameWithoutExtension(array[i])] = pathToModName[Directory.GetParent(array[i]).Parent.FullName];
			}
		}
	}

	public class DecalPreviewOverlay : DevUINode
	{
		private string decalName;

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

			this.fSprites.Add(overlaySprite);
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

			this.fSprites.Add(decalSizeSprite);
			Futile.stage.AddChild(decalSizeSprite);


			// Decal image
			decalSprite = new FSprite("pixel")
			{
				anchorX = 0.5f,
				anchorY = 0.5f,
				x = 200, 
				y = 200
			};

			this.fSprites.Add(decalSprite);
			Futile.stage.AddChild(decalSprite);


			// Info label
			infoLabel = new FLabel(GetFont(), "")
			{
				anchorX = 0f,
				anchorY = 0f,
				x = 10.01f,
				y = 10.01f
			};

			this.fLabels.Add(infoLabel);
			Futile.stage.AddChild(infoLabel);
		}

		public override void Update()
		{
			base.Update();

			if (isVisible && Futile.atlasManager.GetAtlasWithName(decalName) != null)
			{
				decalSprite.SetElementByName(decalName);

				float longestSide = Math.Max(decalSprite.textureRect.width, decalSprite.textureRect.height);
				decalSprite.scale = 300f / longestSide;

				decalSizeSprite.scaleX = decalSprite.width;
				decalSizeSprite.scaleY = decalSprite.height;

				infoLabel.text = $"Source: {decalSources[decalName]}    Size: {decalSprite.textureRect.width}x{decalSprite.textureRect.height}";
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
			string str = AssetManager.ResolveFilePath("Decals" + Path.DirectorySeparatorChar.ToString() + fileName + ".png");
			Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + str, true, true);
			HeavyTexturesCache.LoadAndCacheAtlasFromTexture(fileName, texture, false);
		}
	}
}

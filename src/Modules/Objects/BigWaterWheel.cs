using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Watcher;

namespace RegionKit.Modules.Objects
{
	public class BigWaterWheel : UpdatableAndDeletable
	{
		private readonly PlacedObject pObj;
		public Data data;
		private float speed;

		private DynamicLevelElement[] levelElements;

		public BigWaterWheel(PlacedObject pObj, Room room)
		{
			this.pObj = pObj;
			data = (pObj.data as Data)!;
			this.room = room;

			float rotation = data.rotation;

			float scale = 36f; // 0.05f * 720 (size of image) I guess to match the spinning fans

			levelElements = new DynamicLevelElement[10];
			for (int i = levelElements.Length - 1; i >= 0; i--)
			{
				string assetName = i switch
				{
					0 => "Big Water Wheel",
					9 => "Big Water Wheel 3",
					_ => "Big Water Wheel 2"
				};

				if (!Futile.atlasManager.DoesContainAtlas(assetName))
				{
					Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
					string path = AssetManager.ResolveFilePath("Illustrations" + Path.DirectorySeparatorChar.ToString() + assetName + ".png");
					AssetManager.SafeWWWLoadTexture(ref tex, "file:///" + path, true, true);
					HeavyTexturesCache.LoadAndCacheAtlasFromTexture(assetName, tex, false);
				}

				int depth = Mathf.FloorToInt(data.depth * 30f) + i;
				levelElements[i] = new DynamicLevelElement(this.pObj.pos, new Vector2(scale, scale), Futile.atlasManager.GetAtlasWithName(assetName).texture, depth)
				{
					rotation = rotation
				};
				room.AddObject(levelElements[i]);
			}
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			if (slatedForDeletetion || room != room.game.cameras[0].room) return;

			float newSpeed = Mathf.Lerp(-5f, 5f, data.speed);
			if (room.world.rainCycle.brokenAntiGrav != null)
			{
				newSpeed = (room.world.rainCycle.brokenAntiGrav.CurrentLightsOn > 0f) ? newSpeed : 0f;
			}
			speed = Custom.LerpAndTick(speed, newSpeed, 0.035f, 0.0008f);

			int baseDepth = Mathf.FloorToInt(data.depth * 30f);
			for (int i = 0; i < levelElements.Length; i++)
			{
				levelElements[i].pos = room.game.cameras[0].ApplyDepthWithCangle(pObj.pos, baseDepth + i);
				levelElements[i].rotation += speed * 0.1f;
				levelElements[i].setDepthOffset = baseDepth + i;
			}
		}

		public class Data : PlacedObject.Data
		{
			public Data(PlacedObject owner) : base(owner)
			{
				panelPos = new Vector2(0f, 100f);
				speed = 0.55f;
				depth = 6f / 30f;
				rotation = UnityEngine.Random.Range(0, 360f);
			}

			protected string BaseSaveString()
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}~{1}~{2}~{3}~{4}",
				[
				panelPos.x,
				panelPos.y,
				speed,
				depth,
				rotation
				]);
			}

			public override string ToString()
			{
				string text = BaseSaveString();
				text = SaveState.SetCustomData(this, text);
				return SaveUtils.AppendUnrecognizedStringAttrs(text, "~", unrecognizedAttributes);
			}

			public override void FromString(string s)
			{
				string[] array = Regex.Split(s, "~");
				int i = 0;
				if (array.Length > i) panelPos.x = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (array.Length > i) panelPos.y = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (array.Length > i) speed = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (array.Length > i) depth = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (array.Length > i) rotation = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, i);
			}

			public Vector2 panelPos;
			public float speed;
			public float depth;
			public float rotation;
		}
	}
}

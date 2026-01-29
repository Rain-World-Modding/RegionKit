using System.Globalization;
using System.Text.RegularExpressions;

namespace RegionKit.Modules.Objects
{
	public class BGFlatLight : CosmeticSprite
	{
		public readonly PlacedObject pObj;
		public Data data => (pObj.data as Data)!;

		private bool lastCloudMode;

		public BGFlatLight(PlacedObject pObj)
		{
			this.pObj = pObj;
			pos = pObj.pos;
			lastCloudMode = data.cloudMode;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			pos = pObj.pos;
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = [
				new FSprite("Futile_White") {
					shader = rCam.game.rainWorld.Shaders[data.cloudMode ? "BGCloudLight" : "BGFlatLight"]
				}];
			AddToContainer(sLeaser, rCam, null!);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (lastCloudMode != data.cloudMode)
			{
				sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders[data.cloudMode ? "BGCloudLight" : "BGFlatLight"];
				lastCloudMode = data.cloudMode;
			}
			sLeaser.sprites[0].SetPosition(pos - camPos);
			sLeaser.sprites[0].color = GetColor(rCam);
			sLeaser.sprites[0].alpha = data.CustomColor.a;
			sLeaser.sprites[0].scale = data.handlePos.magnitude / 8f;
		}

		public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			newContatiner ??= rCam.ReturnFContainer("ForegroundLights");
			newContatiner.AddChild(sLeaser.sprites[0]);
		}

		public Color GetColor(RoomCamera rCam)
		{
			// GetPixel operates from bottom left
			if (data.mode == Mode.EffectColor1)
			{
				return rCam.paletteTexture.GetPixel(30, 5);
			}
			if (data.mode == Mode.EffectColor1Far)
			{
				return rCam.paletteTexture.GetPixel(30, 4);
			}
			if (data.mode == Mode.EffectColor2)
			{
				return rCam.paletteTexture.GetPixel(30, 3);
			}
			if (data.mode == Mode.EffectColor2Far)
			{
				return rCam.paletteTexture.GetPixel(30, 2);
			}
			if (data.mode == Mode.White)
			{
				return rCam.paletteTexture.GetPixel(30, 1);
			}
			if (data.mode == Mode.FogColor)
			{
				return rCam.paletteTexture.GetPixel(1, 7);
			}
			return data.CustomColor;
		}

		public class Data : PlacedObject.ResizableObjectData
		{
			public Vector2 panelPos = new(100f, 100f);
			public Mode mode = Mode.CustomColor;
			public float r = 1f, g = 1f, b = 1f;
			public float strength = 1f;
			public bool cloudMode = false;

			public Color CustomColor => new(r, g, b, strength);

			public Data(PlacedObject owner) : base(owner)
			{
			}

			public override string ToString()
			{
				string text = string.Format(CultureInfo.InvariantCulture,
					"{0}~{1}~{2}~{3}~{4}~{5}~{6}~{7}~{8}~{9}",
					handlePos.x,
					handlePos.y,
					panelPos.x,
					panelPos.y,
					mode,
					r, g, b,
					strength,
					cloudMode);
				text = SaveState.SetCustomData(this, text);
				return SaveUtils.AppendUnrecognizedStringAttrs(text, "~", unrecognizedAttributes);
			}

			public override void FromString(string s)
			{
				try
				{
					string[] array = Regex.Split(s, "~");
					if (array.Length > 0) handlePos.x = float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
					if (array.Length > 1) handlePos.y = float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
					if (array.Length > 2) panelPos.x = float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
					if (array.Length > 3) panelPos.y = float.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture);
					if (array.Length > 4) mode = new Mode(array[4], false);
					if (array.Length > 5) r = float.Parse(array[5], NumberStyles.Any, CultureInfo.InvariantCulture);
					if (array.Length > 6) g = float.Parse(array[6], NumberStyles.Any, CultureInfo.InvariantCulture);
					if (array.Length > 7) b = float.Parse(array[7], NumberStyles.Any, CultureInfo.InvariantCulture);
					if (array.Length > 8) strength = float.Parse(array[8], NumberStyles.Any, CultureInfo.InvariantCulture);
					if (array.Length > 9) cloudMode = bool.Parse(array[9]);
					unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, 10);
				}
				catch (Exception ex)
				{
					LogWarning("Failed to parse BGFlatLight data!");
					LogError(ex);
				}
			}
		}

		public class Mode(string value, bool register = false) : ExtEnum<Mode>(value, register)
		{
			public static readonly Mode CustomColor = new("Custom", true);
			public static readonly Mode EffectColor1 = new("EffectColor1", true);
			public static readonly Mode EffectColor1Far = new("EffectColor1Far", true);
			public static readonly Mode EffectColor2 = new("EffectColor2", true);
			public static readonly Mode EffectColor2Far = new("EffectColor2Far", true);
			public static readonly Mode White = new("White", true);
			public static readonly Mode FogColor = new("FogColor", true);
		}
	}
}

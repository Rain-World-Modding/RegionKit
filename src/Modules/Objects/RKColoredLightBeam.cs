using System.Globalization;
using System.Text.RegularExpressions;
using DevInterface;

namespace RegionKit.Modules.Objects;

/// <summary>
/// By Dracentis, ported by M4rbleL1ne
/// Light beams that can use effect colors
/// </summary>
public class ColoredLightBeam : LightBeam
{
	internal Color _paletteEffectColor;
	internal bool _baseColorMode, _colorUpdated;
	internal int _effectColor = -1;

	///<inheritdoc/>
	public ColoredLightBeam(PlacedObject placedObject) : base(placedObject) => _paletteEffectColor = Color.white;

	internal static void Apply()
	{
		On.LightBeam.UpdateColor += LightBeamUpdateColor;
		On.LightBeam.DrawSprites += LightBeamDrawSprites;
		On.DevInterface.LightBeamRepresentation.LightBeamControlPanel.ctor += LightBeamControlPanelCtor;
		On.DevInterface.LightBeamRepresentation.LightBeamControlPanel.Signal += LightBeamControlPanelSignal;
	}

	internal static void Undo()
	{
		On.LightBeam.UpdateColor -= LightBeamUpdateColor;
		On.LightBeam.DrawSprites -= LightBeamDrawSprites;
		On.DevInterface.LightBeamRepresentation.LightBeamControlPanel.ctor -= LightBeamControlPanelCtor;
		On.DevInterface.LightBeamRepresentation.LightBeamControlPanel.Signal -= LightBeamControlPanelSignal;
	}

	private static void LightBeamDrawSprites(On.LightBeam.orig_DrawSprites orig, LightBeam self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (self is ColoredLightBeam b)
			b._colorUpdated = false;
		orig(self, sLeaser, rCam, timeStacker, camPos);
		if (self is ColoredLightBeam b2 && !b2._colorUpdated)
			self.UpdateColor(sLeaser, rCam, self.lastAlpha);
	}

	private static void LightBeamControlPanelSignal(On.DevInterface.LightBeamRepresentation.LightBeamControlPanel.orig_Signal orig, LightBeamRepresentation.LightBeamControlPanel self, DevUISignalType type, DevUINode sender, string message)
	{
		orig(self, type, sender, message);
		if (self.parentNode is ColoredLightBeamRepresentation rep && sender.IDstring == "Color_Button")
		{
			ColoredLightBeamData data = (rep.pObj.data as ColoredLightBeamData)!;
			if (data.colorType >= ColoredLightBeamData.ColorType.EffectColor2)
				data.colorType = ColoredLightBeamData.ColorType.Environment;
			else
				data.colorType++;
			(sender as Button)!.Text = data.colorType.ToString();
			if (rep.LB is ColoredLightBeam b)
			{
				b._baseColorMode = data.colorType is ColoredLightBeamData.ColorType.Environment;
				b._effectColor = (int)data.colorType - 1;
			}
		}
	}

	private static void LightBeamControlPanelCtor(On.DevInterface.LightBeamRepresentation.LightBeamControlPanel.orig_ctor orig, LightBeamRepresentation.LightBeamControlPanel self, DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos)
	{
		orig(self, owner, IDstring, parentNode, pos);
		if (parentNode is ColoredLightBeamRepresentation rep)
		{
			self.subNodes.Add(new Button(owner, "Color_Button", self, new(5f, 125f), 110f, (rep.pObj.data as ColoredLightBeamData)!.colorType.ToString()));
			self.fLabels[0].text = "Colored Light Beam";
			self.size.y += 20f;
		}
	}

	private static void LightBeamUpdateColor(On.LightBeam.orig_UpdateColor orig, LightBeam self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float a)
	{
		orig(self, sLeaser, rCam, a);
		if (self is ColoredLightBeam b)
		{
			if (!b._baseColorMode)
			{
				if (b._effectColor >= 0)
					b._paletteEffectColor = rCam.currentPalette.texture.GetPixel(30, 5 - b._effectColor * 2);
				self.color = Color.Lerp(b._paletteEffectColor, Color.white, (b.placedObject.data as LightBeamData)!.colorA);
				Color color = RGB2RGBA(self.color, a);
				TriangleMesh mesh = (sLeaser.sprites[0] as TriangleMesh)!;
				for (var i = 0; i < mesh.verticeColors.Length; i++)
					mesh.verticeColors[i] = color;
			}
			b._colorUpdated = true;
		}
	}

	internal class ColoredLightBeamData : LightBeamData
	{
		public ColorType colorType;

		public ColoredLightBeamData(PlacedObject owner) : base(owner) { }

		public override void FromString(string s)
		{
			base.FromString(s);
			var array = Regex.Split(s, "~");
			if (array.Length > 15)
				Enum.TryParse(array[15], out colorType);
			SaveUtils.PopulateUnrecognizedStringAttrs(array, 16);
		}

		public override string ToString() => SaveUtils.AppendUnrecognizedStringAttrs(string.Concat(
			BaseSaveString() + string.Format(CultureInfo.InvariantCulture, "~{0}~{1}~{2}~{3}~{4}~{5}", panelPos.x, panelPos.y, alpha, colorA, colorB, sun ? "1" : "0"),
			string.Format(CultureInfo.InvariantCulture, "~{0}~{1}~{2}~{3}", blinkType, blinkRate, nightLight ? "1" : "0", colorType)),
			"~", unrecognizedAttributes);

		public enum ColorType
		{
			Environment,
			EffectColor1,
			EffectColor2
		}
	}
}

internal class ColoredLightBeamRepresentation : LightBeamRepresentation
{
	public ColoredLightBeamRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj) 
	{
		fLabels[0].text = "Colored Light Beam";
		if (LB is ColoredLightBeam b)
		{
			b._baseColorMode = (pObj.data as ColoredLightBeam.ColoredLightBeamData)!.colorType is ColoredLightBeam.ColoredLightBeamData.ColorType.Environment;
			b._effectColor = (int)(pObj.data as ColoredLightBeam.ColoredLightBeamData)!.colorType - 1;
		}
	}
}

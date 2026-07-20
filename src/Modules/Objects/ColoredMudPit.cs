using System.Globalization;
using System.Text.RegularExpressions;
using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using RegionKit.Modules.Iggy;

namespace RegionKit.Modules.Objects
{
	public static class ColoredMudPit
	{
		internal static void Apply()
		{
			On.MudPit.DrawSprites += MudPit_DrawSprites;
			IL.MudPit.ApplyPalette += MudPit_ApplyPalette;
		}

		internal static void Undo()
		{
			On.MudPit.DrawSprites -= MudPit_DrawSprites;
			IL.MudPit.ApplyPalette -= MudPit_ApplyPalette;
		}

		private static void MudPit_DrawSprites(On.MudPit.orig_DrawSprites orig, MudPit self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (!self.slatedForDeletetion && self.Data is ColoredMudPitData coloredData && sLeaser.sprites[1].color != coloredData.selectedColor)
			{
				self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
			}
			orig(self, sLeaser, rCam, timeStacker, camPos);
		}

		private static void MudPit_ApplyPalette(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.AfterLabel, x => x.MatchStfld<MudPit>(nameof(MudPit.color)));
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((Color origColor, MudPit self) =>
			{
				if (self.Data is ColoredMudPitData coloredData)
				{
					return coloredData.selectedColor;
				}
				return origColor;
			});
		}

		public class ColoredMudPitData : MudPit.MudPitData
		{
			public Vector2 panelPos = new Vector2(0f, 100f);
			public Color selectedColor = MudPit.defaultColor;

			public ColoredMudPitData(PlacedObject owner) : base(owner)
			{
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] array = Regex.Split(s, "~");
				int i = 0;

				if (i < array.Length) handlePos.x = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (i < array.Length) handlePos.y = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (i < array.Length) decalSize = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (i < array.Length) panelPos.x = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (i < array.Length) panelPos.y = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (i < array.Length) selectedColor.r = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (i < array.Length) selectedColor.g = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (i < array.Length) selectedColor.b = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);

				unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, i);
			}

			public override string ToString()
			{
				string text = BaseSaveString() + string.Format(CultureInfo.InvariantCulture,
					"~{0}~{1}~{2}~{3}~{4}~{5}",
					decalSize,
					panelPos.x,
					panelPos.y,
					selectedColor.r,
					selectedColor.g,
					selectedColor.b
					);
				text = SaveState.SetCustomData(this, text);
				return SaveUtils.AppendUnrecognizedStringAttrs(text, "~", this.unrecognizedAttributes);
			}
		}

		public class ColoredMudPitRepresentation : MudPitRepresentation, IGiveAToolTip
		{
			private FSprite panelLine;
			private new ColoredMudPitData Data => (pObj.data as ColoredMudPitData)!;

			private RGBSelectPanel panel;

			public ToolTip? ToolTip => new ToolTip("Used to make a single mudpit a different color than either the default or the region properties-defined mud color.", 6, panel);

			public bool MouseOverMe => MouseOver || panel.MouseOver;

			public ColoredMudPitRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
			{
				fSprites.Add(panelLine = new FSprite("pixel") { anchorY = 0f });
				owner.placedObjectsContainer.AddChild(panelLine);
				subNodes.Add(panel = new RGBSelectPanel(owner, this, Data.panelPos, "ColoredMudPitRep_Panel", "Colored Mud Pit", Data.selectedColor));
				for (int i = 0; i < 4; i++)
				{
					fSprites[firstRectSprite + i].color = Data.selectedColor;
				}
			}

			public override void Update()
			{
				base.Update();
				if (panel.actualValue != Data.selectedColor)
				{
					Data.selectedColor = panel.actualValue;
					for (int i = 0; i < 4; i++)
					{
						fSprites[firstRectSprite + i].color = panel.actualValue;
					}
				}
			}

			public override void Refresh()
			{
				base.Refresh();
				DrawLine(panelLine, absPos, panel.absPos);
				Data.panelPos = panel.pos;
			}

			private void DrawLine(FSprite sprite, Vector2 from, Vector2 to)
			{
				sprite.SetPosition(from);
				sprite.scaleY = Vector2.Distance(from, to);
				sprite.rotation = Custom.AimFromOneVectorToAnother(from, to);
			}
		}
	}
}

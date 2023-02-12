﻿using DevInterface;
namespace RegionKit;


internal class PaletteTextInput
{
	internal static void Apply()
	{
		On.DevInterface.PaletteController.ctor += PaletteController_ctor;
	}
	internal static void Undo()
	{
		On.DevInterface.PaletteController.ctor -= PaletteController_ctor;
	}

	private static void PaletteController_ctor(
		On.DevInterface.PaletteController.orig_ctor orig,
		DevInterface.PaletteController self,
		DevInterface.DevUI owner,
		string IDstring,
		DevInterface.DevUINode parentNode,
		UnityEngine.Vector2 pos,
		string title,
		int controlPoint)
	{
		orig(self, owner, IDstring, parentNode, pos, title, controlPoint);

		if (controlPoint >= 0 && controlPoint <= 3) // one that we know how to process
		{
			//todo: probably breaks
			var paletteField = new PaletteField(self);
			var strfield = new ManagedStringControl(paletteField, new ManagedData(null!, new ManagedField[] { paletteField }), self, 0f);
			strfield.ClearSprites();
			strfield.subNodes[1] = self.subNodes.Find(e => e.IDstring == "Number");
			self.subNodes.Add(strfield);
		}
	}


	// WARNING this is an extremely cursed hack and NOT a reference implementation. If we need string controls for more things we probably need more versatile string controls!
	internal class PaletteField : IntegerField
	{
		DevInterface.PaletteController ctroller;
		public PaletteField(DevInterface.PaletteController ctroller) : base("", -1, int.MaxValue, 0)
		{
			this.ctroller = ctroller;
		}

		public override string DisplayValueForNode(DevInterface.PositionedDevUINode node, ManagedData data)
		{
			string arg = string.Empty;
			switch (ctroller.controlPoint)
			{
			case 0:
				if (ctroller.RoomSettings.pal != null)
				{
					arg = string.Empty;
				}
				else if (!ctroller.RoomSettings.parent.isAncestor && ctroller.RoomSettings.parent.pal != null)
				{
					arg = "<T>";
				}
				else
				{
					arg = "<A>";
				}
				return arg + " " + ctroller.RoomSettings.Palette;
			case 1:
				if (ctroller.RoomSettings.eColA != null)
				{
					arg = string.Empty;
				}
				else if (!ctroller.RoomSettings.parent.isAncestor && ctroller.RoomSettings.parent.eColA != null)
				{
					arg = "<T>";
				}
				else
				{
					arg = "<A>";
				}
				return arg + " " + ctroller.RoomSettings.EffectColorA;
			case 2:
				if (ctroller.RoomSettings.eColB != null)
				{
					arg = string.Empty;
				}
				else if (!ctroller.RoomSettings.parent.isAncestor && ctroller.RoomSettings.parent.eColB != null)
				{
					arg = "<T>";
				}
				else
				{
					arg = "<A>";
				}
				return arg + " " + ctroller.RoomSettings.EffectColorB;
			case 3:
				if (ctroller.RoomSettings.fadePalette == null)
				{
					return "NONE";
				}
				else
				{
					return ctroller.RoomSettings.fadePalette.palette.ToString();
				}
			}
			return "";
		}

		public override void ParseFromText(DevInterface.PositionedDevUINode node, ManagedData data, string newValue)
		{
			base.ParseFromText(node, data, newValue);

			var nint = data.GetValue<int>(key);
			int change = 0;

			switch (ctroller.controlPoint)
			{
			case 0:
				change = nint - (ctroller.RoomSettings.pal ?? 0);
				if (nint > 0 && ctroller.RoomSettings.pal == null) ctroller.Increment(1);
				break;
			case 1:
				change = nint - (ctroller.RoomSettings.eColA ?? 0);
				if (nint > 0 && ctroller.RoomSettings.eColA == null) ctroller.Increment(1);
				break;
			case 2:
				change = nint - (ctroller.RoomSettings.eColB ?? 0);
				if (nint > 0 && ctroller.RoomSettings.eColB == null) ctroller.Increment(1);
				break;
			case 3:
				change = nint - (ctroller.RoomSettings.fadePalette?.palette ?? 0);
				if (nint > 0 && ctroller.RoomSettings.fadePalette == null) ctroller.Increment(1);
				break;
			}

			ctroller.Increment(change);
		}
	}
}

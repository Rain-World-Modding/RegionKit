using System.Reflection;
using System.Text.RegularExpressions;
using DevInterface;
using System.Globalization;

namespace RegionKit.Modules.Objects;

/// <summary>
/// From LB/M4rbleL1ne
/// Allows selecting arbitrary sprites for pipe symbols
/// </summary>
internal static class CustomEntranceSymbols
{
	internal static void Apply() => On.ShortcutGraphics.Draw += ShortcutGraphicsDraw;

	internal static void Undo() => On.ShortcutGraphics.Draw -= ShortcutGraphicsDraw;

	internal static void Dispose() => CESRepresentation.CESControlPanel.SelectSpritePanel.DisposeStatic();

	private static void ShortcutGraphicsDraw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
	{
		orig(self, timeStacker, camPos);
		if (self.room is not Room rm || !rm.shortCutsReady || self.camera is not RoomCamera cam || rm.shortcuts is not ShortcutData[] sAr || rm.roomSettings?.placedObjects is not List<PlacedObject> list)
			return;
		for (var l = 0; l < sAr.Length; l++)
		{
			ShortcutData shortcut = sAr[l];
			for (var m = 0; m < list.Count; m++)
			{
				PlacedObject pObj = list[m];
				if (pObj.type == _Enums.CustomEntranceSymbol && pObj.active && pObj.data is CESData data && rm.GetTilePosition(pObj.pos) == shortcut.StartTile)
				{
					if (self.entranceSprites[l, 0] is not FSprite sprite)
					{
						self.entranceSprites[l, 0] = new(data._imageName) { rotation = data._rotation * 360 };
						self.entranceSpriteLocations[l] = rm.MiddleOfTile(shortcut.StartTile) + IntVector2.ToVector2(rm.ShorcutEntranceHoleDirection(shortcut.StartTile)) * 15f;
						if ((ModManager.MMF && MoreSlugcats.MMF.cfgShowUnderwaterShortcuts.Value) || (rm.water && rm.waterInFrontOfTerrain && rm.PointSubmerged(self.entranceSpriteLocations[l] + new Vector2(0f, 5f))))
							cam.ReturnFContainer((ModManager.MMF && MoreSlugcats.MMF.cfgShowUnderwaterShortcuts.Value) ? "GrabShaders" : "Items").AddChild(self.entranceSprites[l, 0]);
						else
						{
							cam.ReturnFContainer("Shortcuts").AddChild(self.entranceSprites[l, 0]);
							cam.ReturnFContainer("Water").AddChild(self.entranceSprites[l, 1]);
						}
					}
					else
					{
						if (sprite.element.name != data._imageName)
							sprite.element = Futile.atlasManager.GetElementWithName(data._imageName);
						sprite.rotation = data._rotation * 360;
					}
					break;
				}
			}
		}
	}
}

/// <summary>
/// Data for custom entrance symbol
/// </summary>
public class CESData : PlacedObject.Data
{
	internal Vector2 _panelPos;
	internal string _imageName = "ShortcutDots";
	internal float _rotation;

	/// <summary>
	/// Data ctor
	/// </summary>
	public CESData(PlacedObject owner) : base(owner) => _panelPos = DegToVec(120f) * 20f;

	///<inheritdoc/>
	public override void FromString(string s)
	{
		var array = Regex.Split(s, "~");
		float.TryParse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture, out _panelPos.x);
		float.TryParse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture, out _panelPos.y);
		_imageName = array[2];
		float.TryParse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture, out _rotation);
		unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, 4);
	}

	///<inheritdoc/>
	public override string ToString() => SaveUtils.AppendUnrecognizedStringAttrs($"{_panelPos.x}~{_panelPos.y}~{_imageName}~{_rotation}", "~", unrecognizedAttributes);
}

/// <summary>
/// DevUI representation
/// </summary>
public class CESRepresentation : TileObjectRepresentation
{
	internal class CESControlPanel : Panel, IDevUISignals
	{
		internal class SelectSpritePanel : Panel
		{
			private static FieldInfo _IDstringInfo = typeof(DevUINode).GetField("IDstring");

			public SelectSpritePanel(DevUI owner, DevUINode parentNode, Vector2 pos, string[] decalNames) : base(owner, "Select_Sprite_Panel", parentNode, pos, new(175f, 225f), "Select sprite")
			{
				var intVector = new IntVector2(0, 0);
				for (var i = 0; i < decalNames.Length; i++)
				{
					subNodes.Add(new Button(owner, decalNames[i], this, new(5f, size.y - 20f - 20f * intVector.y), 165f, decalNames[i]));
					intVector.y++;
					if (intVector.y > 9)
					{
						intVector.x++;
						intVector.y = 0;
					}
				}
				subNodes.Add(new Button(owner, "Button_Sprites_Previous0", this, new(5f, 5f), 80f, "Previous"));
				subNodes.Add(new Button(owner, "Button_Sprites_Next0", this, new(90f, 5f), 80f, "Next"));
				OrganizeSprites(0);
			}

			public void OrganizeSprites(int page)
			{
				var intVector = new IntVector2(0, 0);
				for (var i = 0; i < subNodes.Count; i++)
				{
					if (subNodes[i] is not Button button)
						continue;
					if (!button.IDstring.StartsWith("Button_Sprites_Next") && !button.IDstring.StartsWith("Button_Sprites_Previous"))
					{
						button.pos = (intVector.x >= page && intVector.x <= page) ? new(5f, size.y - 20f - 20f * intVector.y) : new(10000f, 10000f);
						intVector.y++;
						if (intVector.y > 9)
						{
							intVector.x++;
							intVector.y = 0;
						}
					}
					else if (button.IDstring.StartsWith("Button_Sprites_Next"))
						ChangeIDstring(button, "Button_Sprites_Next" + page);
					else if (button.IDstring.StartsWith("Button_Sprites_Previous"))
						ChangeIDstring(button, "Button_Sprites_Previous" + page);
				}
			}

			private void ChangeIDstring(Button button, string value) => _IDstringInfo.SetValue(button, value);

			internal static void DisposeStatic() => _IDstringInfo = null!;
		}

		private SelectSpritePanel? _spriteSelectPanel;

		internal CESControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new(250f, 45f), "Custom Entrance Symbol")
		{
			CESData data = ((parentNode as CESRepresentation)!.pObj.data as CESData)!;
			subNodes.Add(new Button(owner, "Sprite_Button", this, new(5f, 5f), 240f, "Sprite: " + data._imageName));
			subNodes.Add(new Button(owner, "Rotation_Button", this, new(5f, 25f), 240f, "Rotation: " + data._rotation * 360 + "%"));
		}

		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			var data = (parentNode as CESRepresentation)!.pObj.data as CESData;
			if (data is null) 
				return;
			if (sender.IDstring is "Rotation_Button")
			{
				switch (data._rotation)
				{
				case 0f:
					data._rotation = .25f;
					break;
				case .25f:
					data._rotation = .5f;
					break;
				case .5f:
					data._rotation = .75f;
					break;
				case .75f:
					data._rotation = 0f;
					break;
				}
				(sender as Button)!.Text = "Rotation: " + data._rotation * 360 + "%";
				return;
			}
			else if ((parentNode as CESRepresentation)!._files is string[] f)
			{
				if (_spriteSelectPanel is SelectSpritePanel s)
				{
					if (sender.IDstring.StartsWith("Button_Sprites_Next"))
					{
						int.TryParse(sender.IDstring[19..], NumberStyles.Any, CultureInfo.InvariantCulture, out var num);
						var nP = f.Length / 10f - 1f;
						if (num < nP)
						{
							num++;
							s.OrganizeSprites(num);
						}
					}
					else if (sender.IDstring.StartsWith("Button_Sprites_Previous"))
					{
						int.TryParse(sender.IDstring[23..], NumberStyles.Any, CultureInfo.InvariantCulture, out var num);
						if (num > 0)
						{
							num--;
							s.OrganizeSprites(num);
						}
					}
					else
					{
						if (sender.IDstring != "Sprite_Button")
						{
							data._imageName = sender.IDstring;
							if (subNodes.FirstOrDefault(x => x.IDstring == "Sprite_Button") is Button b)
								b.Text = "Sprite: " + data._imageName;
						}
						subNodes.Remove(s);
						s.ClearSprites();
						_spriteSelectPanel = null;
					}
				}
				else
				{
					if (sender.IDstring == "Sprite_Button")
					{
						_spriteSelectPanel = new(owner, this, new Vector2(190f, 225f) - absPos, f);
						subNodes.Add(_spriteSelectPanel);
						return;
					}
				}
			}
		}
	}

	private readonly string[] _files;
	private readonly int _pixelIndex;
	private readonly CESControlPanel _panel;

	///<inheritdoc/>
	public CESRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, pObj.type.ToString())
	{
		_panel = new(owner, "CustomEntranceSymbolPanel", this, new(0f, 100f));
		subNodes.Add(_panel);
		_panel.pos = (pObj.data as CESData)!._panelPos;
		fSprites.Add(new("pixel"));
		_pixelIndex = fSprites.Count - 1;
		owner.placedObjectsContainer.AddChild(fSprites[_pixelIndex]);
		fSprites[_pixelIndex].anchorY = 0f;
		_files = (from n in Futile.atlasManager._allElementsByName.Values where n.name.Contains("Symbol", "Shortcut", "Kill") select n.name).ToArray();
	}

	///<inheritdoc/>
	public override void Refresh()
	{
		base.Refresh();
		(pObj.data as CESData)!._panelPos = _panel.pos;
		MoveSprite(_pixelIndex, absPos);
		fSprites[_pixelIndex].scaleY = _panel.pos.magnitude;
		fSprites[_pixelIndex].rotation = AimFromOneVectorToAnother(absPos, _panel.absPos);
	}
}

internal static class CESExtensions
{
	public static bool Contains(this string self, params string[] values)
	{
		var res = false;
		for (var i = 0; i < values.Length; i++)
		{
			if (self.Contains(values[i]))
			{
				res = true;
				break;
			}
		}
		return res;
	}
}

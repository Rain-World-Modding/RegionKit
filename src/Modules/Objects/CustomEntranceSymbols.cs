using System.Reflection;
using System.Text.RegularExpressions;
using DevInterface;
using System.Globalization;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.Objects;

/// <summary>
/// From LB/M4rbleL1ne
/// Allows selecting arbitrary sprites for pipe symbols
/// </summary>
internal static class CustomEntranceSymbols
{
	internal static void Apply() => On.ShortcutGraphics.Draw += ShortcutGraphicsDraw;

	internal static void Undo() => On.ShortcutGraphics.Draw -= ShortcutGraphicsDraw;

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
	public override string ToString() => SaveUtils.AppendUnrecognizedStringAttrs(SaveState.SetCustomData(this, $"{_panelPos.x}~{_panelPos.y}~{_imageName}~{_rotation}"), "~", unrecognizedAttributes);
}

/// <summary>
/// DevUI representation
/// </summary>
public class CESRepresentation : TileObjectRepresentation
{
	internal class CESControlPanel : Panel, IDevUISignals
	{
		private SearchableSelectPanel? _spriteSelectPanel;

		public CESRepresentation parentRepresentation;

		internal CESControlPanel(DevUI owner, string IDstring, CESRepresentation parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new(250f, 45f), "Custom Entrance Symbol")
		{
			parentRepresentation = parentNode;
			CESData data = parentRepresentation.cesData;
			subNodes.Add(new Button(owner, "Sprite_Button", this, new(5f, 5f), 240f, "Sprite: " + data._imageName));
			subNodes.Add(new Button(owner, "Rotation_Button", this, new(5f, 25f), 240f, "Rotation: " + data._rotation * 360 + "°"));
		}

		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			CESData data = parentRepresentation.cesData;
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
				(sender as Button)!.Text = "Rotation: " + data._rotation * 360 + "°";
			}
			else if (parentRepresentation._files is string[] f)
			{
				if (_spriteSelectPanel != null)
				{

					if (sender.IDstring == "BackPage99289..?/~")
					{
						_spriteSelectPanel.PrevPage();
					}
					else if (sender.IDstring == "NextPage99289..?/~")
					{
						_spriteSelectPanel.NextPage();
					}
					else if (sender.parentNode == _spriteSelectPanel && sender.IDstring != "Search99289..?/~")
					{
						data._imageName = sender.IDstring;
						if (subNodes.FirstOrDefault(x => x.IDstring == "Sprite_Button") is Button b)
							b.Text = "Sprite: " + data._imageName;

						subNodes.Remove(_spriteSelectPanel);
						_spriteSelectPanel.ClearSprites();
						_spriteSelectPanel = null;
					}
				}
				else
				{
					if (sender.IDstring == "Sprite_Button")
					{
						_spriteSelectPanel = new(owner, "CustomEntranceSymbols_Search", this, new Vector2(190f, 225f) - absPos, "Select sprite", f, selectedItem: data._imageName);
						subNodes.Add(_spriteSelectPanel);
					}
				}
			}
		}
	}

	private readonly string[] _files;
	private readonly int _pixelIndex;
	private readonly CESControlPanel _panel;
	public readonly CESData cesData;

	///<inheritdoc/>
	public CESRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, pObj.type.ToString())
	{
		cesData = (pObj.data as CESData)!;
		_files = (from n in Futile.atlasManager._allElementsByName.Values where n.name.ToLowerInvariant().Contains("symbol", "shortcut", "kill") select n.name).ToArray();

		_panel = new CESControlPanel(owner, "CustomEntranceSymbolPanel", this, new(0f, 100f)) { pos = cesData._panelPos };
		subNodes.Add(_panel);

		fSprites.Add(new FSprite("pixel") { anchorY = 0f });
		_pixelIndex = fSprites.Count - 1;
		owner.placedObjectsContainer.AddChild(fSprites[_pixelIndex]);
	}

	///<inheritdoc/>
	public override void Refresh()
	{
		base.Refresh();
		cesData._panelPos = _panel.pos;
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

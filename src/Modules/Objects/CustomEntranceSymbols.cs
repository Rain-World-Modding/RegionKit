using System;
using System.Reflection;
using System.Text.RegularExpressions;
using DevInterface;
using UnityEngine;
using RWCustom;
using static System.Reflection.BindingFlags;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.Objects
{
	public sealed class CustomEntranceSymbols
	{
		public static class Enums_CES
		{
			public static PlacedObject.Type CustomEntranceSymbol = new("CustomEntranceSymbol", true);
		}

		public static void ApplyHooks()
		{
			On.ShortcutGraphics.Draw += (orig, self, timeStacker, camPos) =>
			{
				orig(self, timeStacker, camPos);
				if (self.room?.shortcuts is null || self.room.roomSettings?.placedObjects is null)
					return;
				for (var l = 0; l < self.room.shortcuts.Length; l++)
				{
					for (var m = 0; m < self.room.roomSettings.placedObjects.Count; m++)
					{
						var pObj = self.room.roomSettings.placedObjects[m];
						if (pObj.type == Enums_CES.CustomEntranceSymbol && pObj.active && pObj.data is CESData data && self.room.GetTilePosition(pObj.pos) == self.room.shortcuts[l].StartTile)
						{
							if (self.entranceSprites[l, 0] is null)
							{
								self.entranceSprites[l, 0] = new(data._imageName) { rotation = data._rotation * 360 };
								self.entranceSpriteLocations[l] = self.room.MiddleOfTile(self.room.shortcuts[l].StartTile) + IntVector2.ToVector2(self.room.ShorcutEntranceHoleDirection(self.room.shortcuts[l].StartTile)) * 15f;
								if (self.room.water && self.room.waterInFrontOfTerrain && self.room.PointSubmerged(self.entranceSpriteLocations[l] + new Vector2(0f, 5f)))
								{
									self.camera?.ReturnFContainer("Items").AddChild(self.entranceSprites[l, 0]);
									continue;
								}
								self.camera?.ReturnFContainer("Shortcuts").AddChild(self.entranceSprites[l, 0]);
								self.camera?.ReturnFContainer("Water").AddChild(self.entranceSprites[l, 1]);
							}
							else
							{
								self.entranceSprites[l, 0].element = Futile.atlasManager.GetElementWithName(data._imageName);
								self.entranceSprites[l, 0].rotation = data._rotation * 360;
							}
							break;
						}
					}
				}
			};
			On.DevInterface.ObjectsPage.CreateObjRep += (orig, self, tp, pObj) =>
			{
				if (tp == Enums_CES.CustomEntranceSymbol)
				{
					if (pObj is null)
					{
						self.RoomSettings.placedObjects.Add(pObj = new(tp, null)
						{
							pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new(-683f, 384f), .25f) + Custom.DegToVec(Random.value * 360f) * .2f
						});
					}
					var pObjRep = new CESRepresentation(self.owner, tp.ToString() + "_Rep", self, pObj);
					self.tempNodes.Add(pObjRep);
					self.subNodes.Add(pObjRep);
				}
				else
					orig(self, tp, pObj);
			};
			On.PlacedObject.GenerateEmptyData += (orig, self) =>
			{
				orig(self);
				if (self.type == Enums_CES.CustomEntranceSymbol)
					self.data = new CESData(self);
			};
		}
	}

	public class CESData : PlacedObject.Data
	{
		internal Vector2 _panelPos;
		internal string _imageName = "ShortcutDots";
		internal float _rotation;

		public CESData(PlacedObject owner) : base(owner) => _panelPos = Custom.DegToVec(120f) * 20f;

		public override void FromString(string s)
		{
			var array = Regex.Split(s, "~");
			_panelPos.x = float.Parse(array[0]);
			_panelPos.y = float.Parse(array[1]);
			_imageName = array[2];
			_rotation = float.Parse(array[3]);
		}

		public override string ToString() => $"{_panelPos.x}~{_panelPos.y}~{_imageName}~{_rotation}";
	}

	public class CESRepresentation : TileObjectRepresentation
	{
		internal class CESControlPanel : Panel, IDevUISignals
		{
			internal class SelectSpritePanel : Panel
			{
				static readonly FieldInfo _IDstring = typeof(DevUINode).GetField("IDstring", Instance | Static | Public | NonPublic);

				object this[FieldInfo field, object obj] { set => field.SetValue(obj, value, Instance | Static | Public | NonPublic, Type.DefaultBinder, null); }

				internal SelectSpritePanel(DevUI owner, DevUINode parentNode, Vector2 pos, string[] decalNames) : base(owner, "Select_Sprite_Panel", parentNode, pos, new(175f, 225f), "Select sprite")
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
					OrganizeSprites(0, this);
				}

				internal static void OrganizeSprites(int page, SelectSpritePanel self)
				{
					var intVector = new IntVector2(0, 0);
					for (var i = 0; i < self.subNodes.Count; i++)
					{
						if (self.subNodes[i] is not Button button)
							continue;
						if (!button.IDstring.StartsWith("Button_Sprites_Next") && !button.IDstring.StartsWith("Button_Sprites_Previous"))
						{
							button.pos = (intVector.x >= page && intVector.x <= page) ? new(5f, self.size.y - 20f - 20f * intVector.y) : new(10000f, 10000f);
							intVector.y++;
							if (intVector.y > 9)
							{
								intVector.x++;
								intVector.y = 0;
							}
						}
						else if (button.IDstring.StartsWith("Button_Sprites_Next"))
							self[_IDstring, button] = "Button_Sprites_Next" + page;
						else if (button.IDstring.StartsWith("Button_Sprites_Previous"))
							self[_IDstring, button] = "Button_Sprites_Previous" + page;
					}
				}
			}

			private SelectSpritePanel? _spriteSelectPanel;

			internal CESControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new(250f, 45f), "Custom Entrance Symbol")
			{

				var data = ((parentNode as CESRepresentation)!.pObj.data as CESData)!;
				subNodes.Add(new Button(owner, "Sprite_Button", this, new(5f, 5f), 240f, "Sprite: " + data._imageName));
				subNodes.Add(new Button(owner, "Rotation_Button", this, new(5f, 25f), 240f, "Rotation: " + data._rotation * 360 + "�"));
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				var data = (parentNode as CESRepresentation)!.pObj.data as CESData;
				if (data is null) return;
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
					(sender as Button)!.Text = "Rotation: " + data._rotation * 360 + "�";
					return;
				}
				CESRepresentation rep = (parentNode as CESRepresentation)!;
				if (sender.IDstring.StartsWith("Button_Sprites_Next") && _spriteSelectPanel is not null)
				{
					var num = int.Parse(sender.IDstring.Substring(19));
					var nP = rep!._files.Length / 10f - 1f;
					if (num < nP)
					{
						num++;
						SelectSpritePanel.OrganizeSprites(num, _spriteSelectPanel);
					}
				}
				else if (sender.IDstring.StartsWith("Button_Sprites_Previous") && _spriteSelectPanel is not null)
				{
					var num = int.Parse(sender.IDstring.Substring(23));
					if (num > 0)
					{
						num--;
						SelectSpritePanel.OrganizeSprites(num, _spriteSelectPanel);
					}
				}
				else
				{
					if (sender.IDstring is "Sprite_Button")
					{
						if (_spriteSelectPanel is not null)
						{
							subNodes.Remove(_spriteSelectPanel);
							_spriteSelectPanel.ClearSprites();
							_spriteSelectPanel = null;
						}
						else
						{
							_spriteSelectPanel = new(owner, this, new Vector2(190f, 225f) - absPos, rep._files);
							subNodes.Add(_spriteSelectPanel);
						}
						return;
					}
					data._imageName = sender.IDstring;
					for (int i = 0; i < subNodes.Count; i++)
					{
						if (subNodes[i].IDstring is "Sprite_Button") (subNodes[i] as Button)!.Text = "Sprite: " + data._imageName;
					}
					if (_spriteSelectPanel is not null)
					{
						subNodes.Remove(_spriteSelectPanel);
						_spriteSelectPanel.ClearSprites();
						_spriteSelectPanel = null;
					}
				}
			}
		}

		private readonly string[] _files;
		private readonly int _pixelIndex;
		readonly CESControlPanel _panel;

		public CESRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, pObj.type.ToString())
		{
			_panel = new(owner, "CustomEntranceSymbolPanel", this, new(0f, 100f));
			subNodes.Add(_panel);
			_panel.pos = (pObj.data as CESData)!._panelPos;
			fSprites.Add(new("pixel"));
			_pixelIndex = fSprites.Count - 1;
			owner.placedObjectsContainer.AddChild(fSprites[_pixelIndex]);
			fSprites[_pixelIndex].anchorY = 0f;
			_files = new string[Futile.atlasManager._allElementsByName.Count];
			var i = 0;
			foreach (var item in Futile.atlasManager._allElementsByName)
			{
				if (item.Value.name.Contains("Symbol", "Shortcut", "Kill"))
					i++;
			}
			_files = new string[i];
			i = 0;
			foreach (var item in Futile.atlasManager._allElementsByName)
			{
				if (item.Value.name.Contains("Symbol", "Shortcut", "Kill"))
				{
					_files[i] = item.Value.name;
					i++;
				}
			}
		}

		public override void Refresh()
		{
			base.Refresh();
			(pObj.data as CESData)!._panelPos = _panel.pos;
			MoveSprite(_pixelIndex, absPos);
			fSprites[_pixelIndex].scaleY = _panel.pos.magnitude;
			fSprites[_pixelIndex].rotation = Custom.AimFromOneVectorToAnother(absPos, _panel.absPos);
		}
	}

	public static class CESExtensions
	{
		public static bool Contains(this string self, params string[] values)
		{
			var res = false;
			foreach (var val in values)
			{
				if (self.Contains(val))
				{
					res = true;
					break;
				}
			}
			return res;
		}
	}
}

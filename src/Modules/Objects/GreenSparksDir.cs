using System.Globalization;
using System.Runtime.CompilerServices;
using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using RegionKit.Modules.Iggy;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.Objects
{
	public static class GreenSparksDir
	{
		private static readonly ConditionalWeakTable<Room, Handler> _implCWT = new();

		public static Handler? GetHandler(Room room)
		{
			return _implCWT.TryGetValue(room, out Handler? handler) ? handler : null;
		}

		public static void AddOrRefresh(Room room)
		{
			if (!_implCWT.TryGetValue(room, out Handler handler))
			{
				handler = new Handler(room);
				_implCWT.Add(room, handler);
				//Implementation.Apply();
			}
			handler.Refresh();
		}

		public static void ToggleDebugArrows(Room room)
		{
			if (room.updateList.FirstOrDefault(x => x is DebugArrows) is DebugArrows first)
			{
				first.Destroy();
			}
			else
			{
				room.AddObject(new DebugArrows());
			}
		}

		public class Handler
		{
			private readonly Room room;
			private readonly List<Handle> handles = [];
			private readonly List<IntVector2> safeSpawns = [];

			internal Handler(Room room)
			{
				this.room = room;
			}

			public void Refresh()
			{
				handles.Clear();
				foreach (PlacedObject pObj in room.roomSettings.placedObjects.Where(x => x.type == _Enums.GreenSparksDir))
				{
					handles.Add(new Handle
					{
						Position = pObj.pos,
						Direction = (pObj.data as Data)!.dir
					});
				}

				safeSpawns.Clear();
				for (int i = 0; i < room.TileWidth; i++)
				{
					Vector2 bottomPos = room.MiddleOfTile(i, 0);
					Vector2 topPos = room.MiddleOfTile(i, room.TileHeight);
					MaybeAddSpawnPos(bottomPos);
					MaybeAddSpawnPos(topPos);
				}
				for (int j = 0; j < room.TileHeight; j++)
				{
					Vector2 leftPos = room.MiddleOfTile(0, j);
					Vector2 rightPos = room.MiddleOfTile(room.TileWidth, j);
					MaybeAddSpawnPos(leftPos);
					MaybeAddSpawnPos(rightPos);
				}
			}

			private void MaybeAddSpawnPos(Vector2 pos)
			{
				Vector2 dir = DirectionAt(pos);
				Vector2 roomDir = Custom.DirVec(pos, room.RoomRect.Center);
				if (Vector2.Dot(dir, roomDir) > 0)
				{
					safeSpawns.Add(room.GetTilePosition(pos));
				}
			}

			public Vector2 DirectionAt(Vector2 pos)
			{
				// Inverse distance weighting
				Vector2 weightedDir = Vector2.zero;
				float sumWeight = 0f;
				foreach (Handle handle in handles)
				{
					float w = 1f / (handle.Position - pos).sqrMagnitude; // 1 / distance(pos, handle)^2
					weightedDir += handle.Direction * w;
					sumWeight += w;
				}
				if (sumWeight == 0f || float.IsNaN(weightedDir.x) || float.IsNaN(weightedDir.y))
				{
					return Vector2.zero; // don't explode
				}
				return (weightedDir / sumWeight).normalized;
			}

			public IntVector2 RandomSpawnPos()
			{
				if (safeSpawns.Count == 0)
				{
					return Random.value < 0.5f 
						? new IntVector2(Random.Range(0, room.TileWidth), Random.value < 0.5f ? 0 : room.TileHeight)
						: new IntVector2(Random.value < 0.5f ? 0 : room.TileWidth, Random.Range(0, room.TileHeight));
				}
				return safeSpawns[Random.Range(0, safeSpawns.Count)];
			}

			private struct Handle
			{
				public Vector2 Position;
				public Vector2 Direction;
			}
		}

		private class DebugArrows : UpdatableAndDeletable
		{
			public override void Update(bool eu)
			{
				const int RAD = 20;
				base.Update(eu);
				if (_implCWT.TryGetValue(room, out Handler impl))
				{
					Vector2 mousePos = (Vector2)Futile.mousePosition + room.game.cameras[0].pos;
					for (int i = -RAD; i < RAD; i++)
					{
						for (int j = -RAD; j < RAD; j++)
						{
							Vector2 roomPos = mousePos + new Vector2(20f * i, 20f * j);
							Vector2 dir = impl.DirectionAt(roomPos);
							if (dir.sqrMagnitude > 0f)
							{
								DebugDrawing.DrawArrow(room, roomPos, roomPos + dir * 15f, Custom.HSL2RGB((Custom.VecToDeg(dir) + 180f) / 360f, 1f, 0.5f), 1, 5);
							}
						}
					}
				}
			}
		}

		public class Data : PlacedObject.Data
		{
			public Vector2 dir;

			public Data(PlacedObject owner) : base(owner)
			{
				dir = Custom.RNV();
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] array = s.Split('~');
				int i = 0;

				if (i < array.Length) dir.x = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
				if (i < array.Length) dir.y = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);

				unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, i);
			}

			public override string ToString()
			{
				string text = string.Format(CultureInfo.InvariantCulture,
					"{0}~{1}",
					dir.x,
					dir.y
					);
				text = SaveState.SetCustomData(this, text);
				return SaveUtils.AppendUnrecognizedStringAttrs(text, "~", unrecognizedAttributes);
			}
		}

		public class Representation : PlacedObjectRepresentation, IGiveAToolTip
		{
			private Data data => (pObj.data as Data)!;

			public ToolTip? ToolTip => new ToolTip("Control handle for green sparks direction. Middle mouse click to see resulting vector field.", 10, this);

			public bool MouseOverMe => MouseOver;

			private readonly DirectionPicker dirPicker;
			private bool wasButtonPressed = false;
			private Vector2 lastPos = Vector2.zero;

			public Representation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
			{
				float radius = 30f;
				subNodes.Add(dirPicker = new DirectionPicker(owner, "Dir", this, Vector2.zero, radius, data.dir, false));
				AddOrRefresh(owner.room);
			}

			public override void Update()
			{
				base.Update();
				if (dirPicker.Dir != data.dir)
				{
					data.dir = dirPicker.Dir;
					AddOrRefresh(owner.room);
				}
				else if (lastPos != pos)
				{
					lastPos = pos;
					AddOrRefresh(owner.room);
				}

				if (MouseOver)
				{
					bool buttonPressed = Input.GetMouseButton(2);
					if (buttonPressed && !wasButtonPressed)
					{
						ToggleDebugArrows(owner.room);
					}
					wasButtonPressed = buttonPressed;
				}
			}
		}

		internal static class Implementation
		{
			private static bool appliedHooks = false;
			internal static void Apply()
			{
				if (!appliedHooks)
				{
					try
					{
						IL.GreenSparks.AddSpark += GreenSparks_AddSpark;
						IL.GreenSparks.GreenSpark.Update += GreenSpark_Update;
					}
					catch (Exception e)
					{
						LogFatal(e);
					}
				}
				appliedHooks = true;
			}

			internal static void Undo()
			{
				if (appliedHooks)
				{
					try
					{
						IL.GreenSparks.AddSpark -= GreenSparks_AddSpark;
						IL.GreenSparks.GreenSpark.Update -= GreenSpark_Update;
					}
					catch (Exception e)
					{
						LogFatal(e);
					}
				}
				appliedHooks = false;
			}

			private static void GreenSparks_AddSpark(ILContext il)
			{
				var c = new ILCursor(il);
				c.GotoNext(x => x.MatchCallOrCallvirt<Room>(nameof(Room.GetTile)));
				c.GotoPrev(MoveType.AfterLabel, x => x.MatchLdarg(0), x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)));
				c.Emit(OpCodes.Ldloca, 0);
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate((ref IntVector2 pos, GreenSparks self) =>
				{
					if (_implCWT.TryGetValue(self.room, out Handler handler))
					{
						pos = handler.RandomSpawnPos();
					}
				});
			}

			private static void GreenSpark_Update(ILContext il)
			{
				var c = new ILCursor(il);
				c.GotoNext(MoveType.After, x => x.MatchNewobj<Vector2>());
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate((Vector2 oldVec, GreenSparks.GreenSpark spark) =>
				{
					if (_implCWT.TryGetValue(spark.room, out Handler handler))
					{
						// Custom.LerpMap(this.life, 0f, 0.5f, -0.1f, 0.05f)
						Vector2 vec = 0.12f * handler.DirectionAt(spark.pos);
						vec.y -= Custom.LerpMap(spark.life, 0f, 0.5f, -0.15f, 0f);
						return vec;
					}
					return oldVec;
				});
			}
		}
	}
}

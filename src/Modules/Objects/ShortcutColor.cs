using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IL.MoreSlugcats;

namespace RegionKit.Modules.Objects
{
	/// <summary>
	/// Contains all the backend data for ColorifierUAD object to work with
	/// </summary>
	public class ShortcutColorifierData : ManagedData
	{
		const string redFieldKey = "R";
		const string greenFieldKey = "G";
		const string blueFieldKey = "B";
		/// <summary>
		/// Default constructor, does nothing but base call
		/// </summary>
		/// <param name="po"></param>
		public ShortcutColorifierData(PlacedObject po) : base(po, new ManagedField[] { })
		{}
		/// <summary>
		/// Red component of RGB
		/// </summary>
		[FloatField(redFieldKey, 0f, 1f, 0f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "R")]
		public float red;
		/// <summary>
		/// Green component of RGB
		/// </summary>
		[FloatField(greenFieldKey, 0f, 1f, 0f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "G")]
		public float green;
		/// <summary>
		/// Blue component of RGB
		/// </summary>
		[FloatField(blueFieldKey, 0f, 1f, 0f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "B")]
		public float blue;
		/// <summary>
		/// The radius of ColorifierUAD object within which it applies RGB overwrite
		/// </summary>
		[Vector2Field("Radius", defX: 80f, defY: 0f, Vector2Field.VectorReprType.circle)]
		public Vector2 radius;
	}

	internal class ColorifierUAD : UpdatableAndDeletable
	{
		private ShortcutColorifierData data;

		public ColorifierUAD(PlacedObject placedObject, Room room)
		{
			ShortcutColorifierData? maybedata = placedObject.data as ShortcutColorifierData;
			if (maybedata == null)
			{
				throw new ArgumentException($"{nameof(PlacedObject)} was null or didn't contain a {nameof(ShortcutColorifierData)} instance");
			}
			data = maybedata;
			this.room = room;
			On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;
		}

		private void ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
		{
			orig(self, timeStacker, camPos);
			if (!WorkInThisRoom(self.camera.room)) return;
			if (room?.shortcuts == null) return;
			foreach (int shortcutnumber in self.sprites.Keys)
			{
				IntVector2 pos = new IntVector2(shortcutnumber / self.room.TileHeight, shortcutnumber % self.room.TileHeight);
				Vector2 unsnappedPos = new Vector2(room.MiddleOfTile(pos).x, room.MiddleOfTile(pos).y);
				if ((data.owner.pos - unsnappedPos).magnitude < data.radius.magnitude)
				{
					self.sprites[shortcutnumber].color = new Color(data.red, data.green, data.blue);
				}
			}
			AccountForCreatures(self);
		}
		private bool WorkInThisRoom(Room room) => this.room == room;
		private void AccountForCreatures(ShortcutGraphics self)
		{
			foreach (ShortcutHandler.ShortCutVessel shortCutVessel in self.shortcutHandler.transportVessels)
			{
				int roomCoordHash3 = self.GetRoomCoordHash(shortCutVessel.pos);
				if (shortCutVessel.room == self.room.abstractRoom && self.sprites.ContainsKey(roomCoordHash3))
				{
					self.sprites[roomCoordHash3].color = self.ShortCutColor(shortCutVessel.creature, shortCutVessel.pos);
					if (shortCutVessel.creature.Template.shortcutSegments > 1)
					{
						for (int l = 0; l < shortCutVessel.lastPositions.Length; l++)
						{
							int roomCoordHash4 = self.GetRoomCoordHash(shortCutVessel.lastPositions[l]);
							if (self.sprites.ContainsKey(roomCoordHash4))
							{
								self.sprites[roomCoordHash4].color = self.ShortCutColor(shortCutVessel.creature, shortCutVessel.lastPositions[l]);
							}
						}
					}
				}
			}
		}
	}
}

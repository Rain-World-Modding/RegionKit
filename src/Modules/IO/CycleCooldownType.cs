using System.Text.RegularExpressions;
using DevInterface;

namespace RegionKit.Modules.IO
{
	public class CycleCooldownType : ManagedObjectType
	{
		public CycleCooldownType() : base("Cycle Cooldown", _Enums.DevObjectCategories.MoonsStuffIO.value, null, typeof(CycleCooldownData), typeof(CycleCooldownRepresentation))
		{
		}

		public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
		{
			return new CycleCooldown(placedObject, room);
		}

		public class CycleCooldownData : IOType.IOData
		{
			public int Cooldown;

			public CycleCooldownData(PlacedObject owner) : base(owner, null)
			{
				Cooldown = 1;
				NeedsControlPanel = true;
			}

			public override string ToString()
			{
				return base.ToString() + "~" + Cooldown;
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] arr = Regex.Split(s, "~");
				try
				{
					Cooldown = int.Parse(arr[base.FieldsWhenSerialized + 0]);
				}
				catch { }
			}
		}

		public class CycleCooldownRepresentation : IOType.IORepresentation, IDevUISignals
		{
			public class CooldownSlider : Slider
			{
				public CooldownSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, bool inheritButton, float titleWidth) : base(owner, IDstring, parentNode, pos, title, inheritButton, titleWidth)
				{
				}
			}

			public CooldownSlider cooldown;

			public PlacedObject PlacedObject;

			public CycleCooldownRepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
			{
				panel!.size = new Vector2(190f, 45f);
				PlacedObject = pObj;

				panel.subNodes.Add(cooldown = new CooldownSlider(this.owner, "cooldownSlider", this.panel, new Vector2(5f, 25f), "Cooldown", false, 180f));

				IOButton.size.x = 180;
				IOButton.fSprites[0].scaleX = 180f;

				IOPanel.pos = new Vector2(panel.size.x + 10f, 5f);
			}

			public override void Refresh()
			{
				base.Refresh();
			}

			public override void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				base.Signal(type, sender, message);
			}
		}
	}
}

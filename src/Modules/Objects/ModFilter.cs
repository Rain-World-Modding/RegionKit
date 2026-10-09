using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.Objects
{
	public static class ModFilter
	{
		public class ModFilterData : PlacedObject.FilterData
		{
			public Vector2 panelPos = new Vector2(0f, 100f);
			public string modId = "regionkit";
			public bool requires = true;

			public ModFilterData(PlacedObject owner) : base(owner)
			{
			}

			public override bool Active(RoomSettings roomSettings, SlugcatStats.Timeline timelinePoint)
			{
				return (ModManager.GetModById(modId) != null) == requires;
			}

			public override string ToString()
			{
				string s = string.Format(
					CultureInfo.InvariantCulture,
					"{0}~{1}~{2}~{3}~{4}",
					BaseSaveString(),
					panelPos.x,
					panelPos.y,
					modId,
					requires ? "1" : "0"
					);
				return s;
			}

			public override void FromString(string s)
			{
				string[] array = s.Split('~');
				handlePos.x = float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
				handlePos.y = float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
				panelPos.x = float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
				panelPos.y = float.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture);
				modId = array[4];
				requires = array[5] == "1";
			}
		}

		internal class ModFilterRepresentation : ResizeableObjectRepresentation
		{
			private ModFilterData Data => (pObj.data as ModFilterData)!;

			private readonly Panel panel;
			private readonly FSprite connector;

			public ModFilterRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name, true)
			{
				panel = new Panel(owner, "Panel", this, Data.panelPos, pObj.type.ToString());
				subNodes.Add(panel);

				connector = new FSprite("pixel") { anchorY = 0f };
				fSprites.Add(connector);
				owner.placedObjectsContainer.AddChild(connector);
			}

			public override void Refresh()
			{
				base.Refresh();
				connector.SetPosition(absPos);
				connector.scaleY = panel.pos.magnitude;
				connector.rotation = Custom.AimFromOneVectorToAnother(absPos, panel.absPos);
				Data.panelPos = panel.pos;
			}

			private class Panel : DevInterface.Panel
			{
				private ModFilterRepresentation Rep => (parentNode as ModFilterRepresentation)!;
				private StringControl modIdInput;
				private Cycler validityCycler;

				public Panel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 45f), title)
				{
					subNodes.Add(new DevUILabel(owner, "ID_Label", this, new Vector2(5f, 25f), 80f, "Mod id:"));
					subNodes.Add(modIdInput = new StringControl(owner, "ID_Input", this, new Vector2(90f, 25f), 155f, Rep.Data.modId, StringControl.TextIsAny));
					subNodes.Add(validityCycler = new Cycler(owner, "Valid_Input", this, new Vector2(5f, 5f), 240f, "Validity: ", ["when mod is enabled", "when mod is disabled"]));
					validityCycler.currentAlternative = (Rep.Data.requires ? 0 : 1);
					validityCycler.Text = validityCycler.baseName + validityCycler.alternatives[validityCycler.currentAlternative];
				}

				public override void Update()
				{
					Rep.Data.modId = modIdInput.actualValue;
					Rep.Data.requires = (validityCycler.currentAlternative == 0);
					base.Update();
				}
			}
		}
	}
}

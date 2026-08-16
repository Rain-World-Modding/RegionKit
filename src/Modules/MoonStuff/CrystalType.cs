using System.Text.RegularExpressions;
using DevInterface;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class CrystalType : ManagedObjectType
	{
		public CrystalType() : base("Crystal", Objects._Module.DECORATIONS_POM_CATEGORY, null, typeof(CrystalData), typeof(CrystalRepresentation)) { }

		public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room) => new Crystal(placedObject, room);

		public class CrystalData : ManagedData
		{
#pragma warning disable 0649
			[IntegerField("lay", 1, 30, 1, ManagedFieldWithPanel.ControlType.slider, "Layer:")]
			public int lay;

			[FloatField("mid", 0f, 1f, 0.5f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "Midpoint:")]
			public float mid;

			[FloatField("midw", 0f, 1f, 0.5f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "Midpoint Width:")]
			public float midw;

			[FloatField("basew", 0f, 1f, 0.5f, 0.1f, ManagedFieldWithPanel.ControlType.slider, "Base Width:")]
			public float basew;

			[BackedByField("pos")]
			public Vector2 pos;
#pragma warning restore 0649

			private static ManagedField[] customFields = new ManagedField[] {
				 new Vector2Field("pos", new Vector2(0, 50), Vector2Field.VectorReprType.line)
			};

			public float CrystalHue;

			public float CrystalSat;

			public float CrystalLit;

			public CrystalData(PlacedObject owner) : base(owner, customFields)
			{
				CrystalHue = -1f;
				CrystalSat = -1f;
				CrystalLit = -1f;
			}


			public override string ToString()
			{
				return base.ToString() + "~" + CrystalHue + "~" + CrystalSat + "~" + CrystalLit;
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] arr = Regex.Split(s, "~");
				try
				{
					CrystalHue = float.Parse(arr[base.FieldsWhenSerialized + 0]);
					CrystalSat = float.Parse(arr[base.FieldsWhenSerialized + 1]);
					CrystalLit = float.Parse(arr[base.FieldsWhenSerialized + 2]);
				}
				catch { }
			}
		}

		public class CrystalRepresentation : ManagedRepresentation, IDevUISignals
		{
			public class CrystalSlider : Slider
			{
				public PlacedObject pObj;
				public CrystalSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, bool inheritButton, float titleWidth)
					: base(owner, IDstring, parentNode, pos, title, inheritButton, titleWidth)
				{
					pObj = (parentNode.parentNode as CrystalRepresentation).pObj;
					subNodes[0].fSprites[0].scaleX = 100f;
					subNodes[1].fSprites[0].scaleX = 24f;
					(subNodes[1] as DevUILabel).pos.x = titleWidth;
				}

				public override void Refresh()
				{
					base.Refresh();
					float nubPos = 0f;


					if (IDstring == "CrystalHue")
					{
						if ((pObj.data as CrystalData).CrystalHue == -1f)
						{
							NumberText = "Def";
						}
						else
						{
							nubPos = (pObj.data as CrystalData).CrystalHue;
							NumberText = (Mathf.Round((pObj.data as CrystalData).CrystalHue * 100) / 100).ToString();
						}
					}
					if (IDstring == "CrystalSat")
					{
						if ((pObj.data as CrystalData).CrystalSat == -1f)
						{
							NumberText = "Def";
						}
						else
						{
							nubPos = (pObj.data as CrystalData).CrystalSat;
							NumberText = (Mathf.Round((pObj.data as CrystalData).CrystalSat * 100) / 100).ToString();
						}
					}
					if (IDstring == "CrystalLit")
					{
						if ((pObj.data as CrystalData).CrystalLit == -1f)
						{
							NumberText = "Def";
						}
						else
						{
							nubPos = (pObj.data as CrystalData).CrystalLit;
							NumberText = (Mathf.Round((pObj.data as CrystalData).CrystalLit * 100) / 100).ToString();
						}
					}

					RefreshNubPos(nubPos);
				}

				public override void NubDragged(float nubPos)
				{
					RefreshNubPos(nubPos);

					if (IDstring == "CrystalHue")
					{
						(pObj.data as CrystalData).CrystalHue = nubPos;
					}
					if (IDstring == "CrystalSat")
					{
						(pObj.data as CrystalData).CrystalSat = nubPos;
					}
					if (IDstring == "CrystalLit")
					{
						(pObj.data as CrystalData).CrystalLit = nubPos;
					}

					parentNode.parentNode.Refresh();
					Refresh();
				}
			}

			public Button Reset;
			public CrystalSlider Hue;
			public CrystalSlider Sat;
			public CrystalSlider Lit;
			public CrystalRepresentation(PlacedObject.Type placedType, DevInterface.ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
			{
				for (int i = 0; i < panel.subNodes.Count; i++)
				{
					(panel.subNodes[i] as Slider).pos = new Vector2(5, i * 20f + 85f);
				}

				panel.subNodes.Add(Reset = new Button(owner, "Default", panel, new Vector2(5, 65), 64f, "Default"));
				panel.subNodes.Add(Hue = new CrystalSlider(owner, "CrystalHue", panel, new Vector2(5, 45), "Hue:", false, 110f));
				panel.subNodes.Add(Sat = new CrystalSlider(owner, "CrystalSat", panel, new Vector2(5, 25), "Sat:", false, 110f));
				panel.subNodes.Add(Lit = new CrystalSlider(owner, "CrystalLit", panel, new Vector2(5, 5), "Lit:", false, 110f));

				panel.size = new Vector2(250f, 165f);
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (sender.IDstring == "Default")
				{
					(pObj.data as CrystalData).CrystalHue = -1f;
					(pObj.data as CrystalData).CrystalSat = -1f;
					(pObj.data as CrystalData).CrystalLit = -1f;
				}
			}
		}
	}
}

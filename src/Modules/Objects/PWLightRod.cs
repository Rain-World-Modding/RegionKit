/* Authors: DeltaTime & Woodensponge */
using DevInterface;

namespace RegionKit.Modules.Objects;
/// <summary>
/// A recolorable light rod
/// </summary>
public class PWLightRod : SSLightRod
{
	/// <summary>
	/// 
	/// </summary>
	public PWLightRod(PlacedObject placedObject, Room room) : base(placedObject, room)
	{
		this.color = (this.rodData as PWLightRodData)!.color;
		this.lights.Clear();
		this.UpdateLightAmount();
	}
	///Changes the light color if the color of the rod changes
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (!color.Equals((rodData as PWLightRodData)!.color))
		{
			for (int i = 0; i < lights.Count; i++)
			{
				lights[i].light.Destroy();
			}
			lights.Clear();
			color = (rodData as PWLightRodData)!.color;
			UpdateLightAmount();
		}
	}
}
internal class PWLightRodData : PlacedObject.SSLightRodData
{
	public PWLightRodData(PlacedObject owner) : base(owner)
	{
		color = new Color(0.4f, 1f, 0.8f);
	}

	public Color color;

	//reads additional values for the color variables (if they exist)
	public override void FromString(string s)
	{
		base.FromString(s);
		string[] array = System.Text.RegularExpressions.Regex.Split(s, "~");
		if (array.Length >= 9)
		{
			this.color = new Color(float.Parse(array[6]), float.Parse(array[7]), float.Parse(array[8]));
		}
		else
		{
			this.color = new Color(0.4f, 1f, 0.8f);
		}
	}

	//color values are added if its not the defualt color.
	public override string ToString()
	{
		string s = base.ToString();
		if (!this.color.Equals(new Color(0.4f, 1f, 0.8f)))
		{
			return string.Concat(new object[] {
				s,
				"~",
				color.r.ToString(),
				"~",
				color.g.ToString(),
				"~",
				color.b.ToString()
			});
		}
		return s;
	}
}

class PWLightRodRepresentation : SSLightRodRepresentation
{
	//Removes the subnode that base SSlightrodRepresentation adds, and adds its own.
	public PWLightRodRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name, bool isNewObject) : base(owner, IDstring, parentNode, pObj, "PW Light rod")
	{
		fLabels.Last().text = "PW Lightrod";
		for (int i = 0; i < subNodes.Count; ++i)
		{
			subNodes[0].ClearSprites();
		}
		subNodes.Clear();
		subNodes.Add(new PWLightrodControlPanel(owner, "PW_Light_Rod_Panel", this, new Vector2(0f, 100f)));
		(this.subNodes[this.subNodes.Count - 1] as PWLightrodControlPanel)!.pos = (pObj.data as PWLightRodData)!.panelPos;
		rod.Destroy();
		//Prevents bug where a new temporary lightrod is added every time you click on the objects page button.
		if (isNewObject)
		{
			rod = new PWLightRod(pObj, owner.room);
			owner.room.AddObject(rod);
		}
	}

	public override void Update()
	{
		base.Update();
	}

	public class PWLightrodControlPanel : SSLightRodControlPanel
	{
		//Adds new sliders for color.
		public PWLightrodControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
		{
			size = new Vector2(250f, 150f);
			fLabels[0].text = "PW Lightrod";
			subNodes.Add(new PWDepthControlSlider(owner, "ColorR_Slider", this, new Vector2(5f, 125f), "Color R: "));
			subNodes.Add(new PWDepthControlSlider(owner, "ColorG_Slider", this, new Vector2(5f, 105f), "Color G: "));
			subNodes.Add(new PWDepthControlSlider(owner, "ColorB_Slider", this, new Vector2(5f, 85f), "Color B: "));
		}

		public class PWDepthControlSlider : DepthControlSlider
		{
			public PWDepthControlSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title)
			{
			}
			//Sets sliders current value.
			public override void Refresh()
			{
				base.Refresh();
				float num = 0f;
				string idstring = IDstring;
				Color rodColor = ((parentNode.parentNode as PWLightRodRepresentation)!.pObj.data as PWLightRodData)!.color;
				switch (idstring)
				{
				case "ColorR_Slider":
					num = rodColor.r;
					NumberText = ((int)(num * 255)).ToString();
					break;
				case "ColorG_Slider":
					num = rodColor.g;
					NumberText = ((int)(num * 255)).ToString();
					break;
				case "ColorB_Slider":
					num = rodColor.b;
					NumberText = ((int)(num * 255)).ToString();
					break;
				}
				RefreshNubPos(num);
			}
			//Allows slider values to change color.
			public override void NubDragged(float nubPos)
			{
				PWLightRodData
					lightrodColor = ((parentNode.parentNode as PWLightRodRepresentation)!.pObj.data as PWLightRodData)!;
				switch (IDstring)
				{
				case "ColorR_Slider":
					lightrodColor.color.r = nubPos;
					break;
				case "ColorG_Slider":
					lightrodColor.color.g = nubPos;
					break;
				case "ColorB_Slider":
					lightrodColor.color.b = nubPos;
					break;
				}
				base.NubDragged(nubPos);
			}
		}
	}
}

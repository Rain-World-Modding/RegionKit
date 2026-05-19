using DevInterface;
using RegionKit.Modules.DevUIMisc;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using static RegionKit.Modules.BackgroundBuilder.BackgroundElementData;

namespace RegionKit.Modules.BackgroundBuilder;

public class ElementListPanel : Panel, IDevUISignals, ElementListPanel.ICollectElementButtons
{
	List<ElementDataButton> elements = new();

	public List<ElementDataButton> Elements
	{
		get => elements;

		set => elements = value;
	}

	private bool _groupsOnly;
	public bool GroupsOnly => _groupsOnly;

	IEnumerable<CustomBgElement> ICollectElementButtons.DataElements
	{
		get
		{
			if (GroupsOnly)
				yield return null!;
			foreach (var s in RoomSettings.BackgroundData().sceneData.backgroundElements)
				if (!GroupsOnly || s is BG_ElementGroup)
					yield return s;
		}
	}

	IEnumerable<CustomBgElement> ICollectElementButtons.GetRevealedElements => GetRevealedElements(this);


	ScrollBar scrollBar;
	public ElementListPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, string title, bool groupsOnly = false) : base(owner, IDstring, parentNode, pos, size, title)
	{
		this._groupsOnly = groupsOnly;
		subNodes.Add(scrollBar = new ScrollBar(owner, "scroll", this, new Vector2(5f, 5f), this.size.y - 20f, 32f));

		MakeElementButtons(this, 25f);
	}

	public class ElementDataButton : Button
	{
		internal CustomBgElement element;
		protected bool hidden = false;
		internal DeleteButton? deleteButton;
		internal ElementDataButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string name, CustomBgElement element) : base(owner, IDstring, parentNode, pos, width, name)
		{
			this.element = element;
			if (!(parentNode is ICollectElementButtons collection && collection.GroupsOnly))
			{
				this.deleteButton = new DeleteButton(owner, "delete_" + IDstring, this, new Vector2(width + 4f, 0f));
				subNodes.Add(deleteButton);
			}
		}

		public override void Update()
		{
			if (!hidden)
				base.Update();

			if (element != null)
			{
				string s = element.DevUIName();
				if (this.Text != s)
					this.Text = s;
				Refresh();
			}
		}

		public virtual float height => 20f;

		public virtual void ScrollVisibilityUpdate(float relativePos, float height)
		{
			hidden = relativePos < 5f || relativePos > height - 15f;
			Refresh();
		}

		public override void Refresh()
		{
			base.Refresh();
			if (hidden)
			{
				base.MoveSprite(0, new Vector2(-1000f, -1000f));
				base.MoveLabel(0, new Vector2(-1000f, -1000f));
			}
		}


		public class DeleteButton : Button
		{
			public DeleteButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, 16, "-")
			{
				fLabels[0].alignment = FLabelAlignment.Center;
				fLabels[0].anchorY = 0.5f;
				fLabels[0].scale = 1.75f;
			}

			public override void Clicked()
			{
				base.Clicked();
				Refresh();
				this.SendSignal(DevUISignalType.ButtonClick, this, "delete");
			}

			public override void Refresh()
			{
				base.Refresh();

				if ((parentNode as ElementDataButton)!.hidden)
				{
					base.MoveSprite(0, new Vector2(-1000f, -1000f));
					base.MoveLabel(0, new Vector2(-1000f, -1000f));
				}
				else
				{
					base.MoveLabel(0, absPos + (size / 2));
				}
			}
		}

	}

	public class GroupButton : ElementDataButton, IDevUISignals, ICollectElementButtons
	{
		public bool Dropped => dropdownButton.dropped;

		DropdownButton dropdownButton;
		List<ElementDataButton> elements = new();
		public override float height => elements.Sum(x => x.height) + 20f;
		public List<ElementDataButton> Elements { get => elements; set => elements = value; }
		IEnumerable<CustomBgElement> ICollectElementButtons.DataElements
		{
			get
			{
				if (group != null)
				{
					foreach (var s in group.elements)
						if (!GroupsOnly || s is BG_ElementGroup)
							yield return s;
				}
			}
		}

		IEnumerable<CustomBgElement> ICollectElementButtons.GetRevealedElements => GetRevealedElements(this);

		private bool _groupsOnly;
		public bool GroupsOnly => _groupsOnly;

		internal BG_ElementGroup? group => (this.element as BG_ElementGroup);
		internal GroupButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string name, BG_ElementGroup? element, bool groupsOnly = false) : base(owner, IDstring, parentNode, pos, width, name, element!)
		{
			_groupsOnly = groupsOnly;
			if (group != null)
				this.Text = "GROUP: " + group.name;
			else
				this.Text = "None";
			dropdownButton = new DropdownButton(owner, "drop_" + IDstring, this, new Vector2(-20, 0));
			subNodes.Add(dropdownButton);
		}

		public override void Update()
		{
			if (!hidden)
			{
				base.Update();
				if ((this as ICollectElementButtons).DataElements.Count() + (GroupsOnly ? 0 : 1) != elements.Count())
				{
					RefreshElements();
				}
			}
			else //keep updating subnodes even if the main node is hidden
			{
				for (int i = subNodes.Count - 1; i >= 0; i--)
				{
					if (subNodes[i] != dropdownButton && subNodes[i] != deleteButton) //don't update dropdown button tho
						subNodes[i].Update();
				}
				if (initRefresh)
				{
					Refresh();
					initRefresh = false;
				}
			}
		}
		public void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender == dropdownButton)
			{
				if (dropdownButton.dropped)
				{
					MakeElementButtons(this, 0f);
				}
				else
				{
					foreach (var s in elements)
					{
						s.ClearSprites();
						subNodes.Remove(s);
					}
					elements.Clear();
				}
				if (this.parentNode is ElementListPanel list)
					list.RefreshElementButtonPos();
				else if (this.parentNode is GroupButton parentGroup)
					parentGroup.RefreshElementButtonPos();


				this.SendSignal(type, sender, message);
			}
			else
			{
				this.SendSignal(type, sender, message);
			}
		}

		public void RefreshElementButtonPos()
		{
			float ppos = -20f;
			foreach (var s in elements)
			{
				s.Move(new Vector2(s.pos.x, ppos));
				ppos -= s.height;
			}

			if (this.parentNode is ElementListPanel list)
				list.RefreshElementButtonPos();
			else if (this.parentNode is GroupButton parentGroup)
				parentGroup.RefreshElementButtonPos();
		}

		public override void ScrollVisibilityUpdate(float relativePos, float height)
		{
			foreach (var e in elements)
			{
				e.ScrollVisibilityUpdate(relativePos + e.pos.y, height);
			}
			base.ScrollVisibilityUpdate(relativePos, height);
		}

		public void RefreshElements()
		{
			if (!Dropped)
				return;
			ElementListPanel.RefreshElements(this, 0f);
		}

		public class DropdownButton : Button
		{
			public bool dropped = false;

			GroupButton groupButton => (parentNode as GroupButton)!;
			public bool Hidden => groupButton.hidden || (groupButton._groupsOnly && GroupsOnlyHidden);

			public bool GroupsOnlyHidden => groupButton is ICollectElementButtons collection && !collection.DataElements.Any(x => x is BG_ElementGroup);

			public DropdownButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, 16, ">")
			{
				fLabels[0].alignment = FLabelAlignment.Center;
			}

			public override void Clicked()
			{
				if (Hidden)
					return;

				base.Clicked();
				dropped = !dropped;
				Refresh();
				this.SendSignal(DevUISignalType.ButtonClick, this, dropped ? "drop" : "undrop");
			}

			public override void Refresh()
			{
				base.Refresh();
				Text = dropped ? "V" : ">";


				if (Hidden)
				{
					base.MoveSprite(0, new Vector2(-1000f, -1000f));
					base.MoveLabel(0, new Vector2(-1000f, -1000f));
				}
				else
				{
					base.MoveLabel(0, absPos + new Vector2(size.x / 2f, 0f));
				}
			}
		}
	}

	public class AddButton : ElementDataButton
	{
		internal AddButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, BG_ElementGroup? group) : base(owner, IDstring, parentNode, pos, width, "+", group!)
		{
			Text = "+";
			fLabels[0].alignment = FLabelAlignment.Center;
			fLabels[0].scale = 2f;
			fLabels[0].anchorY = 0.5f;
			if (deleteButton != null)
			{
				deleteButton.ClearSprites();
				subNodes.Remove(deleteButton);
			}
		}


		public override void Refresh()
		{
			base.Refresh();

			if (!hidden)
			{
				base.MoveLabel(0, absPos + (size / 2));
				this.Text = "+";
			}
		}
	}

	public void RefreshElements()
	{
		RefreshElements(this, 25f);
	}

	public void RefreshElementButtonPos()
	{
		float heightDiff = Mathf.Max(0f, elements.Sum(x => x.height) - size.y + 20f);
		float offset = Mathf.Lerp(heightDiff, 0f, scrollBar.value);
		float ppos = size.y - 20f + offset;
		foreach (var s in elements)
		{
			s.Move(new Vector2(s.pos.x, ppos));

			s.ScrollVisibilityUpdate(ppos, this.size.y);
			ppos -= s.height;
		}
	}

	public override void Update()
	{
		base.Update();

		if ((this as ICollectElementButtons).DataElements.Count() + (GroupsOnly ? 0 : 1) != elements.Count())
		{
			RefreshElements();
		}
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (sender == this.scrollBar)
		{
			RefreshElementButtonPos();
		}
		else
		{
			this.SendSignal(type, sender, message);
		}
	}

	public class ScrollBar : PositionedDevUINode
	{
		float height;
		float nubHeight;
		ScrollNub nub;
		public float value = 1f;
		public ScrollBar(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float height, float nubHeight) : base(owner, IDstring, parentNode, pos)
		{
			this.height = height;
			this.nubHeight = nubHeight;
			fSprites.Add(new FSprite("pixel", true) { scaleY = height, scaleX = 2, anchorX = 0f, anchorY = 0f, alpha = 0.5f });
			Futile.stage.AddChild(fSprites[0]);
			nub = new ScrollNub(owner, "nub_" + IDstring, this, Vector2.zero);
			subNodes.Add(nub);
		}

		public void NubDragged(float newValue)
		{
			value = newValue;

			this.SendSignal(GenericSlider.SliderUpdate, this, newValue.ToString());
		}

		public override void Refresh()
		{
			base.Refresh();
			base.MoveSprite(0, absPos + new Vector2(7f, 0f));
		}

		public class ScrollNub : RectangularDevUINode
		{
			ScrollBar Bar => (parentNode as ScrollBar)!;
			public ScrollNub(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(16f, 32f))
			{
				fSprites.Add(new FSprite("pixel", true) { scaleY = size.y, scaleX = size.x, anchorX = 0f, anchorY = 0f });
				Futile.stage.AddChild(fSprites[0]);
				Move(new Vector2(pos.x, Mathf.Lerp(0f, Bar.height - Bar.nubHeight, Bar.value)));
			}

			public override void Update()
			{
				base.Update();
				if (held)
				{
					owner.draggedNode = this;
					fSprites[fSprites.Count - 1].color = new Color(0.25f, 0.25f, 0.75f);
				}
				else
				{
					fSprites[fSprites.Count - 1].color = (base.MouseOver ? new Color(0.75f, 0.25f, 0.25f) : new Color(0.5f, 0.5f, 0.5f));
				}
				if (owner.mouseClick && base.MouseOver)
				{
					held = true;
					mousePosOffset = absPos.y - owner.mousePos.y;
				}
				if (held)
				{
					Bar.NubDragged(Mathf.InverseLerp(Bar.absPos.y, Bar.absPos.y + Bar.height - Bar.nubHeight, owner!.mousePos.y + mousePosOffset));
					Move(new Vector2(pos.x, Mathf.Lerp(0f, Bar.height - Bar.nubHeight, Bar.value)));
				}
				if (held && !owner.mouseDown)
				{
					held = false;
				}
			}

			public override void Refresh()
			{
				base.Refresh();
				fSprites[0].scaleY = Bar.nubHeight;
				base.MoveSprite(0, absPos);
			}

			public bool held;

			public float mousePosOffset;
		}
	}

	#region ICollectElementButtons
	public interface ICollectElementButtons
	{
		public List<ElementDataButton> Elements { get; set; }
		internal IEnumerable<CustomBgElement> DataElements { get; }
		internal IEnumerable<CustomBgElement> GetRevealedElements { get; }
		public void RefreshElements();
		public void RefreshElementButtonPos();
		public bool GroupsOnly { get; }
	}


	//workaround to no interface default implementations
	internal static void MakeElementButtons(ICollectElementButtons collection, float xPos)
	{
		if (collection is not DevUINode node)
			return;

		foreach (var s in collection.DataElements)
		{
			ElementDataButton? newButton = null;
			if (s is BG_ElementGroup group)
			{
				newButton = new GroupButton(node.owner, group.name, node, new Vector2(xPos + 20f, 0f), 200f, group.DevUIName(), group, collection.GroupsOnly);
			}
			else if (s is null)
			{
				newButton = new GroupButton(node.owner, "none", node, new Vector2(xPos, 0f), 200f, "None", null, true);
			}
			else if (!collection.GroupsOnly)
			{
				newButton = new ElementDataButton(node.owner, s.Serialize(), node, new Vector2(xPos, 0f), 200f, s.DevUIName(), s);
			}

			if (newButton != null)
			{
				collection.Elements.Add(newButton);
				node.subNodes.Add(newButton);
			}
		}

		BG_ElementGroup? group2 = null;
		if (node is GroupButton groupButton)
			group2 = groupButton.group;

		if (!collection.GroupsOnly)
		{
			var add = new AddButton(node.owner, "add", node, new Vector2(xPos + 20f, 0f), 160f, group2);
			collection.Elements.Add(add);
			node.subNodes.Add(add);
		}

		collection.RefreshElementButtonPos();
	}

	internal static void RefreshElements(ICollectElementButtons collection, float xPos)
	{
		if (collection is not DevUINode node)
			return;

		List<CustomBgElement> dataElements = collection.DataElements.ToList();

		ElementDataButton[] newList = new ElementDataButton[dataElements.Count + 1];

		for (int i = collection.Elements.Count - 1; i >= 0; i--)
		{
			if (collection.Elements[i] is AddButton)
			{
				newList[^1] = collection.Elements[i];
				continue;
			}

			if (!dataElements.Contains(collection.Elements[i].element))
			{
				node.subNodes.Remove(collection.Elements[i]);
				collection.Elements[i].ClearSprites();
				collection.Elements.RemoveAt(i);
			}
			else
			{
				newList[dataElements.IndexOf(collection.Elements[i].element)] = collection.Elements[i];
			}
		}

		for (int i = 0; i < newList.Length; i++)
		{
			if (newList[i] == null && i < dataElements.Count)
			{
				CustomBgElement s = dataElements[i];
				ElementDataButton? newButton = null;
				if (s is BG_ElementGroup group)
				{
					newButton = new GroupButton(node.owner, group.name, node, new Vector2(xPos + 20f, 0f), 200f, group.DevUIName(), group, collection.GroupsOnly);
				}
				else if (s is null)
				{
					newButton = new GroupButton(node.owner, "none", node, new Vector2(xPos, 0f), 200f, "None", null, true);
				}
				else if (!collection.GroupsOnly)
				{
					newButton = new ElementDataButton(node.owner, s.Serialize(), node, new Vector2(xPos, 0f), 200f, s.DevUIName(), s);
				}

				if (newButton != null)
				{
					newList[i] = newButton;
					node.subNodes.Add(newButton);
				}
			}
			else if (newList[i] is ICollectElementButtons subCollection)
				subCollection.RefreshElements();
		}

		collection.Elements = newList.Where(x => x != null).ToList();
		collection.RefreshElementButtonPos();
	}
	internal static IEnumerable<CustomBgElement> GetRevealedElements(ICollectElementButtons collection)
	{
		foreach (var element in collection.Elements)
		{
			if (element is GroupButton group)
			{
				if (!group.Dropped)
					yield return group.element;
				else
				{
					foreach (var e2 in (group as ICollectElementButtons).GetRevealedElements)
					{
						yield return e2;
					}
				}
			}
			else if (element is not AddButton)
			{
				yield return element.element;
			}
		}
	}
	#endregion

}

public class ElementGroupSelectButton : Button, IDevUISignals
{
	ElementListPanel? panel = null;

	internal BG_ElementGroup? actualValue;

	public Vector2 panelPos = new Vector2(200f, -20f);

	public Vector2 panelSize = new Vector2(300f, 400f);
	internal ElementGroupSelectButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, BG_ElementGroup initialValue)
		: base(owner, IDstring, parentNode, pos, width, initialValue?.name ?? "None")
	{
		actualValue = initialValue;
	}

	string groupName => actualValue?.name ?? "None";

	public override void Clicked()
	{
		base.Clicked();


		if (panel != null)
		{
			subNodes.Remove(panel);
			panel.ClearSprites();
			panel = null;
		}
		else
		{
			panel = new ElementListPanel(owner, "", this, panelPos - pos, panelSize, "Select Group", true);
			subNodes.Add(panel);
		}
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (panel != null && sender is ElementListPanel.GroupButton group)
		{
			subNodes.Remove(panel);
			panel.ClearSprites();
			panel = null;
			actualValue = group.group;
			Text = groupName;

			//send signal up
			this.SendSignal(DevUISignalType.ButtonClick, this, message);
		}
	}
}


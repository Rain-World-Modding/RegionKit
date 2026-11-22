using System.Runtime.CompilerServices;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc
{
	/// <summary>
	/// Objects category browser, original by MyztiLeaf, integrated with permission by Alduris.
	/// Effects category browser by Alduris based on MyztiLeaf's code.
	/// </summary>
	public static class CategoryBrowsers
	{
		private static readonly ConditionalWeakTable<ObjectsPage, BrowserState> _objectStates = new();
		private static readonly ConditionalWeakTable<RoomSettingsPage, BrowserState> _effectStates = new();

		private sealed class BrowserState
		{
			public bool InCategoryView;
			public int CategoryPage;
			public int TotalCategoryPages;
			public bool NeedsRefresh;
		}

		internal static void Apply()
		{
			// Objects page
			On.DevInterface.ObjectsPage.RefreshObjectsPage += ObjectsPage_RefreshObjectsPage;
			On.DevInterface.ObjectsPage.Signal += ObjectsPage_Signal;
			On.DevInterface.ObjectsPage.Update += ObjectsPage_Update;

			// Effects page
			On.DevInterface.RoomSettingsPage.RefreshEffectsPage += RoomSettingsPage_RefreshEffectsPage;
			On.DevInterface.RoomSettingsPage.Signal += RoomSettingsPage_Signal;
			On.DevInterface.RoomSettingsPage.ctor += RoomSettingsPage_ctor;
		}

		internal static void Undo()
		{
			// Objects page
			On.DevInterface.ObjectsPage.RefreshObjectsPage -= ObjectsPage_RefreshObjectsPage;
			On.DevInterface.ObjectsPage.Signal -= ObjectsPage_Signal;
			On.DevInterface.ObjectsPage.Update -= ObjectsPage_Update;

			// Effects page
			On.DevInterface.RoomSettingsPage.RefreshEffectsPage -= RoomSettingsPage_RefreshEffectsPage;
			On.DevInterface.RoomSettingsPage.Signal -= RoomSettingsPage_Signal;
			On.DevInterface.RoomSettingsPage.ctor -= RoomSettingsPage_ctor;
		}

		#region Objects page

		private static void ObjectsPage_RefreshObjectsPage(On.DevInterface.ObjectsPage.orig_RefreshObjectsPage orig, ObjectsPage self)
		{
			BrowserState state = _objectStates.GetValue(self, _ => new());
			if (!state.InCategoryView)
			{
				orig(self);
				self.objectsPanel.subNodes.Add(new Button(self.owner, "Categories_Button", self.objectsPanel, new Vector2(205f, self.objectsPanel.size.y - 16f - 5f), 95f, "Categories"));
				return;
			}

			Panel panel = self.objectsPanel;
			if (panel == null) return;
			for (int i = panel.subNodes.Count - 1; i >= 2; i--)
			{
				panel.subNodes[i].ClearSprites();
				panel.subNodes.RemoveAt(i);
			}

			self.objectsPanel.subNodes.Add(new Button(self.owner, "Category:", self.objectsPanel, new Vector2(205f, self.objectsPanel.size.y - 16f - 5f), 95f, "Back to Objects"));
			List<(string Name, int FirstPage)> categories = CollectCategories(self);
			int perPage = Math.Max(2, self.maxObjectsPerPage);
			state.TotalCategoryPages = Math.Max(1, (categories.Count + perPage - 1) / perPage);
			if (state.CategoryPage >= state.TotalCategoryPages) state.CategoryPage = 0;
			panel.Title = state.TotalCategoryPages > 1 ? $"CATEGORIES ({state.CategoryPage + 1}/{state.TotalCategoryPages})" : "CATEGORIES";

			int start = state.CategoryPage * perPage;
			float rowsPerColumn = perPage / 2f;
			for (int i = 0; i < perPage && start + i < categories.Count; i++)
			{
				int col = Mathf.FloorToInt(i / rowsPerColumn);
				float x = 5f + (col * 195f);
				float y = panel.size.y - 16f - 35f - (20f * i) + (20f * rowsPerColumn * col);
				string categoryName = categories[start + i].Name;
				var btn = new Button(self.owner, "Category:" + categoryName, panel, new Vector2(x, y), 190f, categoryName);
				panel.subNodes.Add(btn);
				btn.Refresh();
			}
		}

		private static void ObjectsPage_Update(On.DevInterface.ObjectsPage.orig_Update orig, ObjectsPage self)
		{
			orig(self);
			BrowserState state = _objectStates.GetValue(self, _ => new());
			if (state.NeedsRefresh)
			{
				state.NeedsRefresh = false;
				self.RefreshObjectsPage();
			}
		}

		private static void ObjectsPage_Signal(On.DevInterface.ObjectsPage.orig_Signal orig, ObjectsPage self, DevUISignalType type, DevUINode sender, string message)
		{
			if (type == DevUISignalType.ButtonClick && sender != null)
			{
				BrowserState state = _objectStates.GetValue(self, _ => new());
				string id = sender?.IDstring ?? string.Empty;

				if (id == "Categories_Button")
				{
					state.InCategoryView = !state.InCategoryView;
					state.CategoryPage = 0;
					state.NeedsRefresh = true;
					return;
				}

				if (state.InCategoryView)
				{
					if (id == "Prev_Button")
					{
						if (state.TotalCategoryPages > 0)
						{
							state.CategoryPage = (state.CategoryPage - 1 + state.TotalCategoryPages) % state.TotalCategoryPages;
						}
						state.NeedsRefresh = true;
						return;
					}

					if (id == "Next_Button")
					{
						if (state.TotalCategoryPages > 0)
						{
							state.CategoryPage = (state.CategoryPage + 1) % state.TotalCategoryPages;
						}
						state.NeedsRefresh = true;
						return;
					}

					if (id.StartsWith("Category:", StringComparison.Ordinal))
					{
						foreach ((string Name, int FirstPage) in CollectCategories(self))
						{
							if (id[9..].Equals(Name, StringComparison.OrdinalIgnoreCase))
							{
								self.currObjectsPage = FirstPage;
								break;
							}
						}

						state.InCategoryView = false;
						state.CategoryPage = 0;
						state.NeedsRefresh = true;
						return;
					}
				}
			}
			orig(self, type, sender, message);
		}


		private static List<(string Name, int FirstPage)> CollectCategories(ObjectsPage self) // noms all categories - snowflake
		{
			var list = new List<(string Name, int FirstPage)>();
			PlacedObject.Type[] types = self.placedObjectTypes;
			if (types == null || types.Length == 0) return list;

			int perPage = Math.Max(1, self.maxObjectsPerPage);
			HashSet<string> collectedCategories = [];
			for (int i = 0; i < types.Length; i++)
			{
				PlacedObject.Type type = types[i];
				if (type == null || type == PlacedObject.Type.None) continue;

				ObjectsPage.DevObjectCategories Getcategory = self.DevObjectGetCategoryFromPlacedType(type);
				string? categoryName = Getcategory?.value;
				if (string.IsNullOrEmpty(categoryName) || !collectedCategories.Add(categoryName!)) continue;
				list.Add((categoryName!, i / perPage));
			}
			return list;
		}

		#endregion

		#region Effects page

		private static void RoomSettingsPage_RefreshEffectsPage(On.DevInterface.RoomSettingsPage.orig_RefreshEffectsPage orig, RoomSettingsPage self)
		{
			BrowserState state = _effectStates.GetValue(self, _ => new());
			if (!state.InCategoryView)
			{
				orig(self);
				self.effectsPanel.subNodes.Add(new Button(self.owner, "Categories_Button", self.effectsPanel, new Vector2(205f, self.effectsPanel.size.y - 16f - 5f), 95f, "Categories"));
				return;
			}

			Panel panel = self.effectsPanel;
			if (panel == null) return;
			for (int i = panel.subNodes.Count - 1; i >= 2; i--)
			{
				panel.subNodes[i].ClearSprites();
				panel.subNodes.RemoveAt(i);
			}

			self.effectsPanel.subNodes.Add(new Button(self.owner, "Category:", self.effectsPanel, new Vector2(205f, self.effectsPanel.size.y - 16f - 5f), 95f, "Back to Effects"));
			List<(string Name, int FirstPage)> categories = CollectCategories(self);
			int perPage = Math.Max(2, self.maxObjectsPerPage);
			state.TotalCategoryPages = Math.Max(1, (categories.Count + perPage - 1) / perPage);
			if (state.CategoryPage >= state.TotalCategoryPages) state.CategoryPage = 0;
			panel.Title = state.TotalCategoryPages > 1 ? $"CATEGORIES ({state.CategoryPage + 1}/{state.TotalCategoryPages})" : "CATEGORIES";

			int start = state.CategoryPage * perPage;
			float rowsPerColumn = perPage / 2f;
			for (int i = 0; i < perPage && start + i < categories.Count; i++)
			{
				int col = Mathf.FloorToInt(i / rowsPerColumn);
				float x = 5f + (col * 195f);
				float y = panel.size.y - 16f - 35f - (20f * i) + (20f * rowsPerColumn * col);
				string categoryName = categories[start + i].Name;
				var btn = new Button(self.owner, "Category:" + categoryName, panel, new Vector2(x, y), 190f, categoryName);
				panel.subNodes.Add(btn);
				btn.Refresh();
			}
		}

		private static void RoomSettingsPage_ctor(On.DevInterface.RoomSettingsPage.orig_ctor orig, RoomSettingsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
		{
			orig(self, owner, IDstring, parentNode, name);
			self.subNodes.Add(new CategoryBrowserUpdater(self)); // evil solution to get around the fact that RoomSettingsPage has no Update method to override
		}

		private sealed class CategoryBrowserUpdater(RoomSettingsPage page) : DevUINode(page.owner, "__RK_CategoryBrowserUpdater", page)
		{
			public RoomSettingsPage page = page;

			public override void Update()
			{
				BrowserState state = _effectStates.GetValue(page, _ => new());
				if (state.NeedsRefresh)
				{
					state.NeedsRefresh = false;
					page.RefreshEffectsPage();
				}
			}
		}

		private static void RoomSettingsPage_Signal(On.DevInterface.RoomSettingsPage.orig_Signal orig, RoomSettingsPage self, DevUISignalType type, DevUINode sender, string message)
		{
			if (type == DevUISignalType.ButtonClick && sender != null)
			{
				BrowserState state = _effectStates.GetValue(self, _ => new());
				string id = sender?.IDstring ?? string.Empty;

				if (id == "Categories_Button")
				{
					state.InCategoryView = !state.InCategoryView;
					state.CategoryPage = 0;
					state.NeedsRefresh = true;
					return;
				}

				if (state.InCategoryView)
				{
					if (id == "Prev_Button")
					{
						if (state.TotalCategoryPages > 0)
						{
							state.CategoryPage = (state.CategoryPage - 1 + state.TotalCategoryPages) % state.TotalCategoryPages;
						}
						state.NeedsRefresh = true;
						return;
					}

					if (id == "Next_Button")
					{
						if (state.TotalCategoryPages > 0)
						{
							state.CategoryPage = (state.CategoryPage + 1) % state.TotalCategoryPages;
						}
						state.NeedsRefresh = true;
						return;
					}

					if (id.StartsWith("Category:", StringComparison.Ordinal))
					{
						foreach ((string Name, int FirstPage) in CollectCategories(self))
						{
							if (id[9..].Equals(Name, StringComparison.OrdinalIgnoreCase))
							{
								self.currObjectsPage = FirstPage;
								break;
							}
						}

						state.InCategoryView = false;
						state.CategoryPage = 0;
						state.NeedsRefresh = true;
						return;
					}
				}
			}
			orig(self, type, sender, message);
		}

		private static List<(string Name, int FirstPage)> CollectCategories(RoomSettingsPage self)
		{
			var list = new List<(string Name, int FirstPage)>();
			RoomSettings.RoomEffect.Type[] types = self.effectTypes;
			if (types == null || types.Length == 0) return list;

			int perPage = Math.Max(1, self.maxObjectsPerPage);
			HashSet<string> collectedCategories = [];
			for (int i = 0; i < types.Length; i++)
			{
				RoomSettings.RoomEffect.Type type = types[i];
				if (type == null || type == RoomSettings.RoomEffect.Type.None) continue;

				RoomSettingsPage.DevEffectsCategories Getcategory = self.DevEffectGetCategoryFromEffectType(type);
				string? categoryName = Getcategory?.value;
				if (string.IsNullOrEmpty(categoryName) || !collectedCategories.Add(categoryName!)) continue;
				list.Add((categoryName!, i / perPage));
			}
			return list;
		}

		#endregion
	}
}

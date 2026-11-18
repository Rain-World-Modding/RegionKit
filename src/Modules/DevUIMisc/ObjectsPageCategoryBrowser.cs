using System.Runtime.CompilerServices;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc
{
	/// <summary>
	/// Category browser for the objects page. Original by MyztiLeaf, integrated with permission by Alduris
	/// </summary>
	public static class ObjectsPageCategoryBrowser
	{
		private static readonly ConditionalWeakTable<ObjectsPage, BrowserState> _states = new();

		private sealed class BrowserState
		{
			public bool InCategoryView;
			public int CategoryPage;
			public int TotalCategoryPages;
			public bool NeedsRefresh;
		}

		internal static void Apply()
		{
			On.DevInterface.ObjectsPage.RefreshObjectsPage += ObjectsPage_RefreshObjectsPage;
			On.DevInterface.ObjectsPage.Signal += ObjectsPage_Signal;
			On.DevInterface.ObjectsPage.Update += ObjectsPage_Update;
		}

		internal static void Undo()
		{
			On.DevInterface.ObjectsPage.RefreshObjectsPage -= ObjectsPage_RefreshObjectsPage;
			On.DevInterface.ObjectsPage.Signal -= ObjectsPage_Signal;
			On.DevInterface.ObjectsPage.Update -= ObjectsPage_Update;
		}

		private static void ObjectsPage_RefreshObjectsPage(On.DevInterface.ObjectsPage.orig_RefreshObjectsPage orig, ObjectsPage self)
		{
			BrowserState state = _states.GetOrCreateValue(self);
			if (!state.InCategoryView)
			{
				orig(self);
				self.objectsPanel.subNodes.Add(new Button(self.owner, "Category:", self.objectsPanel, new Vector2(205f, self.objectsPanel.size.y - 16f - 5f), 190f, "Categories"));
				return;
			}

			Panel panel = self.objectsPanel;
			if (panel == null) return;
			for (int i = panel.subNodes.Count - 1; i >= 2; i--)
			{
				panel.subNodes[i].ClearSprites();
				panel.subNodes.RemoveAt(i);
			}

			self.objectsPanel.subNodes.Add(new Button(self.owner, "Category:", self.objectsPanel, new Vector2(205f, self.objectsPanel.size.y - 16f - 5f), 190f, "Back to Objects"));
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
			BrowserState state = _states.GetOrCreateValue(self);
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
				BrowserState state = _states.GetOrCreateValue(self);
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
							if (string.Equals(Name, "Category:", StringComparison.OrdinalIgnoreCase))
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
			for (int i = 0; i < types.Length; i++)
			{
				PlacedObject.Type type = types[i];
				if (type == null || type == PlacedObject.Type.None) continue;

				ObjectsPage.DevObjectCategories Getcategory = self.DevObjectGetCategoryFromPlacedType(type);
				string? categoryName = Getcategory?.value;
				if (string.IsNullOrEmpty(categoryName) || !new HashSet<string>(StringComparer.OrdinalIgnoreCase).Add(categoryName!)) continue;
				list.Add((categoryName!, i / perPage));
			}
			return list;
		}
	}
}

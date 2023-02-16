using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DevInterface;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.DevUIMisc
{
	internal static class BackgroundBuilder
	{
		public static void Apply()
		{
			On.DevInterface.DevUI.ctor += DevUI_ctor;
			On.DevInterface.DevUI.SwitchPage += DevUI_SwitchPage;
			On.DevInterface.Page.ctor += Page_ctor;
		}

		private static void Page_ctor(On.DevInterface.Page.orig_ctor orig, Page self, DevUI owner, string IDstring, DevUINode parentNode, string name)
		{
			if (owner != null)
			{
				foreach (string str in owner.pages)
				{ Debug.Log(str); }
			}
			orig(self, owner is null ? null : owner, IDstring, parentNode, name);
		}

		public static void Undo()
		{
			On.DevInterface.DevUI.ctor -= DevUI_ctor;
			On.DevInterface.DevUI.SwitchPage -= DevUI_SwitchPage;
		}

		private static void DevUI_SwitchPage(On.DevInterface.DevUI.orig_SwitchPage orig, DevUI self, int newPage)
		{
			if (newPage == self.pages.IndexOf("Background"))
			{
				self.ClearSprites();
				self.activePage = new BackgroundPage(self, "Background_Page", null, "Background Settings");
			}

			else { orig(self, newPage); }
		}

		private static void DevUI_ctor(On.DevInterface.DevUI.orig_ctor orig, DevUI self, RainWorldGame game)
		{
			self.game = game;
			self.placedObjectsContainer = new FContainer();
			if (game != null)
			{
				Futile.stage.AddChild(self.placedObjectsContainer);
			}
			self.pages = new string[]
			{
				"Room Settings",
				"Objects",
				"Sound",
				"Map",
				"Triggers",
				"Dialog",
				"Background"
			};

			self.SwitchPage(game.setupValues.defaultSettingsScreen);

		}

		public class BackgroundPage : Page
		{
			// Token: 0x060028F3 RID: 10483 RVA: 0x0031DEDC File Offset: 0x0031C0DC
			public BackgroundPage(DevUI owner, string IDstring, DevUINode parentNode, string name) : base(owner, IDstring, parentNode, name)
			{
				subNodes.Add(new BackgroundTemplateCycler(owner, "Game_Over_Type_Cycler", this, new Vector2(170f, 660f), 120f));
			}

		}

		public class BackgroundTemplateCycler : Button
		{
			// Token: 0x060028CD RID: 10445 RVA: 0x0031B114 File Offset: 0x00319314
			public BackgroundTemplateCycler(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width) : base(owner, IDstring, parentNode, pos, width, "")
			{
				subNodes.Add(new DevUILabel(owner, "Title", this, new Vector2(-50f, 0f), 40f, "G.O: "));
			}

			// Token: 0x060028CE RID: 10446 RVA: 0x0031B164 File Offset: 0x00319364

			public BackgroundTemplateType templateType;
			public override void Refresh()
			{
				base.Refresh();
				string text;
				if (RoomSettings.dType != null)
				{
					text = "";
				}
				else if (!RoomSettings.parent.isAncestor && RoomSettings.parent.dType != null)
				{
					text = "<T>";
				}
				else
				{
					text = "<A>";
				}
				string str = text;
				string str2 = " ";
				Text = str + str2 + (templateType != null ? templateType.ToString() : null);
			}

			// Token: 0x060028CF RID: 10447 RVA: 0x0031B1F8 File Offset: 0x003193F8
			public override void Clicked()
			{
				if (templateType == null)
				{
					templateType = new BackgroundTemplateType(ExtEnum<BackgroundTemplateType>.values.GetEntry(0), false);
				}

				else
				{
					templateType = new BackgroundTemplateType(ExtEnum<BackgroundTemplateType>.values.GetEntry(templateType.Index + 1), false);
				}
				Refresh();
			}
		}

		public class BackgroundTemplateType : ExtEnum<RoomRain.DangerType>
		{
			public BackgroundTemplateType(string value, bool register = false) : base(value, register)
			{
			}

			public static readonly BackgroundTemplateType AboveCloudsView = new BackgroundTemplateType("AboveCloudsView", true);

			public static readonly BackgroundTemplateType RoofTopView = new BackgroundTemplateType("RoofTopView", true);

			public static readonly BackgroundTemplateType VoidSeaScene = new BackgroundTemplateType("VoidSeaScene", true);
		}
	}
}

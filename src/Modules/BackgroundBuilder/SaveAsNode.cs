using System.IO;
using DevInterface;
using RegionKit.Modules.DevUIMisc;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.BackgroundBuilder;

public class SaveAsNode : DevUINode, IDevUISignals
{
	public string fileName;
	public string modName;
	public bool inherit;


	Panel? activePanel;
	public SaveAsNode(DevUI owner, string IDstring, DevUINode parentNode, string fileName, string modName, string? currentFile) : base(owner, IDstring, parentNode)
	{
		this.fileName = fileName;
		this.modName = modName;
		if(currentFile != null)
			ChangeActivePanel(new YesOrNoPrompt(owner, "inherit", this, new Vector2(100f, 100f), $"inherit from parent \"{currentFile}\"?" , 250f));
		else
			ChangeActivePanel(new ModSelect(owner, this, new Vector2(100f, 100f), "modselect"));
	}

	public void ChangeActivePanel(Panel newPanel)
	{
		if (activePanel != null)
		{
			activePanel.ClearSprites();
			subNodes.Remove(activePanel);
		}
		subNodes.Add(activePanel = newPanel);
		
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (activePanel == null)
			return;


		if (activePanel.IDstring == "inherit")
		{
			inherit = sender.IDstring == "yes";
			ChangeActivePanel(new ModSelect(owner, this, new Vector2(100f, 100f), "modselect"));
		}

		else if (activePanel is ModSelect modSelect)
		{
			string subbuttonid = sender.IDstring.Remove(0, (modSelect.idstring + "Button99289_").Length);
			modName = subbuttonid;
			ChangeActivePanel(new NameInput(owner, "nameInput", this, new Vector2(100f, 100f), fileName));
		}

		else if (sender.IDstring == "cancel")
		{
			this.ClearSprites();
			this.parentNode.subNodes.Remove(this);
		}

		else if (sender.IDstring == "ok" && activePanel is NameInput nameInput && nameInput.nameInput.isTextValid(nameInput.nameInput.actualValue))
		{
			fileName = nameInput.nameInput.actualValue;
			if (File.Exists(BackgroundPage.SavePath(modName, fileName)))
			{
				bool protectedFile = false;
				foreach (string line in File.ReadAllLines(BackgroundPage.SavePath(modName, fileName)))
				{
					if (line == "PROTECTED")
					{
						protectedFile = true;
						break;
					}
				}
				if(protectedFile)
					ChangeActivePanel(new OKPrompt(owner, "protected", this, new Vector2(100f, 100f), $"\"{fileName}\" is protected and cannot be overwritten", 400f));
				else
					ChangeActivePanel(new YesOrNoPrompt(owner, "overwrite", this, new Vector2(100f, 100f), $"\"{fileName}\" already exists. overwrite?", 250f));
			}
			else 
			{
				Done();
			}
		}

		else if (activePanel.IDstring == "protected")
		{
			ChangeActivePanel(new NameInput(owner, "nameInput", this, new Vector2(100f, 100f), fileName));
		}

		else if (activePanel.IDstring == "overwrite")
		{
			if (sender.IDstring == "yes")
				Done();
			if (sender.IDstring == "no")
			{
				ChangeActivePanel(new NameInput(owner, "nameInput", this, new Vector2(100f, 100f), fileName));
			}
		}
	}

	public void Done()
	{
		this.SendSignal(DevUISignalType.ButtonClick, this, "done!");
		this.ClearSprites();
		this.parentNode.subNodes.Remove(this);
	}

	public class NameInput : Panel
	{
		public StringControl nameInput;
		public static readonly Vector2 defaultSize = new Vector2(200f, 50f);
		public NameInput(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string defaultName) : base(owner, IDstring, parentNode, pos, defaultSize, "file name")
		{
			Vector2 ppos = new Vector2(5f, size.y - 20f);
			subNodes.Add(new DevUILabel(owner, "namelabel", this, ppos, 60f, "filename: "));
			ppos.x += 65f;
			subNodes.Add(nameInput = new StringControl(owner, "name", this, ppos, 120f, defaultName, StringControl.TextIsValidFilename));
			ppos.y -= 20f;
			ppos.x = 5f;
			subNodes.Add(new Button(owner, "ok", this, ppos, 90f, "ok"));
			ppos.x += 100f;
			subNodes.Add(new Button(owner, "cancel", this, ppos, 90f, "cancel"));
		}
	}

	public class YesOrNoPrompt : Panel
	{
		public YesOrNoPrompt(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string question, float width) : base(owner, IDstring, parentNode, pos, new Vector2(width, 50f), "")
		{
			Vector2 ppos = new Vector2(5f, size.y - 20f);
			subNodes.Add(new DevUILabel(owner, "namelabel", this, ppos, width - 10f, question));
			ppos.y -= 20f;
			subNodes.Add(new Button(owner, "yes", this, ppos, 90f, "yes"));
			ppos.x += 100f;
			subNodes.Add(new Button(owner, "no", this, ppos, 90f, "no"));
		}
	}
	public class OKPrompt : Panel
	{
		public OKPrompt(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string statement, float width) : base(owner, IDstring, parentNode, pos, new Vector2(width, 50f), "")
		{
			Vector2 ppos = new Vector2(5f, size.y - 20f);
			subNodes.Add(new DevUILabel(owner, "namelabel", this, ppos, width - 10f, statement));
			ppos.y -= 20f;
			subNodes.Add(new Button(owner, "ok", this, ppos, 90f, "ok"));
		}
	}

	public class ModSelect : ItemSelectPanel
	{
		public ModSelect(DevUI owner, DevUINode parentNode, Vector2 pos, string idstring)
			: base(owner, parentNode, pos, [.. AllowedMods().Keys], idstring, "select mod", new Vector2(160f, 200f), 150f, 1)
		{
		}

		public static Dictionary<string, string> AllowedMods()
		{

			var modNames = new Dictionary<string, string>();

			foreach (ModManager.Mod mod in ModManager.ActiveMods)
			{
				if(!mod.workshopMod && mod.id != "watcher" && mod.id != "moreslugcats" && mod.id != "devtools" && mod.id != "expedition" && mod.id != "rwremix" && mod.id != "jollycoop")
					modNames.Add(mod.name, mod.path);
			}

			return modNames;
		}
	}
}

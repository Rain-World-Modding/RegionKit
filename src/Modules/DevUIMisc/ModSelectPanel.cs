using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc
{
	public class ModSelectPanel : Panel
	{
		// Token: 0x0600401D RID: 16413 RVA: 0x00482E58 File Offset: 0x00481058
		public ModSelectPanel(DevUI owner, DevUINode parentNode, Vector2 pos, string[] list) : base(owner, "Select_Mod_Panel", parentNode, pos, new Vector2(305f, 420f), "Select mod directory")
		{
			SlugcatStats.Name? slugname = owner.game.GetStorySession.saveStateNumber;
			items = list;

			this.currentOffset = 0;
			this.perpage = 36;
			this.PopulateMods(this.currentOffset);
		}

		// Token: 0x0600401E RID: 16414 RVA: 0x00482EAC File Offset: 0x004810AC
		public void PopulateMods(int offset)
		{
			currentOffset = offset;
			foreach (DevUINode devUINode in subNodes)
			{
				devUINode.ClearSprites();
			}
			subNodes.Clear();
			IntVector2 intVector = new IntVector2(0, 0);
			int num = currentOffset;
			while (num < items.Length && num < currentOffset + perpage)
			{
				subNodes.Add(new Button(owner, "ModPanelButton99289_" + items[num], this, new Vector2(5f + (float)intVector.x * 150f, size.y - 25f - 20f * (float)intVector.y), 145f, items[num]));
				intVector.y++;
				if (intVector.y >= (int)Mathf.Floor((float)this.perpage / 2f))
				{
					intVector.x++;
					intVector.y = 0;
				}
				num++;
			}
			subNodes.Add(new Button(owner, "BackPage99289..?/~", this, new Vector2(5f, size.y - 25f - 20f * ((float)(perpage / 2) + 1f)), 145f, "Previous"));
			subNodes.Add(new Button(owner, "NextPage99289..?/~", this, new Vector2(155f, size.y - 25f - 20f * ((float)(perpage / 2) + 1f)), 145f, "Next"));
		}


		// Token: 0x0600401F RID: 16415 RVA: 0x00483094 File Offset: 0x00481294
		public void PrevPage()
		{
			currentOffset -= perpage;
			if (currentOffset < 0)
			{
				currentOffset = 0;
			}
			PopulateMods(currentOffset);
		}

		// Token: 0x06004020 RID: 16416 RVA: 0x004830C8 File Offset: 0x004812C8
		public void NextPage()
		{
			currentOffset += perpage;
			if (currentOffset > items.Length)
			{
				currentOffset = perpage * (int)Mathf.Floor((float)items.Length / (float)perpage);
			}
			PopulateMods(currentOffset);
		}

		// Token: 0x04004387 RID: 17287
		private int perpage;

		// Token: 0x04004388 RID: 17288
		private int currentOffset;

		private string[] items;
	}
}


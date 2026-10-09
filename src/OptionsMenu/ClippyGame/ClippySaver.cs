using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu.Remix.MixedUI;
using System.IO;

namespace RegionKit.OptionsMenu.ClippyGame
{
	public class ClippySaver
	{
		public ClippyTab parent;

		public static string path = Application.persistentDataPath + "/Clippy.txt";

		public ClippySaver(ClippyTab parent)
		{
			this.parent = parent;
		}

		public void Load()
		{
			if (File.Exists(path))
			{
				string[] datas = File.ReadAllText(path, Encoding.UTF8).Split('\n');

				bool m = bool.TryParse(datas[0], out m) ? m : false;
				bool sf = bool.TryParse(datas[1], out sf) ? sf : false;
				int h = int.TryParse(datas[2], out h) ? h : 0;
				int s = int.TryParse(datas[3], out s) ? s : 0;

				parent.MusicEnabled = m;
				parent.SoundEffectsEnabled = sf;
				parent.highScore = h;
				parent.score = s;
			}
		}

		public void Save()
		{
			StringBuilder data = new StringBuilder();
			data.AppendLine(parent.MusicEnabled.ToString());
			data.AppendLine(parent.SoundEffectsEnabled.ToString());
			data.AppendLine(parent.highScore.ToString());
			data.AppendLine(parent.score.ToString());

			File.WriteAllText(path, data.ToString());
		}
	}
}

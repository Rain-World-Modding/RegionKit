using System.IO;
using System.Text.RegularExpressions;
using DevInterface;

namespace RegionKit.Modules.DevUIMisc
{
	/// <summary>
	/// File picker panel. Handles pages automatically, sends a signal when finished
	/// </summary>
	public class FilePicker : Panel
	{
		public static readonly Regex PNGRegex = new(@".*\.png", RegexOptions.IgnoreCase);
		public static readonly Regex TXTRegex = new(@".*\.txt", RegexOptions.IgnoreCase);
		public static readonly DevUISignalType FilePickedSignal = new(nameof(FilePickedSignal), true);

		private static readonly float _buttonWidth = 195f;
		private static readonly int _rows = 18;
		private static readonly int _columns = 2;

		private int ItemsPerPage => _rows * _columns;

		private string _currentDir;
		private bool _restrictDir;
		private string[] _folders = [];
		private string[] _files = [];
		private int _currPage = 0;
		private int _pages = 0;
		private readonly Regex? _fileRegex;

		public FilePicker(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string? startPath, Regex? fileRegex, bool restrictDir = false) 
			: base(owner, IDstring, parentNode, pos, new Vector2(_buttonWidth * _columns + 5f * (_columns + 1), 30f + 20f * _rows), "Select file or folder")
		{
			_fileRegex = fileRegex;
			_restrictDir = restrictDir;

			// Try to resolve path
			string? tempDir = startPath;
			try
			{
				while (tempDir != null && !Directory.Exists(AssetManager.ResolveDirectory(tempDir)) && tempDir.Length > 1)
				{
					tempDir = Path.GetDirectoryName(tempDir);
				}
				tempDir ??= "";
			}
			catch // may encounter an IOException if path is invalid, just ignore it
			{
				tempDir = "";
			}
			_currentDir = tempDir;

			// Populate
			FindFilesAndFolders();
			Populate();
		}

		private void FindFilesAndFolders()
		{
			// Find folders and assign (no special handling needed)
			if (!_restrictDir)
			{
				_folders = [.. AssetManager.ListDirectory(_currentDir, true, false, false).Select(x => Path.GetFileName(x)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)];
			}
			else
			{
				_folders = [];
			}

			// Find files
			string[] foundFiles = [.. AssetManager.ListDirectory(_currentDir, false, false, false).Select(x => Path.GetFileName(x)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase)];

			// Filter and assign
			if (_fileRegex != null)
			{
				_files = [.. foundFiles.Where(x => _fileRegex.IsMatch(x))];
			}
			else
			{
				_files = foundFiles;
			}

			_pages = (_folders.Length + _files.Length + ItemsPerPage - 1) / ItemsPerPage; // ceiling int division
			_currPage = 0;

			fLabels[0].text = $"Select file or folder (current: {_currentDir})";
		}

		private void Populate()
		{
			// Clear out previous items
			foreach (DevUINode node in subNodes)
			{
				node.ClearSprites();
			}
			subNodes.Clear();

			// Top buttons
			float headerButtonWidth = (size.x - 20f) / 3;
			if (_currentDir.Length > 0 && !_restrictDir)
			{
				subNodes.Add(new ParentButton(owner, "FilePicker_Parent", this, new Vector2(5f, size.y - 20f), headerButtonWidth, "Parent folder"));
			}
			subNodes.Add(new PageButton(owner, "FilePicker_Prev", this, new Vector2(5f + (headerButtonWidth + 5f) * 1, size.y - 20f), headerButtonWidth, "Previous", -1));
			subNodes.Add(new PageButton(owner, "FilePicker_Next", this, new Vector2(5f + (headerButtonWidth + 5f) * 2, size.y - 20f), headerButtonWidth, "Next", 1));

			// Option buttons
			for (int i = 0; i < _columns; i++)
			{
				for (int j = 0; j < _rows; j++)
				{
					Vector2 buttonPos = new Vector2(5f + (_buttonWidth + 5f) * i, 5f + 20f * (_rows - 1 - j));
					int k = (i * _rows) + j + ItemsPerPage * _currPage;
					if (k < _folders.Length)
					{
						subNodes.Add(new FileButton(owner, "FilePicker_Folder_" + _folders[k], this, buttonPos, _buttonWidth, _folders[k], true));
					}
					else
					{
						k -= _folders.Length;
						if (k < _files.Length)
						{
							subNodes.Add(new FileButton(owner, "FilePicker_File_" + _files[k], this, buttonPos, _buttonWidth, _files[k], false));
						}
					}
				}
			}
		}

		private void ChangePage(int direction)
		{
			_currPage = Custom.IntClamp(_currPage + Math.Sign(direction), 0, _pages - 1);
			Populate();
		}

		private void Pick(bool folder, string name)
		{
			if (folder)
			{
				if (name == "..")
				{
					_currentDir = Path.GetDirectoryName(_currentDir) ?? "";
				}
				else
				{
					_currentDir = Path.Combine(_currentDir, name);
				}

				_currPage = 0;
				FindFilesAndFolders();
				Populate();
			}
			else if (name.Length > 0)
			{
				string combinedPath = Path.Combine(_currentDir, name);
				if (File.Exists(AssetManager.ResolveFilePath(combinedPath)))
				{
					DevUINode node = this;
					while (node != null)
					{
						node = node.parentNode;
						(node as IDevUISignals)?.Signal(FilePickedSignal, this, combinedPath);
					}
				}
			}

		}

		private class PageButton : Button
		{
			private readonly FilePicker _filePicker;
			private readonly int _signal;

			public PageButton(DevUI owner, string IDstring, FilePicker parentNode, Vector2 pos, float width, string text, int signal) : base(owner, IDstring, parentNode, pos, width, text)
			{
				_filePicker = parentNode;
				_signal = signal;
			}

			public override void Clicked()
			{
				_filePicker.ChangePage(_signal);
			}
		}

		private class ParentButton : Button
		{
			private readonly FilePicker _filePicker;

			public ParentButton(DevUI owner, string IDstring, FilePicker parentNode, Vector2 pos, float width, string text) : base(owner, IDstring, parentNode, pos, width, text)
			{
				_filePicker = parentNode;
			}

			public override void Clicked()
			{
				_filePicker.Pick(true, "..");
			}
		}

		private class FileButton : Button
		{
			private readonly FilePicker _filePicker;
			private readonly bool _folder;
			private readonly string _name;
			private readonly FSprite? _folderSprite;

			public FileButton(DevUI owner, string IDstring, FilePicker parentNode, Vector2 pos, float width, string text, bool folder) : base(owner, IDstring, parentNode, pos, width, text)
			{
				_filePicker = parentNode;
				_folder = folder;
				_name = text;

				if (folder)
				{
					_folderSprite = new FSprite("assets/regionkit/sprites/rk_folder_symbol")
					{
						anchorX = 0f,
						anchorY = 0f,
					};
					fSprites.Add(_folderSprite);
					Futile.stage.AddChild(_folderSprite);
				}
			}

			public override void Refresh()
			{
				base.Refresh();
				if (_folderSprite != null)
				{
					MoveLabel(0, absPos + Vector2.right * 20f);
					_folderSprite.SetPosition(absPos + new Vector2(2.01f, 1.01f));
					_folderSprite.color = fLabels[0].color;
				}
			}

			public override void Clicked()
			{
				_filePicker.Pick(_folder, _name);
			}
		}
	}
}

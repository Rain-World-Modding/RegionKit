using System.Runtime.CompilerServices;
using DevInterface;
using Watcher;

namespace RegionKit.Modules.FloatingDebrisNew
{
	/// <summary>
	/// Specifies that there is extra floater data involved with this floater type.
	/// </summary>
	public interface IFloaterExtraData
	{
		/// <summary>Returns the save data to attach to a single floater point</summary>
		/// <remarks>
		/// If storing multiple pieces of data, do not use <c>&gt;&lt;</c>, <c>~</c>, <c>||</c> as a separator
		/// </remarks>
		/// <returns></returns>
		public string SaveFloaterData();

		/// <summary>
		/// Loads the save data for a single floater point.
		/// </summary>
		/// <remarks>
		/// Default data is empty string. Null is not allowed.
		/// </remarks>
		/// <param name="data">Data to load</param>
		public void LoadFloaterData(string data);

		/// <summary>
		/// Creates a custom DevUI representation.
		/// The DevUI implementation must be self-sufficient to update the data properly.
		/// </summary>
		/// <remarks>
		/// When new handles are added or removed, this will be called again to recreate the dev UI.
		/// This is to ensure that if points get added or removed or if the type gets changed, the dev UI gets swapped properly as well.
		/// </remarks>
		/// <param name="owner">Dev tools interface owner</param>
		/// <param name="parentNode">The handle to attach to</param>
		public void CreateDevUI(DevUI owner, DevUINode parentNode);


		internal static class Implementation
		{
			private static readonly ConditionalWeakTable<FloatingDebrisData, FloaterDataHolder> _dataCWT = new();
			private static readonly ConditionalWeakTable<Handle, FloaterDevUIContainer> _devNodeCWT = new();
			private const string IDENTIFIER = "RKFloaterData";
			private const string SEPARATOR = "$$";
			private const string SUBSEPARATOR = "||";

			internal static void Enable()
			{
				On.Watcher.FloatingDebrisData.FromString += FloatingDebrisData_FromString;
				On.Watcher.FloatingDebrisData.ToString += FloatingDebrisData_ToString;
				On.Watcher.FloatingDebris.RefreshFloaters += FloatingDebris_RefreshFloaters;
				On.Watcher.FloatingDebrisData.AddControlPointLeft += FloatingDebrisData_AddControlPointLeft;
				On.Watcher.FloatingDebrisData.AddControlPointRight += FloatingDebrisData_AddControlPointRight;
				On.Watcher.FloatingDebrisRepresentation.RepositionHandles += FloatingDebrisRepresentation_RepositionHandles;
				On.Watcher.FloatingDebrisRepresentation.FloatingDebrisPanel.Signal += FloatingDebrisPanel_Signal;
			}

			internal static void Disable()
			{
				On.Watcher.FloatingDebrisData.FromString -= FloatingDebrisData_FromString;
				On.Watcher.FloatingDebrisData.ToString -= FloatingDebrisData_ToString;
				On.Watcher.FloatingDebris.RefreshFloaters -= FloatingDebris_RefreshFloaters;
				On.Watcher.FloatingDebrisData.AddControlPointLeft -= FloatingDebrisData_AddControlPointLeft;
				On.Watcher.FloatingDebrisData.AddControlPointRight -= FloatingDebrisData_AddControlPointRight;
				On.Watcher.FloatingDebrisRepresentation.RepositionHandles -= FloatingDebrisRepresentation_RepositionHandles;
				On.Watcher.FloatingDebrisRepresentation.FloatingDebrisPanel.Signal -= FloatingDebrisPanel_Signal;
			}

			internal static T? GetFloaterDataAt<T>(FloatingDebrisData data, int index) where T : IFloaterExtraData
			{
				index = Mathf.Clamp(index, 0, data.controlPointPosX.Count - 1);
				if (_dataCWT.TryGetValue(data, out FloaterDataHolder holder))
				{
					IFloaterExtraData value = holder.dataHolders[index];
					if (value is T valueCasted)
					{
						return valueCasted;
					}
				}
				return default;
			}

			private static void InitData(FloatingDebrisData fdData)
			{
				if (FloatingDebris.types[fdData.type] is ICreateFloaterExtraData dataFactory)
				{
					// Init if necessary
					if (!_dataCWT.TryGetValue(fdData, out FloaterDataHolder? floaterData))
					{
						floaterData = new FloaterDataHolder();
						_dataCWT.Add(fdData, floaterData);
					}

					// Init save values if necessary
					int controlPointsCount = fdData.controlPointPosX.Count;
					if (!floaterData.saveValues.ContainsKey(dataFactory.DataKeyword))
					{
						List<string> blankSaveData = [];
						for (int i = 0; i < controlPointsCount; i++)
						{
							blankSaveData.Add(string.Empty);
						}
						floaterData.saveValues[dataFactory.DataKeyword] = blankSaveData;
					}

					// Init actual holders
					floaterData.dataHolders.Clear();
					for (int i = 0; i < controlPointsCount; i++)
					{
						floaterData.dataHolders.Add(dataFactory.CreateData());
					}
				}
			}

			private static void StoreData(FloatingDebrisData fdData)
			{
				string? saveKey = (FloatingDebris.types[fdData.type] as ICreateFloaterExtraData)?.DataKeyword;
				if (saveKey != null && _dataCWT.TryGetValue(fdData, out FloaterDataHolder floaterData))
				{
					if (!floaterData.saveValues.ContainsKey(saveKey))
					{
						floaterData.saveValues.Add(saveKey, []);
					}
					floaterData.saveValues[saveKey].Clear();
					int numControlPoints = fdData.controlPointPosX.Count;
					for (int i = 0; i < numControlPoints; i++)
					{
						floaterData.saveValues[saveKey].Add(floaterData.dataHolders[i].SaveFloaterData() ?? string.Empty);
					}
				}
			}

			private static void LoadData(FloatingDebrisData fdData)
			{
				// Load data
				string? saveKey = (FloatingDebris.types[fdData.type] as ICreateFloaterExtraData)?.DataKeyword;
				if (saveKey != null && _dataCWT.TryGetValue(fdData, out FloaterDataHolder floaterData) && floaterData.saveValues.ContainsKey(saveKey))
				{
					List<string> saveData = floaterData.saveValues[saveKey];
					for (int i = 0; i < saveData.Count && i < floaterData.dataHolders.Count; i++)
					{
						floaterData.dataHolders[i].LoadFloaterData(saveData[i]);
					}
				}
			}

			private static void UpdateDevUI(FloatingDebrisRepresentation rep)
			{
				if (_dataCWT.TryGetValue(rep.data, out FloaterDataHolder floaterData))
				{
					bool useExtraData = FloatingDebris.types[rep.data.type] is ICreateFloaterExtraData;
					for (int i = 0; i < rep.controlPoints.Count; i++)
					{
						_devNodeCWT.TryGetValue(rep.controlPoints[i], out FloaterDevUIContainer? container);
						if (container != null)
						{
							container.ClearSprites();
							container.subNodes.Clear();
						}
						else
						{
							container = new FloaterDevUIContainer(rep.owner, $"FloaterContainer{i}", rep.controlPoints[i], Vector2.zero);
							rep.controlPoints[i].subNodes.Add(container);
							_devNodeCWT.Add(rep.controlPoints[i], container);
						}

						if (useExtraData)
						{
							floaterData.dataHolders[i].CreateDevUI(rep.owner, container);
						}
						container.Refresh();
					}
				}
			}

			private static void FloatingDebrisData_FromString(On.Watcher.FloatingDebrisData.orig_FromString orig, FloatingDebrisData self, string s)
			{
				orig(self, s);

				// FloatingDebrisData doesn't ever load unrecognizedAttributes. How could it do such a thing :(
				string[] saveParts = s.Split('~');
				for (int i = 17; i < saveParts.Length; i++)
				{
					string[] dataParts = saveParts[i].Split([SEPARATOR], 3, StringSplitOptions.None);
					Dictionary<string, List<string>>? dataDict = null;
					if (dataParts.Length >= 3 && dataParts[0] == IDENTIFIER)
					{
						dataDict ??= [];
						string key = dataParts[1];
						List<string> values = [.. dataParts[2].Split([SUBSEPARATOR], StringSplitOptions.None)];
						dataDict[key] = values;
					}
					if (dataDict != null)
					{
						FloaterDataHolder holder = new FloaterDataHolder()
						{
							saveValues = dataDict
						};
						_dataCWT.Add(self, holder);
						InitData(self);
						LoadData(self);
					}
				}
			}

			private static string FloatingDebrisData_ToString(On.Watcher.FloatingDebrisData.orig_ToString orig, FloatingDebrisData self)
			{
				// Get data to save
				if (self.obj != null)
				{
					StoreData(self);
				}

				// Now actually save it
				string s = orig(self);
				if (_dataCWT.TryGetValue(self, out FloaterDataHolder data))
				{
					foreach ((string key, List<string> subdata) in data.saveValues)
					{
						s += $"~{IDENTIFIER}{SEPARATOR}{key}{SEPARATOR}{string.Join(SUBSEPARATOR, subdata)}";
					}
				}
				return s;
			}

			private static void FloatingDebris_RefreshFloaters(On.Watcher.FloatingDebris.orig_RefreshFloaters orig, FloatingDebris self)
			{
				orig(self);
				LoadData(self.data);
			}

			private static void FloatingDebrisData_AddControlPointLeft(On.Watcher.FloatingDebrisData.orig_AddControlPointLeft orig, FloatingDebrisData self)
			{
				orig(self);
				if (_dataCWT.TryGetValue(self, out FloaterDataHolder data))
				{
					foreach (List<string> list in data.saveValues.Values)
					{
						list.Insert(0, string.Empty);
					}

					if (FloatingDebris.types[self.type] is ICreateFloaterExtraData dataFactory)
					{
						data.dataHolders.Insert(0, dataFactory.CreateData());

						/*StoreData(self);
						if (data.dataHolders.Count > 1 && dataFactory.CopyDataToNewPoints)
						{
							data.dataHolders[0].LoadFloaterData(data.dataHolders[1].SaveFloaterData());
						}*/
					}
				}
			}

			private static void FloatingDebrisData_AddControlPointRight(On.Watcher.FloatingDebrisData.orig_AddControlPointRight orig, FloatingDebrisData self)
			{
				orig(self);
				if (_dataCWT.TryGetValue(self, out FloaterDataHolder data))
				{
					foreach (List<string> list in data.saveValues.Values)
					{
						list.Add(string.Empty);
					}

					if (FloatingDebris.types[self.type] is ICreateFloaterExtraData dataFactory)
					{
						data.dataHolders.Add(dataFactory.CreateData());

						/*StoreData(self);
						if (data.dataHolders.Count > 1 && dataFactory.CopyDataToNewPoints)
						{
							data.dataHolders[^1].LoadFloaterData(data.dataHolders[^2].SaveFloaterData());
						}*/
					}
				}
			}

			private static void FloatingDebrisRepresentation_RepositionHandles(On.Watcher.FloatingDebrisRepresentation.orig_RepositionHandles orig, FloatingDebrisRepresentation self)
			{
				orig(self);
				UpdateDevUI(self);
			}

			private static void FloatingDebrisPanel_Signal(On.Watcher.FloatingDebrisRepresentation.FloatingDebrisPanel.orig_Signal orig, FloatingDebrisRepresentation.FloatingDebrisPanel self, DevUISignalType type, DevUINode sender, string message)
			{
				CustomDecalRepresentation.SelectDecalPanel? oldFloaterTypeSelectPanel = self.floaterTypeSelectPannel;
				orig(self, type, sender, message);
				if (oldFloaterTypeSelectPanel != null && sender.parentNode == oldFloaterTypeSelectPanel && sender.IDstring != "Type")
				{
					FloatingDebrisRepresentation rep = (self.parentNode as FloatingDebrisRepresentation)!;
					InitData(rep.data);
					UpdateDevUI(rep);
				}
			}

			private class FloaterDataHolder
			{
				public Dictionary<string, List<string>> saveValues = [];
				public List<IFloaterExtraData> dataHolders = [];
			}

			/// <summary>
			/// This class exists solely for easily containing dev nodes for IHaveExtraFloaterData implemenations
			/// </summary>
			private class FloaterDevUIContainer(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : PositionedDevUINode(owner, IDstring, parentNode, pos) { }
		}
	}
}

using DevInterface;

namespace RegionKit.Modules.DevUIMisc.GenericNodes
{
	public class StringControlNoSignal : StringControl
	{
		public StringControlNoSignal(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text, IsTextValid del) : base(owner, IDstring, parentNode, pos, width, text, del)
		{
		}

		protected override void TrySetValue(string newValue, bool endTransaction)
		{
			if (isTextValid(newValue))
			{
				actualValue = newValue;
				fLabels[0].color = new Color(0.1f, 0.4f, 0.2f);
			}
			else
			{
				fLabels[0].color = Color.red;
			}
			if (endTransaction)
			{
				Text = actualValue;
				fLabels[0].color = Color.black;
				Refresh();
			}
		}
	}
}

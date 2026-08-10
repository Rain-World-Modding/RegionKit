namespace RegionKit.Modules.IO
{
	public class Counter : IOObject
	{
		public int index;

		public int threshold => (placedObject.data as CounterType.CounterData)!.GetValue<int>("index");

		public bool triggerd;

		public Counter(PlacedObject pObj) : base(pObj)
		{

		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			if (index >= threshold && !triggerd)
			{
				triggerd = true;
				SendOutput(_Enums.OutputType.Output_Trigger);
			}
			else if (index < threshold)
			{
				triggerd = false;
			}
		}

		public override void ReciveInput(_Enums.InputType type)
		{
			base.ReciveInput(type);

			if (type == _Enums.InputType.Input_AddValue)
			{
				index++;
			}

			if (type == _Enums.InputType.Input_RemoveValue)
			{
				index--;
			}

			if (type == _Enums.InputType.Input_ResetValue)
			{
				index = 0;
			}
		}
	}
}

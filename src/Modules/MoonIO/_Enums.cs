namespace RegionKit.Modules.MoonIO
{
	public static class _Enums
	{
		public static class PlacedObjectType
		{
			public static readonly PlacedObject.Type IOLightSource = new PlacedObject.Type("IO Light Source", true);

			public static PlacedObject.Type Trigger = new PlacedObject.Type("IO Trigger", true);

			public static PlacedObject.Type Counter = new PlacedObject.Type("IO Counter", true);

			public static PlacedObject.Type CycleCooldown = new PlacedObject.Type("IO Cycle Cooldown", true);
		}

		public static class DevObjectCategories
		{
			public static readonly DevInterface.ObjectsPage.DevObjectCategories MoonsStuffIO = new DevInterface.ObjectsPage.DevObjectCategories("RegionKit-I/O", true);
		}

		public class InputType : ExtEnum<InputType>
		{
			public static readonly InputType Input_Trigger = new InputType("Trigger", true);

			public static readonly InputType Input_Toggle = new InputType("Toggle", true);

			public static readonly InputType Input_On = new InputType("On", true);

			public static readonly InputType Input_Off = new InputType("Off", true);

			public static readonly InputType Input_AddValue = new InputType("Add 1", true);

			public static readonly InputType Input_RemoveValue = new InputType("Subtract 1", true);

			public static readonly InputType Input_ResetValue = new InputType("Reset value", true);

			public InputType(string value, bool register = false) : base(value, register) { }
		}

		public class OutputType : ExtEnum<OutputType>
		{
			public static readonly OutputType Output_Trigger = new OutputType("Triggerd", true);

			public static readonly OutputType Output_Toggle = new OutputType("Toggled", true);

			public static readonly OutputType Output_On = new OutputType("On", true);

			public static readonly OutputType Output_Off = new OutputType("Off", true);

			public OutputType(string value, bool register = false) : base(value, register) { }
		}

		/// <summary>
		/// The Name to display when choosing an <see cref="InputType"/> for a given <see cref="PlacedObject"/> 
		/// </summary>
		public static string NameForInputType(InputType type, PlacedObject.Type objType)
		{
			if (type == InputType.Input_On && objType == PlacedObjectType.IOLightSource)
			{
				return "Turn On";
			}

			if (type == InputType.Input_Off && objType == PlacedObjectType.IOLightSource)
			{
				return "Turn Off";
			}

			return type.ToString();
		}

		/// <summary>
		/// The Name to display when choosing an <see cref="OutputType"/>  for a given <see cref="PlacedObject"/> 
		/// </summary>
		public static string NameForOutputType(OutputType type, PlacedObject.Type objType)
		{
			if (type == OutputType.Output_On && objType == PlacedObjectType.IOLightSource)
			{
				return "Turned On";
			}

			if (type == OutputType.Output_Off && objType == PlacedObjectType.IOLightSource)
			{
				return "Turned On";
			}

			return type.ToString();
		}
	}
}

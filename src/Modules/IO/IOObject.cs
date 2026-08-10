using static RegionKit.Modules.IO._Enums;

#nullable disable

namespace RegionKit.Modules.IO
{
	public class IOObject : UpdatableAndDeletable
	{
		public class IODelayer
		{
			public int startDelay;

			public int delay;

			public string ID;

			public IOObject owner;

			public Room room;

			public IODelayer(IOObject obj, Room room, string ID, int Delay) : base()
			{
				this.ID = ID;
				this.delay = Delay;
				startDelay = delay;
				this.room = room;
				this.owner = obj;
			}

			public void Update()
			{
				if (delay == 0)
				{
					for (int i = 0; i < room.CustomData().IOObjects.Count; i++)
					{
						IOType.IOData Data = room.CustomData().IOObjects[i].Data;
						foreach (IOType.IODataHolder dataholder in Data.IOHolder)
						{
							if (dataholder.MessageID == ID && dataholder.InputType)
							{
								System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(InputType).TypeHandle);
								if (ExtEnum<InputType>.Parse(typeof(InputType), dataholder.IOType, true) != null)
								{
									InputType e = (InputType)ExtEnum<InputType>.Parse(typeof(InputType), dataholder.IOType, true);

									room.CustomData().IOObjects[i].ReciveInput(e);
								}
							}
						}
					}

					owner.Delays.Remove(this);
				}

				delay--;
			}

			public override string ToString()
			{
				return $"[{delay}, {ID}, {owner.GetType()}, {room.abstractRoom.name}]";
			}
		}

		public PlacedObject placedObject;

		public IOType.IOData Data;

		public List<IODelayer> Delays = new List<IODelayer>();
		public IOObject(PlacedObject pObj) : base()
		{
			this.placedObject = pObj;
			this.Data = placedObject.data as IOType.IOData;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			for (int i = 0; i < Delays.Count; i++)
			{
				Delays[i].Update();
			}
		}

		/// <summary>
		/// Send an Output for other I/O objects to recive
		/// </summary>
		/// <param name="type">OutputType the type of output that your object sent</param>
		public void SendOutput(OutputType type)
		{
			for (int i = 0; i < Data.IOHolder.Count; i++)
			{
				if (!Data.IOHolder[i].InputType && ExtEnum<OutputType>.Parse(typeof(OutputType), Data.IOHolder[i].IOType, true) != null)
				{
					OutputType e = (OutputType)ExtEnum<OutputType>.Parse(typeof(OutputType), Data.IOHolder[i].IOType, true);

					ConsoleVisualizerIO.LogIO(room.world.game, GetType().Name + " => \"" + Data.IOHolder[i].MessageID + "\" : [" + e.ToString() + "]");
					Delays.Add(new IODelayer(this, this.room, Data.IOHolder[i].MessageID, (int)(Data.IOHolder[i].Delay * 10f)));
				}
			}
		}

		/// <summary>
		/// Recive an input
		/// </summary>
		/// <param name="type">InputType the type of input that has been triggerd for your object</param>
		public virtual void ReciveInput(InputType type)
		{
		}
	}
}

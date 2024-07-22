using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RegionKit.Modules.Atmo.Data;

namespace RegionKit.Modules.Atmo.Body;

public abstract class HappenAction
{
	public Happen owner;
	public ArgSet args;
	protected HappenAction(Happen owner, ArgSet args)
	{
		this.owner = owner;
		this.args = args;
	}

	public virtual void AbstractUpdate() { }

	public virtual void RealizedUpdate(Room room) { }

	public virtual void Init() { }

	public class EventfulAction : HappenAction
	{
		public EventfulAction(Happen owner, ArgSet args) : base(owner, args)
		{
		}

		public Action<EventfulAction, Room>? On_RealizedUpdate;
		public Action<EventfulAction>? On_AbstractUpdate;
		public Action<EventfulAction>? On_Init;

		public override void AbstractUpdate()
		{
			On_AbstractUpdate?.Invoke(this);
		}

		public override void RealizedUpdate(Room room)
		{
			On_RealizedUpdate?.Invoke(this, room);
		}

		public override void Init()
		{
			On_Init?.Invoke(this);
		}
	}

	public class EventfulAction<T> : HappenAction
	{
		public T persistent;
		public EventfulAction(Happen owner, ArgSet args, T persistent) : base(owner, args)
		{
			this.persistent = persistent;
		}
		public Action<EventfulAction<T>, Room>? On_RealizedUpdate;
		public Action<EventfulAction<T>>? On_AbstractUpdate;
		public Action<EventfulAction<T>>? On_Init;
		public override void AbstractUpdate()
		{
			On_AbstractUpdate?.Invoke(this);
		}

		public override void RealizedUpdate(Room room)
		{
			On_RealizedUpdate?.Invoke(this, room);
		}

		public override void Init()
		{
			On_Init?.Invoke(this);
		}
	}
}


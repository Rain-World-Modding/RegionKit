using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Modules.Effects;
internal class NonlethalWater : UpdatableAndDeletable
{
	public NonlethalWater(EffExt.EffectExtraData data)
	{
		if (data.Amount == 0) Destroy();
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		foreach (AbstractCreature creature in room.abstractRoom.creatures)
		{
			if (creature.realizedCreature == null) continue;
			if (creature.realizedCreature is AirBreatherCreature a) a.lungs = 1f;
			if (creature.realizedCreature is Player p) p.airInLungs = 1f;
		}
	}
}

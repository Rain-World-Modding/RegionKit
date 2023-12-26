using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OverseerHolograms;
using UnityEngine;

namespace RegionKit.Modules.CustomProjections;

public class DoorPointerHologram : OverseerHologram.DoorPointer
{
	// Token: 0x060023A6 RID: 9126 RVA: 0x002354B0 File Offset: 0x002336B0
	public DoorPointerHologram(Overseer overseer, Message message, Creature communicateWith, float importance) : base(overseer, message, communicateWith, importance)
	{

		cycleColor = new Color(1f, 0f, 0f);
		cycleDuration = 15;
		cycleLength = 30;

		direction = (overseer.AI.communication.forcedDirectionToGive as CustomDoorPointer)!;

		symbol = new Symbol(this, totalSprites, direction.data.Symbol);
		AddPart(symbol);
	}

	// Token: 0x170005A0 RID: 1440
	// (get) Token: 0x060023A7 RID: 9127 RVA: 0x002355BC File Offset: 0x002337BC
	public override Color color
	{
		get
		{
			if (cycle % cycleLength < cycleDuration)
			{
				return cycleColor;
			}
			return base.color;
		}
	}

	// Token: 0x060023A8 RID: 9128 RVA: 0x00235688 File Offset: 0x00233888
	public override void Update(bool eu)
	{
		cycle++;
		base.Update(eu);
		if (direction == null || direction.room != overseer.room)
		{
			stillRelevant = false;
			return;
		}
		door = direction.data.Exit;
		pointerNecessary = true;
		symbol.visible = true;
		lightEffect.visible = true;
	}

	// Token: 0x04002700 RID: 9984
	CustomDoorPointer direction;

	// Token: 0x04002701 RID: 9985
	public int cycle;

	public int cycleDuration;

	public int cycleLength;

	public Color cycleColor;

}

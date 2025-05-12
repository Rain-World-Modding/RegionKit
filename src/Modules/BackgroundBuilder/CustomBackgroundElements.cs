using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionKit.Modules.BackgroundBuilder;

internal static class CustomBackgroundElements
{
	public class SimpleBackgroundElement : BackgroundScene.BackgroundSceneElement
	{
		public SimpleBackgroundElement(BackgroundScene scene, string assetName, Vector2 pos, float depth)
			: base(scene, pos, depth)
		{
			this.assetName = assetName;
			this.scene.LoadGraphic(assetName, true, false);
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			base.InitiateSprites(sLeaser, rCam);
			sLeaser.sprites = new FSprite[1];
			sLeaser.sprites[0] = new FSprite(assetName, true);
			sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Background"];
			this.AddToContainer(sLeaser, rCam, null);
		}

		// Token: 0x0600465C RID: 18012 RVA: 0x0057914C File Offset: 0x0057734C
		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			float yShift = scene is AboveCloudsView acv ? acv.yShift : 0f;
			Vector2 vector = DrawPos(new Vector2(camPos.x, camPos.y + yShift), rCam.hDisplace);
			sLeaser.sprites[0].x = vector.x;
			sLeaser.sprites[0].y = vector.y;
			sLeaser.sprites[0].color = new Color(Mathf.Pow(Mathf.InverseLerp(0f, 600f, depth), 0.3f) * 0.9f, 0f, 0f);
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		}

		public string assetName;
	}
}

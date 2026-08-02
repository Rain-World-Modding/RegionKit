using System.Runtime.CompilerServices;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RegionKit.Modules.Effects
{
	internal static class MurkyWater
	{
		// Original by ASlightlyOvergrownCactus with help from Xan
		// MurkyWater 2.0 by Alduris with help from SlimeCubed

		private const string MaskName = "_MurkyWaterMask";
		private const string MaskShader = "MurkyWaterSaveMask";

		private static readonly ConditionalWeakTable<RoomCamera, MurkyWaterCameraData> cameraDataCWT = new();

		private static void UpdateCameraData(RoomCamera rCam, Room updateRoom)
		{
			if (!cameraDataCWT.TryGetValue(rCam, out MurkyWaterCameraData cameraData))
			{
				cameraDataCWT.Add(rCam, cameraData = new MurkyWaterCameraData(rCam));
			}

			if (updateRoom != rCam.room)
			{
				cameraData.needsClear = true;
			}
		}

		private static void RefreshCameraData(RoomCamera rCam)
		{
			if (cameraDataCWT.TryGetValue(rCam, out MurkyWaterCameraData cameraData))
			{
				if (cameraData.needsClear)
				{
					cameraData.Clear();
				}
			}
			else
			{
				Shader.SetGlobalTexture(MaskName, Texture2D.blackTexture);
			}
		}

		internal static void Apply()
		{
			On.Lantern.InitiateSprites += Lantern_InitiateSprites;
			On.LightSource.AddToContainer += LightSource_AddToContainer;
			On.LightSource.InitiateSprites += LightSource_InitiateSprites;

			On.RoomCamera.Update += RoomCamera_Update;
			On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
			On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera_int;
			On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int;
			On.RoomCamera.WarpMoveCameraActual += RoomCamera_WarpMoveCameraActual;
			On.RoomCamera.ApplyPositionChange += RoomCamera_ApplyPositionChange;
			On.RoomCamera.ClearAllSprites += RoomCamera_ClearAllSprites;

			On.Water.DrawSprites += Water_DrawSprites;
		}

		internal static void Undo()
		{
			On.Lantern.InitiateSprites -= Lantern_InitiateSprites;
			On.LightSource.AddToContainer -= LightSource_AddToContainer;
			On.LightSource.InitiateSprites -= LightSource_InitiateSprites;

			On.RoomCamera.Update -= RoomCamera_Update;
			On.RoomCamera.DrawUpdate -= RoomCamera_DrawUpdate;
			On.RoomCamera.MoveCamera_int -= RoomCamera_MoveCamera_int;
			On.RoomCamera.MoveCamera_Room_int -= RoomCamera_MoveCamera_Room_int;
			On.RoomCamera.WarpMoveCameraActual -= RoomCamera_WarpMoveCameraActual;
			On.RoomCamera.ApplyPositionChange -= RoomCamera_ApplyPositionChange;
			On.RoomCamera.ClearAllSprites -= RoomCamera_ClearAllSprites;

			On.Water.DrawSprites -= Water_DrawSprites;
		}

		private static void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
		{
			if (self.usingBlankHoldFrame && self.room != null && !cameraDataCWT.TryGetValue(self, out _) && self.room.roomSettings.GetEffect(_Enums.MurkyWater) != null)
			{
				UpdateCameraData(self, self.room);
				RefreshCameraData(self);
			}

			if (cameraDataCWT.TryGetValue(self, out MurkyWaterCameraData? cameraData) && cameraData != null)
			{
				cameraData.Update();
			}
			orig(self);
		}

		private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
		{
			orig(self, timeStacker, timeSpeed);
			if (self.room != null && cameraDataCWT.TryGetValue(self, out MurkyWaterCameraData? cameraData))
			{
				cameraData.DrawUpdate();
			}
		}

		private static void RoomCamera_MoveCamera_int(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera self, int camPos)
		{
			orig(self, camPos);
			UpdateCameraData(self, self.room);
		}

		private static void RoomCamera_MoveCamera_Room_int(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera self, Room newRoom, int camPos)
		{
			orig(self, newRoom, camPos);
			UpdateCameraData(self, newRoom);
		}

		private static void RoomCamera_WarpMoveCameraActual(On.RoomCamera.orig_WarpMoveCameraActual orig, RoomCamera self, Room newRoom, int camPos)
		{
			orig(self, newRoom, camPos);
			UpdateCameraData(self, newRoom);
		}

		private static void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera self)
		{
			RefreshCameraData(self);
			orig(self);
		}

		private static void RoomCamera_ClearAllSprites(On.RoomCamera.orig_ClearAllSprites orig, RoomCamera self)
		{
			orig(self);
			if (cameraDataCWT.TryGetValue(self, out MurkyWaterCameraData cameraData))
			{
				cameraData.Dispose();
			}
		}

		private static void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (cameraDataCWT.TryGetValue(rCam, out MurkyWaterCameraData cameraData))
			{
				cameraData.UpdateMesh(self, sLeaser);
			}
		}

		private static void LightSource_AddToContainer(On.LightSource.orig_AddToContainer orig, LightSource self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			orig(self, sLeaser, rCam, newContatiner);
			if (rCam.room.roomSettings.GetEffect(_Enums.MurkyWater) != null && self.room == rCam.room)
			rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[0]);
		}

		private static void LightSource_InitiateSprites(On.LightSource.orig_InitiateSprites orig, LightSource self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			if (rCam.room.roomSettings.GetEffect(_Enums.MurkyWater) != null && self.room == rCam.room)
				sLeaser.sprites[0].shader = self.room.game.rainWorld.Shaders["MurkyWaterLightSource"];
		}

		private static void Lantern_InitiateSprites(On.Lantern.orig_InitiateSprites orig, Lantern self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			if (rCam.room.roomSettings.GetEffect(_Enums.MurkyWater) != null)
				sLeaser.sprites[3].shader = self.room.game.rainWorld.Shaders["MurkyWaterLightSource"];
		}


		private class MurkyWaterCameraData
		{
			private RoomCamera owner;
			private RenderTexture waterRenderTexture;
			private Material waterMaskMaterial;
			private Mesh? waterMesh;
			private Vector3[] waterVertexArray = [];
			private int[] waterIndexArray = [];

			public bool needsClear = false;
			public bool isPassAdded = false;

			public MurkyWaterCameraData(RoomCamera owner)
			{
				this.owner = owner;

				waterRenderTexture = new RenderTexture(Futile.screen.pixelWidth / 4, Futile.screen.pixelHeight / 4, 0)
				{
					useMipMap = true,
					autoGenerateMips = true,
					filterMode = FilterMode.Bilinear
				};

				waterMaskMaterial = new Material(owner.game.rainWorld.Shaders[MaskShader].shader);
			}

			public void Clear()
			{
				if (!needsClear) return;

				Shader.SetGlobalTexture(MaskName, Texture2D.blackTexture);
				Graphics.Blit(Texture2D.blackTexture, waterRenderTexture);

				needsClear = false;
			}

			public void Update()
			{
				if (owner.room != null)
				{
					float amount = owner.room.roomSettings.GetEffectAmount(_Enums.MurkyWater);
					Shader.SetGlobalFloat("_MurkyWaterAmount", amount);
				}
			}

			public void DrawUpdate()
			{
				if (waterMesh != null)
				{
					// Save previous state
					Camera cam = Camera.main;
					RenderTexture lastActiveRt = RenderTexture.active;
					RenderTexture.active = waterRenderTexture;
					GL.PushMatrix();

					// Draw water mesh into waterRenderTexture
					GL.modelview = cam.worldToCameraMatrix;
					GL.LoadProjectionMatrix(cam.projectionMatrix);
					GL.Clear(true, true, Color.clear);
					waterMaskMaterial.SetPass(0);
					Graphics.DrawMeshNow(waterMesh, Matrix4x4.identity);

					// Restore previous state
					GL.PopMatrix();
					RenderTexture.active = lastActiveRt;

					// Make the mask available to shaders
					Shader.SetGlobalTexture(MaskName, waterRenderTexture);
				}
				else
				{
					Graphics.Blit(Texture2D.blackTexture, waterRenderTexture);
				}
			}

			public void UpdateMesh(Water water, RoomCamera.SpriteLeaser sLeaser)
			{
				if (waterMesh == null)
				{
					waterMesh = new Mesh();
				}

				// Figure out how long we need our stuff to be
				int meshesToCopy = water.surfaces.Length;
				int verticesLength = 0;
				int indicesLength = 0;
				for (int i = 0; i < meshesToCopy; i++)
				{
					WaterTriangleMesh mesh = (sLeaser.sprites[i * 2 + 1] as WaterTriangleMesh)!;
					verticesLength += mesh.vertices.Length;
					indicesLength += mesh.triangles.Length * 3;
				}

				// Resize arrays as necessary
				if (waterVertexArray.Length != verticesLength)
					Array.Resize(ref waterVertexArray, verticesLength);
				if (waterIndexArray.Length != indicesLength)
					Array.Resize(ref waterIndexArray, indicesLength);

				// Put data in the arrays
				for (int i = 0, vert = 0, ind = 0; i < meshesToCopy; i++)
				{
					WaterTriangleMesh mesh = (sLeaser.sprites[i * 2 + 1] as WaterTriangleMesh)!;
					int initialVertLength = vert;
					for (int j = 0; j < mesh.vertices.Length; j++)
					{
						waterVertexArray[vert++] = mesh.vertices[j];
					}
					for (int j = 0; j < mesh.triangles.Length; j++)
					{
						waterIndexArray[ind++] = mesh.triangles[j].a + initialVertLength;
						waterIndexArray[ind++] = mesh.triangles[j].b + initialVertLength;
						waterIndexArray[ind++] = mesh.triangles[j].c + initialVertLength;
					}
				}

				// Assign to mesh
				waterMesh.vertices = waterVertexArray;
				waterMesh.triangles = waterIndexArray;
			}

			public void Dispose()
			{
				Object.Destroy(waterRenderTexture);
				Object.Destroy(waterMaskMaterial);
				Object.Destroy(waterMesh);
				waterVertexArray = [];
				waterIndexArray = [];
			}
		}
	}
}

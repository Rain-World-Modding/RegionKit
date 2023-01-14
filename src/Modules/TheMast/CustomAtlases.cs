using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Collections;

//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast
{
    internal static class CustomAtlases
    {
        public static FAtlas FetchAtlas(string name)
        {
            if (Futile.atlasManager.DoesContainAtlas(name)) return Futile.atlasManager.GetAtlasWithName(name);
            else return LoadAtlas(name);
        }

        private static FAtlas LoadAtlas(string name)
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            // Load image from resources
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            {
                Stream atlasImage = asm.GetManifestResourceStream($"RegionKit.TheMast.Atlases.{name}.png");
                byte[] data = new byte[atlasImage.Length];
                atlasImage.Read(data, 0, data.Length);
                tex.LoadImage(data);
                tex.filterMode = FilterMode.Point;
            }

            string json;
            {
                Stream atlasJson = asm.GetManifestResourceStream($"RegionKit.TheMast.Atlases.{name}.txt");
                byte[] data = new byte[atlasJson.Length];
                atlasJson.Read(data, 0, data.Length);
                json = System.Text.Encoding.UTF8.GetString(data);
            }

            FAtlas atlas = Futile.atlasManager.LoadAtlasFromTexture(name, tex, false);
            LoadAtlasData(atlas, json);
            return atlas;
        }

        // Fixes a bug from FAtlas.LoadAtlasData
        private static FieldInfo _FAtlas_elementsByName = typeof(FAtlas).GetField("_elementsByName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo _FAtlasManager_AddAtlas = typeof(FAtlasManager).GetMethod("AddAtlas", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static void LoadAtlasData(FAtlas atlas, string data)
        {
            Dictionary<string, FAtlasElement> elementsByName = (Dictionary<string, FAtlasElement>)_FAtlas_elementsByName.GetValue(atlas);
            atlas.elements.Clear();
            elementsByName.Clear();

            Dictionary<string, object> atlasData = data.dictionaryFromJson();
            Dictionary<string, object> frames = (Dictionary<string, object>)atlasData["frames"];
            float invScl = Futile.resourceScaleInverse;
            int elemIndex = 0;
            foreach (KeyValuePair<string, object> element in frames)
            {
                FAtlasElement fatlasElement = new FAtlasElement();
                fatlasElement.indexInAtlas = elemIndex++;
                string text = element.Key;
                if (Futile.shouldRemoveAtlasElementFileExtensions)
                {
                    int num2 = text.LastIndexOf(".");
                    if (num2 >= 0) text = text.Substring(0, num2);
                }
                fatlasElement.name = text;
                IDictionary elementProperties = (IDictionary)element.Value;
                fatlasElement.isTrimmed = (bool)elementProperties["trimmed"];
                IDictionary elemFrame = (IDictionary)elementProperties["frame"];
                float elemX = float.Parse(elemFrame["x"].ToString());
                float elemY = float.Parse(elemFrame["y"].ToString());
                float elemW = float.Parse(elemFrame["w"].ToString());
                float elemH = float.Parse(elemFrame["h"].ToString());
                Rect uvRect = new Rect(elemX / atlas.textureSize.x, (atlas.textureSize.y - elemY - elemH) / atlas.textureSize.y, elemW / atlas.textureSize.x, elemH / atlas.textureSize.y);
                fatlasElement.uvRect = uvRect;
                fatlasElement.uvTopLeft.Set(uvRect.xMin, uvRect.yMax);
                fatlasElement.uvTopRight.Set(uvRect.xMax, uvRect.yMax);
                fatlasElement.uvBottomRight.Set(uvRect.xMax, uvRect.yMin);
                fatlasElement.uvBottomLeft.Set(uvRect.xMin, uvRect.yMin);
                IDictionary elemSourceSize = (IDictionary)elementProperties["sourceSize"];
                fatlasElement.sourcePixelSize.x = float.Parse(elemSourceSize["w"].ToString());
                fatlasElement.sourcePixelSize.y = float.Parse(elemSourceSize["h"].ToString());
                fatlasElement.sourceSize.x = fatlasElement.sourcePixelSize.x * invScl;
                fatlasElement.sourceSize.y = fatlasElement.sourcePixelSize.y * invScl;
                IDictionary dictionary6 = (IDictionary)elementProperties["spriteSourceSize"];
                float left = float.Parse(dictionary6["x"].ToString()) * invScl;
                float top = float.Parse(dictionary6["y"].ToString()) * invScl;
                float width = float.Parse(dictionary6["w"].ToString()) * invScl;
                float height = float.Parse(dictionary6["h"].ToString()) * invScl;
                fatlasElement.sourceRect = new Rect(left, top, width, height);
                atlas.elements.Add(fatlasElement);
                elementsByName.Add(fatlasElement.name, fatlasElement);
            }

            _FAtlasManager_AddAtlas.Invoke(Futile.atlasManager, new object[] { atlas });
        }
    }
}

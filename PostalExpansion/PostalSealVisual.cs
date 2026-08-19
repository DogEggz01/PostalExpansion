using System;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PostalExpansion
{
	internal sealed class PostalSealVisual
	{
		private readonly string objectName;
		private readonly string textureFileName;
		private readonly string resourceName;
		private readonly float size;

		private Texture2D texture;
		private Material material;
		private Mesh mesh;

		internal PostalSealVisual(
			string objectName,
			string textureFileName,
			string resourceName,
			float size)
		{
			this.objectName = objectName;
			this.textureFileName = textureFileName;
			this.resourceName = resourceName;
			this.size = size;
		}

		internal void Ensure(GameObject prefab, Vector3 localPosition, Quaternion localRotation)
		{
			if (prefab == null)
			{
				return;
			}

			MeshRenderer parentRenderer = prefab.GetComponent<MeshRenderer>();
			Material sealMaterial = GetMaterial(parentRenderer);
			if (sealMaterial == null)
			{
				return;
			}

			Transform sealTransform = prefab.transform.Find(objectName);
			if (sealTransform == null)
			{
				sealTransform = new GameObject(objectName).transform;
				sealTransform.SetParent(prefab.transform, false);
			}

			sealTransform.localPosition = localPosition;
			sealTransform.localRotation = localRotation;
			sealTransform.localScale = Vector3.one;

			GameObject sealObject = sealTransform.gameObject;
			sealObject.SetActive(true);
			sealObject.layer = prefab.layer;
			MeshFilter meshFilter = sealObject.GetComponent<MeshFilter>() ??
				sealObject.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = GetMesh();

			MeshRenderer renderer = sealObject.GetComponent<MeshRenderer>() ??
				sealObject.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = sealMaterial;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = true;
			if (parentRenderer != null)
			{
				renderer.lightProbeUsage = parentRenderer.lightProbeUsage;
				renderer.reflectionProbeUsage = parentRenderer.reflectionProbeUsage;
				renderer.renderingLayerMask = parentRenderer.renderingLayerMask;
			}
		}

		private Material GetMaterial(MeshRenderer parentRenderer)
		{
			Texture2D sealTexture = LoadTexture();
			if (sealTexture == null)
			{
				return null;
			}

			if (material != null)
			{
				return material;
			}

			Material parentMaterial = parentRenderer?.sharedMaterial;
			if (parentMaterial == null || parentMaterial.shader == null)
			{
				Debug.LogWarning(
					$"Postal Expansion: no source material was found for the {resourceName}.");
				return null;
			}

			material = new Material(parentMaterial)
			{
				name = resourceName + "_material",
				mainTexture = sealTexture,
				color = Color.white,
				renderQueue = (int)RenderQueue.AlphaTest
			};
			ConfigureLitCutoutMaterial(material);
			return material;
		}

		private static void ConfigureLitCutoutMaterial(Material targetMaterial)
		{
			targetMaterial.SetOverrideTag("RenderType", "TransparentCutout");
			if (targetMaterial.HasProperty("_Mode"))
			{
				targetMaterial.SetFloat("_Mode", 1f);
			}
			if (targetMaterial.HasProperty("_Cutoff"))
			{
				targetMaterial.SetFloat("_Cutoff", 0.1f);
			}
			if (targetMaterial.HasProperty("_SrcBlend"))
			{
				targetMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
			}
			if (targetMaterial.HasProperty("_DstBlend"))
			{
				targetMaterial.SetInt("_DstBlend", (int)BlendMode.Zero);
			}
			if (targetMaterial.HasProperty("_ZWrite"))
			{
				targetMaterial.SetInt("_ZWrite", 1);
			}
			if (targetMaterial.HasProperty("_Cull"))
			{
				targetMaterial.SetInt("_Cull", (int)CullMode.Back);
			}
			if (targetMaterial.HasProperty("_Glossiness"))
			{
				targetMaterial.SetFloat("_Glossiness", 0f);
			}
			if (targetMaterial.HasProperty("_Metallic"))
			{
				targetMaterial.SetFloat("_Metallic", 0f);
			}
			if (targetMaterial.HasProperty("_SpecularHighlights"))
			{
				targetMaterial.SetFloat("_SpecularHighlights", 0f);
			}
			if (targetMaterial.HasProperty("_GlossyReflections"))
			{
				targetMaterial.SetFloat("_GlossyReflections", 0f);
			}
			if (targetMaterial.HasProperty("_EmissionColor"))
			{
				targetMaterial.SetColor("_EmissionColor", Color.black);
			}

			targetMaterial.EnableKeyword("_ALPHATEST_ON");
			targetMaterial.DisableKeyword("_ALPHABLEND_ON");
			targetMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			targetMaterial.DisableKeyword("_EMISSION");
		}

		private Texture2D LoadTexture()
		{
			if (texture != null)
			{
				return texture;
			}

			string texturePath = GetPostalAssetPath(textureFileName);
			if (texturePath == null)
			{
				Debug.LogWarning(
					$"Postal Expansion: {resourceName} texture was not found.");
				return null;
			}

			byte[] imageData;
			try
			{
				imageData = File.ReadAllBytes(texturePath);
			}
			catch (IOException exception)
			{
				Debug.LogWarning(
					$"Postal Expansion: {resourceName} texture could not be read: " +
					exception.Message);
				return null;
			}
			catch (UnauthorizedAccessException exception)
			{
				Debug.LogWarning(
					$"Postal Expansion: {resourceName} texture could not be read: " +
					exception.Message);
				return null;
			}

			var loadedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			if (!loadedTexture.LoadImage(imageData))
			{
				Debug.LogWarning(
					$"Postal Expansion: {resourceName} texture could not be loaded.");
				Object.Destroy(loadedTexture);
				return null;
			}

			loadedTexture.name = resourceName + "_texture";
			loadedTexture.filterMode = FilterMode.Bilinear;
			loadedTexture.wrapMode = TextureWrapMode.Clamp;
			loadedTexture.anisoLevel = 8;
			texture = loadedTexture;
			return texture;
		}

		private Mesh GetMesh()
		{
			if (mesh != null)
			{
				return mesh;
			}

			float halfSize = size / 2f;
			mesh = new Mesh
			{
				name = resourceName + "_mesh",
				vertices = new[]
				{
					new Vector3(-halfSize, -halfSize, 0f),
					new Vector3(-halfSize, halfSize, 0f),
					new Vector3(halfSize, halfSize, 0f),
					new Vector3(halfSize, -halfSize, 0f),
					new Vector3(-halfSize, -halfSize, 0f),
					new Vector3(-halfSize, halfSize, 0f),
					new Vector3(halfSize, halfSize, 0f),
					new Vector3(halfSize, -halfSize, 0f)
				},
				uv = new[]
				{
					new Vector2(0f, 0f),
					new Vector2(0f, 1f),
					new Vector2(1f, 1f),
					new Vector2(1f, 0f),
					new Vector2(0f, 0f),
					new Vector2(0f, 1f),
					new Vector2(1f, 1f),
					new Vector2(1f, 0f)
				},
				normals = new[]
				{
					Vector3.back,
					Vector3.back,
					Vector3.back,
					Vector3.back,
					Vector3.forward,
					Vector3.forward,
					Vector3.forward,
					Vector3.forward
				},
				triangles = new[]
				{
					0, 1, 2,
					0, 2, 3,
					6, 5, 4,
					7, 6, 4
				}
			};
			mesh.RecalculateBounds();
			return mesh;
		}

		private static string GetPostalAssetPath(string fileName)
		{
			string assemblyDirectory =
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string[] candidatePaths =
			{
				Path.Combine(assemblyDirectory ?? string.Empty, "assets", fileName),
				Path.Combine(Paths.PluginPath, "PostalExpansion", "assets", fileName)
			};

			foreach (string candidatePath in candidatePaths)
			{
				if (File.Exists(candidatePath))
				{
					return candidatePath;
				}
			}

			return null;
		}
	}
}

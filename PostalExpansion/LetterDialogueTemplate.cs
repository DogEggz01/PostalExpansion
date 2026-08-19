using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PostalExpansion
{
	internal static class LetterDialogueTemplate
	{
		private const string TemplateObjectName =
			"postal_expansion_letter_dialogue_template";

		private static GameObject template;
		private static string textRelativePath;

		internal static bool TryCapture(TavernRumorsDude source)
		{
			if (template != null)
			{
				return true;
			}

			if (source == null ||
				source.speechUI == null ||
				source.text == null)
			{
				return false;
			}

			string sourceTextPath = GetRelativePath(
				source.speechUI.transform,
				source.text.transform);
			if (sourceTextPath == null)
			{
				return false;
			}

			GameObject captured = Object.Instantiate(source.speechUI);
			captured.name = TemplateObjectName;
			captured.transform.SetParent(null, true);

			if (source.drinkButton != null)
			{
				string buttonPath = GetRelativePath(
					source.speechUI.transform,
					source.drinkButton.transform);
				Transform button = FindRelativeTransform(
					captured.transform,
					buttonPath);
				if (button != null)
				{
					button.gameObject.SetActive(false);
					Object.Destroy(button.gameObject);
				}
			}

			captured.SetActive(false);
			Object.DontDestroyOnLoad(captured);
			template = captured;
			textRelativePath = sourceTextPath;
			return true;
		}

		internal static GameObject Create(
			Transform parent,
			out TextMesh dialogueText)
		{
			dialogueText = null;
			if (parent == null || (!EnsureCaptured() && template == null))
			{
				return null;
			}

			GameObject panel = Object.Instantiate(template);
			panel.name = "postal_expansion_letter_dialogue";
			panel.transform.SetParent(parent, false);
			panel.transform.localPosition = Vector3.zero;
			panel.transform.localRotation = Quaternion.identity;

			Transform textTransform = FindRelativeTransform(
				panel.transform,
				textRelativePath);
			dialogueText = textTransform != null
				? textTransform.GetComponent<TextMesh>()
				: null;
			panel.SetActive(false);
			if (dialogueText != null)
			{
				return panel;
			}

			Object.Destroy(panel);
			return null;
		}

		internal static void Dispose()
		{
			if (template != null)
			{
				Object.Destroy(template);
			}

			template = null;
			textRelativePath = null;
		}

		private static bool EnsureCaptured()
		{
			if (template != null)
			{
				return true;
			}

			foreach (TavernRumorsDude source in
				Object.FindObjectsOfType<TavernRumorsDude>())
			{
				if (TryCapture(source))
				{
					return true;
				}
			}

			foreach (TavernRumorsDude source in
				Resources.FindObjectsOfTypeAll<TavernRumorsDude>())
			{
				if (TryCapture(source))
				{
					return true;
				}
			}

			return false;
		}

		private static Transform FindRelativeTransform(
			Transform root,
			string relativePath)
		{
			if (root == null || relativePath == null)
			{
				return null;
			}

			return relativePath.Length == 0
				? root
				: root.Find(relativePath);
		}

		private static string GetRelativePath(Transform root, Transform child)
		{
			if (root == null || child == null)
			{
				return null;
			}

			var names = new List<string>();
			Transform current = child;
			while (current != null && current != root)
			{
				names.Add(current.name);
				current = current.parent;
			}

			if (current != root)
			{
				return null;
			}

			names.Reverse();
			return string.Join("/", names.ToArray());
		}
	}

	[HarmonyPatch(typeof(TavernRumorsDude), "Awake")]
	internal static class LetterDialogueTemplateCapturePatch
	{
		private static void Postfix(TavernRumorsDude __instance)
		{
			LetterDialogueTemplate.TryCapture(__instance);
		}
	}
}

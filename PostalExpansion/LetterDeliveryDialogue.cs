using System;
using UnityEngine;

namespace PostalExpansion
{
	internal sealed class LetterDeliveryDialogue
	{
		private const string BubbleGraphicsName = "gfx";
		private const int DialogueWrapCharacterLimit = 40;
		private const float ObserverOffset = 0.75f;
		private const float HeadHeightOffset = 0.5f;
		private const float BubblePaddingMultiplier = 1.15f;
		private const float MinimumBubbleWidthMultiplier = 0.8f;
		private const float MaximumBubbleWidthMultiplier = 1.8f;

		private static bool missingBubbleGraphicsWarningLogged;

		private readonly Transform anchor;
		private readonly int portIndex;
		private bool missingTemplateWarningLogged;
		private GameObject panel;
		private TextMesh text;
		private Transform bubbleGraphics;
		private Vector3 baseBubbleScale;
		private float baseBubbleWidth;

		internal LetterDeliveryDialogue(Transform dialogueAnchor, int destinationPortIndex)
		{
			anchor = dialogueAnchor;
			portIndex = destinationPortIndex;
		}

		internal bool Show(string dialogueText)
		{
			if (!EnsureCreated())
			{
				return false;
			}

			string wrappedText = Wrap(
				dialogueText,
				DialogueWrapCharacterLimit);
			text.text = wrappedText;
			ResizeBubble(wrappedText);
			PositionTowardObserver();
			panel.SetActive(true);
			FaceObserver();
			return true;
		}

		internal void Hide()
		{
			if (panel != null)
			{
				panel.SetActive(false);
			}
		}

		internal void FaceObserver()
		{
			if (panel == null ||
				!panel.activeInHierarchy ||
				Refs.observerMirror == null)
			{
				return;
			}

			PositionTowardObserver();
			panel.transform.rotation = Refs.observerMirror.transform.rotation;
		}

		private void PositionTowardObserver()
		{
			if (panel == null || Refs.observerMirror == null)
			{
				return;
			}

			Vector3 towardObserver =
				Refs.observerMirror.transform.position - anchor.position;
			towardObserver.y = 0f;

			if (towardObserver.sqrMagnitude < 0.001f)
			{
				towardObserver = -anchor.forward;
			}
			else
			{
				towardObserver.Normalize();
			}

			Vector3 panelPosition =
				anchor.position + towardObserver * ObserverOffset;
			panelPosition.y =
				Refs.observerMirror.transform.position.y + HeadHeightOffset;
			panel.transform.position = panelPosition;
		}

		private bool EnsureCreated()
		{
			if (panel != null && text != null)
			{
				return true;
			}

			panel = LetterDialogueTemplate.Create(anchor, out text);
			if (panel == null || text == null)
			{
				if (!missingTemplateWarningLogged)
				{
					missingTemplateWarningLogged = true;
					Debug.LogWarning(
						"Postal Expansion: the letter dialogue template is unavailable at port " +
						portIndex + ".");
				}

				return false;
			}

			InitializeBubbleLayout();
			panel.SetActive(false);
			return true;
		}

		private void InitializeBubbleLayout()
		{
			Transform bubbleRoot = text != null ? text.transform.parent : null;
			bubbleGraphics = bubbleRoot != null
				? bubbleRoot.Find(BubbleGraphicsName)
				: null;

			MeshFilter graphicsMesh = bubbleGraphics != null
				? bubbleGraphics.GetComponent<MeshFilter>()
				: null;
			if (graphicsMesh == null || graphicsMesh.sharedMesh == null)
			{
				if (!missingBubbleGraphicsWarningLogged)
				{
					missingBubbleGraphicsWarningLogged = true;
					Debug.LogWarning(
						"Postal Expansion: letter dialogue bubble graphics could not be found; dynamic sizing is disabled.");
				}

				return;
			}

			baseBubbleScale = bubbleGraphics.localScale;
			baseBubbleWidth =
				graphicsMesh.sharedMesh.bounds.size.x *
				Mathf.Abs(baseBubbleScale.x);
		}

		private void ResizeBubble(string dialogueText)
		{
			if (bubbleGraphics == null ||
				text == null ||
				text.font == null ||
				baseBubbleWidth <= 0f)
			{
				return;
			}

			bubbleGraphics.localScale = baseBubbleScale;
			text.font.RequestCharactersInTexture(
				dialogueText,
				text.fontSize,
				text.fontStyle);

			float lineWidth = 0f;
			float maximumLineWidth = 0f;
			foreach (char character in dialogueText)
			{
				if (character == '\n')
				{
					maximumLineWidth = Mathf.Max(
						maximumLineWidth,
						lineWidth);
					lineWidth = 0f;
					continue;
				}

				if (text.font.GetCharacterInfo(
						character,
						out CharacterInfo info,
						text.fontSize,
						text.fontStyle))
				{
					lineWidth += info.advance * text.characterSize;
				}
			}

			maximumLineWidth = Mathf.Max(
				maximumLineWidth,
				lineWidth) * Mathf.Abs(text.transform.localScale.x);
			if (maximumLineWidth <= 0f)
			{
				return;
			}

			float widthMultiplier = Mathf.Clamp(
				maximumLineWidth * BubblePaddingMultiplier /
					baseBubbleWidth,
				MinimumBubbleWidthMultiplier,
				MaximumBubbleWidthMultiplier);
			Vector3 scale = baseBubbleScale;
			scale.x *= widthMultiplier;
			bubbleGraphics.localScale = scale;
		}

		private static string Wrap(string value, int size)
		{
			value = value.TrimStart(Array.Empty<char>());
			if (value.Length <= size)
			{
				return value;
			}

			int breakIndex = value.LastIndexOf(' ', size);
			if (breakIndex == -1)
			{
				breakIndex = Mathf.Min(value.Length, size);
			}

			return value.Substring(0, breakIndex) +
				(breakIndex >= value.Length
					? string.Empty
					: "\n" + Wrap(value.Substring(breakIndex), size));
		}
	}
}

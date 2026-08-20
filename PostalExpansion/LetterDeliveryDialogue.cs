using System;
using UnityEngine;

namespace PostalExpansion
{
	internal sealed class LetterDeliveryDialogue
	{
		private const string BubbleGraphicsName = "gfx";
		private const string BubbleReferenceText =
			"Share a drink with a fellow sailor?";
		private const int DialogueWrapCharacterLimit = 40;
		private const float ObserverOffset = 0.75f;
		private const float HeadHeightOffset = 0.5f;
		private const float MinimumBubbleWidthMultiplier = 0.35f;
		private const float MaximumBubbleWidthMultiplier = 1.8f;

		private static bool missingBubbleGraphicsWarningLogged;

		private readonly Transform anchor;
		private readonly int portIndex;
		private bool missingTemplateWarningLogged;
		private GameObject panel;
		private TextMesh text;
		private Transform bubbleGraphics;
		private Vector3 baseBubbleScale;
		private float baseTextWidth;

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

			if (bubbleGraphics == null || text == null || text.font == null)
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
			baseTextWidth = MeasureMaximumLineWidth(BubbleReferenceText);
		}

		private void ResizeBubble(string dialogueText)
		{
			if (bubbleGraphics == null ||
				text == null ||
				text.font == null ||
				baseTextWidth <= 0f)
			{
				return;
			}

			bubbleGraphics.localScale = baseBubbleScale;
			float renderedTextWidth = MeasureMaximumLineWidth(dialogueText);
			if (renderedTextWidth <= 0f)
			{
				return;
			}

			float widthMultiplier = Mathf.Clamp(
				renderedTextWidth / baseTextWidth,
				MinimumBubbleWidthMultiplier,
				MaximumBubbleWidthMultiplier);
			Vector3 scale = baseBubbleScale;
			scale.x *= widthMultiplier;
			bubbleGraphics.localScale = scale;
		}

		private float MeasureMaximumLineWidth(string value)
		{
			text.font.RequestCharactersInTexture(
				value,
				text.fontSize,
				text.fontStyle);

			float lineWidth = 0f;
			float maximumLineWidth = 0f;
			foreach (char character in value)
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
					lineWidth += info.advance;
				}
			}

			return Mathf.Max(maximumLineWidth, lineWidth);
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

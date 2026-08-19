using System.Collections.Generic;
using UnityEngine;

namespace PostalExpansion
{
	internal sealed class LetterDialoguePresenceArea : MonoBehaviour
	{
		private const float DialogueRetryInterval = 0.5f;

		private readonly HashSet<Collider> playerColliders =
			new HashSet<Collider>();

		private LetterDeliveryDialogue dialogue;
		private string pendingText;
		private bool shown;
		private bool consumed;
		private float nextDialogueRetryTime;

		internal void Initialize(
			LetterMissionDefinition missionDefinition)
		{
			dialogue = new LetterDeliveryDialogue(
				transform,
				missionDefinition.DestinationPortIndex);
		}

		internal void ShowForDelivery(string text)
		{
			if (string.IsNullOrEmpty(text) ||
				shown ||
				pendingText != null)
			{
				return;
			}

			consumed = false;
			pendingText = text;
			nextDialogueRetryTime = 0f;
			TryShow();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.CompareTag("Player"))
			{
				playerColliders.Add(other);
				TryShow();
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (!other.CompareTag("Player"))
			{
				return;
			}

			playerColliders.Remove(other);
			if (playerColliders.Count != 0)
			{
				return;
			}

			if (shown)
			{
				dialogue.Hide();
			}

			shown = false;
			consumed = true;
			pendingText = null;
			nextDialogueRetryTime = 0f;
		}

		private void Update()
		{
			if (shown)
			{
				dialogue.FaceObserver();
				return;
			}

			if (pendingText != null &&
				playerColliders.Count > 0 &&
				Time.time >= nextDialogueRetryTime)
			{
				nextDialogueRetryTime = Time.time + DialogueRetryInterval;
				TryShow();
			}
		}

		private void TryShow()
		{
			if (shown || consumed || pendingText == null || playerColliders.Count == 0)
			{
				return;
			}

			shown = dialogue.Show(pendingText);
		}
	}
}

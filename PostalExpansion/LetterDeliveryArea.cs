using System;
using UnityEngine;

namespace PostalExpansion
{
	internal sealed class LetterDeliveryArea : MonoBehaviour
	{
		private Port port;
		private LetterMissionDefinition definition;
		private LetterDialoguePresenceArea dialogueArea;
		private Good waitingLetter;
		private bool closedNoticeShown;

		internal void Initialize(
			Port destinationPort,
			LetterMissionDefinition missionDefinition,
			LetterDialoguePresenceArea presenceArea)
		{
			port = destinationPort;
			definition = missionDefinition;
			dialogueArea = presenceArea;
		}

		private void OnTriggerEnter(Collider other)
		{
			Good letter = other.GetComponentInParent<Good>();
			if (!IsLetter(letter))
			{
				return;
			}

			if (!TryGetMatchingMission(letter, out _))
			{
				ShowNotification("Wrong Letter!");
				return;
			}

			waitingLetter = letter;
			closedNoticeShown = false;
			TryDeliver();
		}

		private void OnTriggerExit(Collider other)
		{
			Good letter = other.GetComponentInParent<Good>();
			if (letter == waitingLetter)
			{
				waitingLetter = null;
				closedNoticeShown = false;
			}
		}

		private void Update()
		{
			if (waitingLetter != null &&
				LetterDeliveryHours.IsOpen(definition.DeliveryWindow))
			{
				TryDeliver();
			}
		}

		private void TryDeliver()
		{
			if (waitingLetter == null ||
				!TryGetMatchingMission(waitingLetter, out Mission mission))
			{
				waitingLetter = null;
				return;
			}

			if (!LetterDeliveryHours.IsOpen(definition.DeliveryWindow))
			{
				if (!closedNoticeShown)
				{
					closedNoticeShown = true;
					ShowNotification("Wrong Time!");
				}

				return;
			}

			Good deliveredLetter = waitingLetter;
			waitingLetter = null;
			closedNoticeShown = false;
			string dialogueText = definition.GetDeliveryDialogue(mission);
			LetterMissions.MissionDelivered(mission);
			deliveredLetter.Deliver();
			dialogueArea?.ShowForDelivery(dialogueText);
		}

		private bool TryGetMatchingMission(Good letter, out Mission mission)
		{
			mission = null;
			if (letter == null || PlayerMissions.missions == null)
			{
				return false;
			}

			int missionIndex = letter.GetMissionIndex();
			if (missionIndex < 0 || missionIndex >= PlayerMissions.missions.Length)
			{
				return false;
			}

			mission = PlayerMissions.missions[missionIndex];
			return mission != null &&
				mission.missionIndex == missionIndex &&
				mission.destinationPort == port &&
				LetterMissions.TryGetDefinition(
					mission,
					out LetterMissionDefinition assignedDefinition) &&
				string.Equals(
					assignedDefinition.Id,
					definition.Id,
					StringComparison.Ordinal);
		}

		private static bool IsLetter(Good good)
		{
			SaveablePrefab saveable =
				good != null ? good.GetComponent<SaveablePrefab>() : null;
			return PostalMail.IsRegisteredLetter(saveable);
		}

		private static void ShowNotification(string message)
		{
			if (NotificationUi.instance != null)
			{
				NotificationUi.instance.ShowNotification(message);
			}
		}
	}
}

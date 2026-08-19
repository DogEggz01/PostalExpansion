using UnityEngine;

namespace PostalExpansion
{
	internal static class MailDueDateCalculator
	{
		internal static int Calculate(
			Port origin,
			Port destination,
			Good mailGood,
			float baseSpeed)
		{
			float missionDistance =
				Mission.GetDistance(origin, destination);
			return CalculateFromMissionDistance(
				missionDistance,
				mailGood,
				baseSpeed);
		}

		internal static int CalculateFromMissionDistance(
			float missionDistance,
			Good mailGood,
			float baseSpeed)
		{
			float distance = missionDistance / 10f;
			float divisor =
				baseSpeed * (Sun.sun.GetRealtimeDayLength() / 60f / 60f);
			if (mailGood.requiredRepLevel > 1)
			{
				divisor *= 3.5f;
			}

			divisor *= mailGood.requiredRepLevel == 1 ? 2.5f : 1.5f;
			int deliveryDays = Mathf.Max(1, Mathf.RoundToInt(distance / divisor));
			return GameState.day + deliveryDays;
		}
	}
}

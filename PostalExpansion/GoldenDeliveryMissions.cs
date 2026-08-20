using System.Collections.Generic;
using UnityEngine;

namespace PostalExpansion
{
	internal static class GoldenDeliveryMissions
	{
		internal const string DisplayName = "Golden Delivery";
		internal const int RequiredReputation = 9;
		internal const float MoneyMultiplier = 10f;
		internal const float ReputationMultiplier = 15f;
		internal const int SeverelyLatePenalty = -50000;

		private const float SpawnChance = 0.5f;
		private const float MinimumDistance = 288f;
		private const float DueDayBaseSpeed = 4.0f;
		private const float ComparableRegularMailPackageMultiplier = 2f;
		private const string ActiveMissionDataKey =
			"PostalExpansion.GoldenDelivery.Active.v1";
		private const string ClaimedOfferDataKey =
			"PostalExpansion.GoldenDelivery.Claimed.v1";

		private static readonly DailySpecialMissionState State =
			new DailySpecialMissionState(
				ActiveMissionDataKey,
				ClaimedOfferDataKey,
				SerializeActiveMission,
				MatchesSavedMission,
				ApplyMissionName);

		internal static bool IsGolden(Mission mission)
		{
			return State.IsMarked(mission);
		}

		internal static void AddMission(Port origin, List<Mission> missions)
		{
			if (!CanOfferAt(origin) || missions == null)
			{
				return;
			}

			PrefabsDirectory directory = PrefabsDirectory.instance;
			if (!PostalExpressMail.EnsureRegistered(directory))
			{
				return;
			}

			var random = new System.Random(GetDailySeed(origin));
			if (random.NextDouble() >= SpawnChance)
			{
				return;
			}

			List<DestinationCandidate> candidates = GetCandidates(origin);
			if (candidates.Count == 0)
			{
				return;
			}

			DestinationCandidate selected = candidates[random.Next(candidates.Count)];
			Mission mission = GenerateMission(origin, selected);
			MarkRuntime(mission);
			missions.Add(mission);
		}

		internal static void MissionAccepted(Mission mission)
		{
			State.MissionAccepted(mission);
		}

		internal static void SavePersistentState()
		{
			State.SavePersistentState();
		}

		internal static void LoadPersistentState()
		{
			State.LoadPersistentState();
		}

		internal static void ResetRuntimeState()
		{
			State.ResetRuntimeState();
		}

		private static bool CanOfferAt(Port origin)
		{
			if (origin == null ||
				origin.portIndex == SailwindPortIndex.SaffronIsland ||
				origin.portIndex == SailwindPortIndex.TestPort ||
				PlayerReputation.GetRepLevel(origin.region) < RequiredReputation)
			{
				return false;
			}

			return State.CanOfferAt(origin);
		}

		private static List<DestinationCandidate> GetCandidates(Port origin)
		{
			var candidates = new List<DestinationCandidate>();
			if (Port.ports == null)
			{
				return candidates;
			}

			foreach (Port destination in Port.ports)
			{
				if (destination == null ||
					destination == origin ||
					destination.portIndex == SailwindPortIndex.TestPort ||
					PlayerAlreadyHasGoldenMission(origin, destination))
				{
					continue;
				}

				float distance = Mission.GetDistance(origin, destination);
				if (distance > MinimumDistance)
				{
					candidates.Add(new DestinationCandidate(destination, distance));
				}
			}

			return candidates;
		}

		private static Mission GenerateMission(
			Port origin,
			DestinationCandidate candidate)
		{
			Good expressGood = PostalExpressMail.Prefab.GetComponent<Good>();
			float distanceReward =
				candidate.Distance * DebugMarketTracker.instance.missionDistanceFee;
			int totalPrice = Mathf.RoundToInt(
				distanceReward *
					ComparableRegularMailPackageMultiplier *
					Plugin.RegularMailRewardMultiplier.Value *
					MoneyMultiplier);
			totalPrice = CurrencyMarket.instance.GetSellPriceInCurrency(
				(Currency)candidate.Destination.region,
				totalPrice,
				false);

			int dueDay = MailDueDateCalculator.Calculate(
				origin,
				candidate.Destination,
				expressGood,
				DueDayBaseSpeed);
			var mission = new Mission(
				origin,
				candidate.Destination,
				PostalExpressMail.Prefab,
				1,
				totalPrice,
				1f,
				0,
				dueDay);
			mission.pricePerKm = mission.totalPrice / mission.distance;
			return mission;
		}

		private static void MarkRuntime(Mission mission)
		{
			State.Mark(mission);
			ApplyMissionName(mission);
		}

		private static void ApplyMissionName(Mission mission)
		{
			mission.missionName =
				DisplayName + " to " + mission.destinationPort.GetPortName();
		}

		private static bool IsExpressMission(Mission mission)
		{
			if (mission == null || mission.goodPrefab == null)
			{
				return false;
			}

			return PostalMail.IsExpressMail(
				mission.goodPrefab.GetComponent<SaveablePrefab>());
		}

		private static bool PlayerAlreadyHasGoldenMission(
			Port origin,
			Port destination)
		{
			if (PlayerMissions.missions == null)
			{
				return false;
			}

			foreach (Mission mission in PlayerMissions.missions)
			{
				if (IsGolden(mission) &&
					mission.originPort == origin &&
					mission.destinationPort == destination)
				{
					return true;
				}
			}

			return false;
		}

		private static int GetDailySeed(Port origin)
		{
			return ((1907 + GameState.day) * 31 + origin.portIndex) * 31 +
				RequiredReputation;
		}

		private static string SerializeActiveMission(int slot, Mission mission)
		{
			return string.Join(",",
				slot,
				mission.originPort.portIndex,
				mission.destinationPort.portIndex,
				mission.dueDay,
				mission.totalPrice);
		}

		private static bool MatchesSavedMission(
			Mission mission,
			string[] fields)
		{
			return fields.Length == 5 &&
				int.TryParse(fields[1], out int originIndex) &&
				int.TryParse(fields[2], out int destinationIndex) &&
				int.TryParse(fields[3], out int dueDay) &&
				int.TryParse(fields[4], out int totalPrice) &&
				IsExpressMission(mission) &&
				mission.goodCount == 1 &&
				mission.originPort.portIndex == originIndex &&
				mission.destinationPort.portIndex == destinationIndex &&
				mission.dueDay == dueDay &&
				mission.totalPrice == totalPrice;
		}

		private readonly struct DestinationCandidate
		{
			internal DestinationCandidate(Port destination, float distance)
			{
				Destination = destination;
				Distance = distance;
			}

			internal Port Destination { get; }
			internal float Distance { get; }
		}
	}
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = System.Random;

namespace PostalExpansion
{
	internal readonly struct LetterMissionSaveKeys
	{
		internal LetterMissionSaveKeys(string activeMissions, string claimedOffers)
		{
			ActiveMissions = activeMissions;
			ClaimedOffers = claimedOffers;
		}

		internal string ActiveMissions { get; }
		internal string ClaimedOffers { get; }
	}

	internal sealed class LetterMissionSet<TDefinition>
		where TDefinition : LetterMissionDefinition
	{
		private readonly string categoryName;
		private readonly IReadOnlyList<TDefinition> definitions;
		private readonly Dictionary<string, TDefinition> definitionsById;
		private readonly LetterMissionSaveKeys saveKeys;
		private readonly int requiredReputation;
		private readonly float dueDayBaseSpeed;
		private readonly int fixedGoldReward;
		private readonly Func<Port, string> getMissionName;

		private ConditionalWeakTable<Mission, MissionData> missionData =
			new ConditionalWeakTable<Mission, MissionData>();
		private readonly Dictionary<string, Mission> dailyOffers =
			new Dictionary<string, Mission>(StringComparer.Ordinal);
		private readonly HashSet<string> claimedOffers =
			new HashSet<string>(StringComparer.Ordinal);
		private int dailyOfferDay = int.MinValue;

		internal LetterMissionSet(
			string categoryName,
			IReadOnlyList<TDefinition> definitions,
			LetterMissionSaveKeys saveKeys,
			int requiredReputation,
			float dueDayBaseSpeed,
			int fixedGoldReward,
			Func<Port, string> getMissionName)
		{
			this.categoryName = categoryName;
			this.definitions = definitions;
			this.saveKeys = saveKeys;
			this.requiredReputation = requiredReputation;
			this.dueDayBaseSpeed = dueDayBaseSpeed;
			this.fixedGoldReward = fixedGoldReward;
			this.getMissionName = getMissionName;

			definitionsById = new Dictionary<string, TDefinition>(
				StringComparer.Ordinal);
			foreach (TDefinition definition in definitions)
			{
				definitionsById.Add(definition.Id, definition);
			}
		}

		internal void AddMissions(Port origin, List<Mission> missions)
		{
			if (origin == null || missions == null)
			{
				return;
			}

			EnsureDailyOffers();
			foreach (TDefinition definition in definitions)
			{
				if (!dailyOffers.TryGetValue(definition.Id, out Mission offer) ||
					offer == null ||
					offer.originPort != origin ||
					!CanOfferFrom(origin, offer.destinationPort) ||
					offer.missionIndex != -1 ||
					IsClaimed(definition.Id) ||
					HasActiveMission(definition.Id))
				{
					continue;
				}

				missions.Add(offer);
			}
		}

		internal List<string> GetDebugStatusLines()
		{
			EnsureDailyOffers();
			var lines = new List<string>();
			foreach (TDefinition definition in definitions)
			{
				string status;
				if (TryGetActiveMission(definition.Id, out Mission activeMission))
				{
					status = "active from " + GetPortName(activeMission.originPort);
				}
				else if (IsClaimed(definition.Id))
				{
					status = "already claimed today";
				}
				else if (dailyOffers.TryGetValue(
						definition.Id,
						out Mission offer) &&
					offer != null &&
					offer.missionIndex == -1 &&
					CanOfferFrom(offer.originPort, offer.destinationPort))
				{
					status = GetPortName(offer.originPort);
				}
				else
				{
					status = "not available today";
				}

				lines.Add(
					categoryName + " | " +
					definition.LocationDescription + ": " + status);
			}

			return lines;
		}

		internal bool TryGetDefinition(
			Mission mission,
			out TDefinition definition)
		{
			if (mission != null &&
				missionData.TryGetValue(mission, out MissionData data))
			{
				definition = data.Definition;
				return true;
			}

			definition = null;
			return false;
		}

		internal void MissionAccepted(Mission mission)
		{
			if (!TryGetDefinition(mission, out TDefinition definition) ||
				mission.missionIndex < 0 ||
				PlayerMissions.missions == null ||
				mission.missionIndex >= PlayerMissions.missions.Length ||
				PlayerMissions.missions[mission.missionIndex] != mission)
			{
				return;
			}

			claimedOffers.Add(GetClaimKey(GameState.day, definition.Id));
		}

		internal void MissionDelivered(Mission mission)
		{
			if (TryGetDefinition(mission, out TDefinition definition))
			{
				claimedOffers.Add(GetClaimKey(GameState.day, definition.Id));
			}
		}

		internal void SavePersistentState()
		{
			if (GameState.modData == null)
			{
				GameState.modData = new Dictionary<string, string>();
			}

			var activeEntries = new List<string>();
			if (PlayerMissions.missions != null)
			{
				for (int i = 0; i < PlayerMissions.missions.Length; i++)
				{
					Mission mission = PlayerMissions.missions[i];
					if (!TryGetDefinition(mission, out TDefinition definition) ||
						mission.originPort == null ||
						mission.destinationPort == null)
					{
						continue;
					}

					activeEntries.Add(string.Join(",",
						i,
						mission.originPort.portIndex,
						mission.destinationPort.portIndex,
						definition.Id));
				}
			}

			GameState.modData[saveKeys.ActiveMissions] =
				string.Join(";", activeEntries);
			PruneClaimsForCurrentDay();
			var claims = new List<string>(claimedOffers);
			claims.Sort(StringComparer.Ordinal);
			GameState.modData[saveKeys.ClaimedOffers] = string.Join(";", claims);
		}

		internal void LoadPersistentState(
			params LetterMissionSaveKeys[] legacySaveKeys)
		{
			ResetRuntimeState();
			if (GameState.modData == null)
			{
				return;
			}

			LoadClaims(saveKeys);
			LoadActiveMissions(saveKeys);
			foreach (LetterMissionSaveKeys legacyKeys in legacySaveKeys)
			{
				LoadClaims(legacyKeys);
				LoadActiveMissions(legacyKeys);
			}
		}

		internal void ResetRuntimeState()
		{
			missionData = new ConditionalWeakTable<Mission, MissionData>();
			dailyOffers.Clear();
			claimedOffers.Clear();
			dailyOfferDay = int.MinValue;
		}

		private void LoadClaims(LetterMissionSaveKeys sourceKeys)
		{
			if (!GameState.modData.TryGetValue(
					sourceKeys.ClaimedOffers,
					out string claimData))
			{
				return;
			}

			string currentDayPrefix = GameState.day + ",";
			foreach (string claim in claimData.Split(
				new[] { ';' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				if (!claim.StartsWith(currentDayPrefix, StringComparison.Ordinal))
				{
					continue;
				}

				string missionId = claim.Substring(currentDayPrefix.Length);
				if (definitionsById.ContainsKey(missionId))
				{
					claimedOffers.Add(claim);
				}
			}
		}

		private void LoadActiveMissions(LetterMissionSaveKeys sourceKeys)
		{
			if (PlayerMissions.missions == null ||
				!GameState.modData.TryGetValue(
					sourceKeys.ActiveMissions,
					out string activeData))
			{
				return;
			}

			foreach (string entry in activeData.Split(
				new[] { ';' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				string[] fields = entry.Split(',');
				if (fields.Length != 4 ||
					!int.TryParse(fields[0], out int slot) ||
					!int.TryParse(fields[1], out int originIndex) ||
					!int.TryParse(fields[2], out int destinationIndex) ||
					slot < 0 ||
					slot >= PlayerMissions.missions.Length ||
					!definitionsById.TryGetValue(fields[3], out TDefinition definition))
				{
					continue;
				}

				Mission mission = PlayerMissions.missions[slot];
				if (IsLetterMission(mission) &&
					mission.missionIndex == slot &&
					mission.originPort != null &&
					mission.destinationPort != null &&
					mission.originPort.portIndex == originIndex &&
					mission.destinationPort.portIndex == destinationIndex &&
					definition.DestinationPortIndex == destinationIndex)
				{
					Attach(mission, definition);
					claimedOffers.Add(
						GetClaimKey(GameState.day, definition.Id));
				}
			}
		}

		private void EnsureDailyOffers()
		{
			if (dailyOfferDay == GameState.day)
			{
				return;
			}

			if (Port.ports == null || Letter.Prefab == null || Sun.sun == null)
			{
				return;
			}

			dailyOffers.Clear();
			dailyOfferDay = GameState.day;
			PruneClaimsForCurrentDay();

			foreach (TDefinition definition in definitions)
			{
				if (IsClaimed(definition.Id) || HasActiveMission(definition.Id))
				{
					continue;
				}

				Port destination = GetPort(definition.DestinationPortIndex);
				if (destination == null)
				{
					continue;
				}

				var eligibleOrigins = new List<Port>();
				foreach (int portIndex in definition.SpawnPortIndices)
				{
					Port origin = GetPort(portIndex);
					if (CanOfferFrom(origin, destination))
					{
						eligibleOrigins.Add(origin);
					}
				}

				if (eligibleOrigins.Count == 0)
				{
					continue;
				}

				ValidatePortName(definition, destination);
				var random = new Random(GetSelectionSeed(definition.Id));
				Port selectedOrigin =
					eligibleOrigins[random.Next(eligibleOrigins.Count)];
				dailyOffers[definition.Id] = CreateMission(
					selectedOrigin,
					destination,
					definition);
			}
		}

		private bool CanOfferFrom(Port origin, Port destination)
		{
			if (origin == null || destination == null ||
				PlayerReputation.GetRepLevel(origin.region) < requiredReputation)
			{
				return false;
			}

			float distance = Mission.GetDistance(origin, destination);
			return PostalMail.IsWithinVanillaReputationRange(origin, distance);
		}

		private Mission CreateMission(
			Port origin,
			Port destination,
			TDefinition definition)
		{
			Good letterGood = Letter.Prefab.GetComponent<Good>();
			float missionDistance = LetterMissionRoute.GetMissionDistance(
				origin,
				destination,
				definition);
			int dueDay = definition.UseDeliveryLocationForRoute
				? MailDueDateCalculator.CalculateFromMissionDistance(
					missionDistance,
					letterGood,
					dueDayBaseSpeed)
				: MailDueDateCalculator.Calculate(
					origin,
					destination,
					letterGood,
					dueDayBaseSpeed);

			var mission = new Mission(
				origin,
				destination,
				Letter.Prefab,
				1,
				fixedGoldReward,
				1f,
				0,
				dueDay);
			mission.missionName = getMissionName(destination);
			if (definition.UseDeliveryLocationForRoute)
			{
				mission.distance = missionDistance;
			}
			mission.pricePerKm = 0f;
			Attach(mission, definition);
			return mission;
		}

		private void Attach(Mission mission, TDefinition definition)
		{
			if (mission == null ||
				definition == null ||
				TryGetDefinition(mission, out _))
			{
				return;
			}

			missionData.Add(mission, new MissionData(definition));
			ApplyDeliveryRoute(mission, definition);
			mission.totalPrice = fixedGoldReward;
			mission.pricePerKm = 0f;
			mission.missionName = getMissionName(mission.destinationPort);
		}

		private void ApplyDeliveryRoute(Mission mission, TDefinition definition)
		{
			if (!definition.UseDeliveryLocationForRoute)
			{
				return;
			}

			float portDistance = Mission.GetDistance(
				mission.originPort,
				mission.destinationPort);
			float routeDistance = LetterMissionRoute.GetMissionDistance(
				mission.originPort,
				mission.destinationPort,
				definition);
			float oldRouteDifference = Mathf.Abs(mission.distance - portDistance);
			float newRouteDifference = Mathf.Abs(mission.distance - routeDistance);
			if (oldRouteDifference < newRouteDifference)
			{
				Good letterGood = mission.goodPrefab.GetComponent<Good>();
				int oldDueDay = MailDueDateCalculator.CalculateFromMissionDistance(
					portDistance,
					letterGood,
					dueDayBaseSpeed);
				int newDueDay = MailDueDateCalculator.CalculateFromMissionDistance(
					routeDistance,
					letterGood,
					dueDayBaseSpeed);
				mission.dueDay += newDueDay - oldDueDay;
			}

			mission.distance = routeDistance;
		}

		private bool HasActiveMission(string missionId)
		{
			return TryGetActiveMission(missionId, out _);
		}

		private bool TryGetActiveMission(
			string missionId,
			out Mission activeMission)
		{
			activeMission = null;
			if (PlayerMissions.missions == null)
			{
				return false;
			}

			foreach (Mission mission in PlayerMissions.missions)
			{
				if (TryGetDefinition(mission, out TDefinition definition) &&
					string.Equals(definition.Id, missionId, StringComparison.Ordinal))
				{
					activeMission = mission;
					return true;
				}
			}

			return false;
		}

		private static string GetPortName(Port port)
		{
			return port != null ? port.GetPortName() : "unknown port";
		}

		private static bool IsLetterMission(Mission mission)
		{
			if (mission == null || mission.goodPrefab == null)
			{
				return false;
			}

			SaveablePrefab saveable =
				mission.goodPrefab.GetComponent<SaveablePrefab>();
			return PostalMail.IsRegisteredLetter(saveable);
		}

		private static Port GetPort(int portIndex)
		{
			return Port.ports != null &&
				portIndex >= 0 &&
				portIndex < Port.ports.Length
					? Port.ports[portIndex]
					: null;
		}

		private void ValidatePortName(TDefinition definition, Port destination)
		{
			if (!string.Equals(
					definition.DestinationPortName,
					destination.GetPortName(),
					StringComparison.Ordinal))
			{
				Debug.LogWarning(
					"Postal Expansion: " + categoryName + " mission " +
					definition.Id + " expects port " +
					definition.DestinationPortName + " but index " +
					definition.DestinationPortIndex + " is " +
					destination.GetPortName() + ".");
			}
		}

		private static int GetSelectionSeed(string missionId)
		{
			unchecked
			{
				int hash = 17;
				foreach (char character in missionId)
				{
					hash = hash * 31 + character;
				}

				return (GameState.day + 761) * 31 + hash;
			}
		}

		private bool IsClaimed(string missionId)
		{
			return claimedOffers.Contains(GetClaimKey(GameState.day, missionId));
		}

		private static string GetClaimKey(int day, string missionId)
		{
			return day + "," + missionId;
		}

		private void PruneClaimsForCurrentDay()
		{
			string currentDayPrefix = GameState.day + ",";
			claimedOffers.RemoveWhere(
				claim => !claim.StartsWith(
					currentDayPrefix,
					StringComparison.Ordinal));
		}

		private sealed class MissionData
		{
			internal MissionData(TDefinition definition)
			{
				Definition = definition;
			}

			internal TDefinition Definition { get; }
		}
	}
}

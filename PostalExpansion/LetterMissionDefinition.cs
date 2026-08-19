using UnityEngine;

namespace PostalExpansion
{
	internal abstract class LetterMissionDefinition
	{
		protected LetterMissionDefinition(
			string id,
			string destinationPortName,
			int destinationPortIndex,
			Vector3 localPosition,
			Vector3 localEulerAngles,
			Vector3 triggerSize,
			LetterDeliveryWindow deliveryWindow,
			int[] spawnPortIndices,
			string locationDescription)
		{
			Id = id;
			DestinationPortName = destinationPortName;
			DestinationPortIndex = destinationPortIndex;
			LocalPosition = localPosition;
			LocalEulerAngles = localEulerAngles;
			TriggerSize = triggerSize;
			DeliveryWindow = deliveryWindow;
			SpawnPortIndices = spawnPortIndices;
			LocationDescription = locationDescription;
		}

		internal string Id { get; }
		internal string DestinationPortName { get; }
		internal int DestinationPortIndex { get; }
		internal Vector3 LocalPosition { get; }
		internal Vector3 LocalEulerAngles { get; }
		internal Vector3 TriggerSize { get; }
		internal LetterDeliveryWindow DeliveryWindow { get; }
		internal int[] SpawnPortIndices { get; }
		internal string LocationDescription { get; }
		internal virtual float DistanceToOffice =>
			new Vector2(LocalPosition.x, LocalPosition.z).magnitude;
		internal virtual bool UsePersistentWorldAnchor => false;
		internal virtual bool UseDeliveryLocationForRoute => false;
		internal virtual bool ShowDeliveryCoordinates => false;
		internal virtual bool HideDestination => false;

		internal abstract string GetDeliveryDialogue(Mission mission);

		protected static int GetOriginPortIndex(Mission mission)
		{
			return mission?.originPort?.portIndex ?? -1;
		}

		protected string UnexpectedOrigin(Mission mission)
		{
			Port origin = mission?.originPort;
			string originDescription = origin != null
				? origin.GetPortName() + " (" + origin.portIndex + ")"
				: "missing origin";

			Debug.LogWarning(
				"Postal Expansion: letter mission " + Id +
				" received unexpected origin " + originDescription + ".");
			return string.Empty;
		}
	}
}

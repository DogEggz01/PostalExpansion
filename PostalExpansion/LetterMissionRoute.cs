namespace PostalExpansion
{
	internal static class LetterMissionRoute
	{
		internal static float GetMissionDistance(
			Port origin,
			Port destination,
			LetterMissionDefinition definition)
		{
			float portDistance = Mission.GetDistance(origin, destination);
			if (!definition.UseDeliveryLocationForRoute ||
				destination == null ||
				destination.portIndex != definition.DestinationPortIndex)
			{
				return portDistance;
			}

			return portDistance + definition.DistanceToOffice / 100f;
		}
	}
}

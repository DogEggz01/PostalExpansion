using System;
using BepInEx.Configuration;

namespace PostalExpansion
{
	internal sealed class ConfigurationManagerAttributes
	{
		public Action<ConfigEntryBase> CustomDrawer;

		public object DefaultValue;
	}
}

# Postal Expansion 1.1.1

Postal Expansion is a BepInEx mod for Sailwind that adds a dedicated Mail tab to the port mission book and expands vanilla mail deliveries.

## Current behavior

- Adds a third `mail` tab beside the vanilla `local` and `world` tabs.
- Keeps mail missions out of the Local and World lists.
- Shows regular mail, Registered Letter, Anonymous Letter, Express Mail, and Golden Delivery missions together in the Mail list.
- Reuses the vanilla mission details, map, acceptance, cargo spawning, and mission completion systems.
- Adds Express Mail as good `69` / prefab `239`, with reputation-gated deterministic Local and World offers.
- Adds one Urgent Express offer from World Express missions per eligible origin/day at reputation level 6 or above.
- Adds one deterministic Golden Delivery offer per eligible origin/day at reputation level 9, with a 50% daily spawn chance and destinations farther than 288 mission-distance units.
- Golden Delivery uses the Express Mail prefab, spawns one item, pays ten times the comparable regular-mail reward, and awards fifteen times regular-mail reputation when on time.
- Golden Delivery pays no money or reputation one day late and applies `-50000` reputation two or more days late.
- Golden Delivery deadlines use base speed `4.0f`, equivalent to `21 km/h` (`11.34 knots`) with the Express Mail prefab's speed tier.
- Persists accepted Urgent status and offer claims through `GameState.modData`.
- Persists Golden Delivery status, daily claims, and distinct Golden, Urgent, and Anonymous mission-history names through `GameState.modData`.
- Defines each Registered and Anonymous Letter mission in its own class while sharing generation, persistence, UI, and delivery infrastructure.
- Includes 34 independently generated Registered Letter missions across Gold Rock City, Neverdin, Albacore Town, Alchemist's Island, Al'Ankh Academy, Dragon Cliffs, Sanctuary, Crab Beach, Sage Hills, Fort Aestrin, Sunspire, Happy Bay, Oasis, Siren Song, Serpent Isle, Mount Malefic, Chronos, Fire Fish Town, Sen'na, On'na, Firefly Grotto, Aestra Abbey, Fey Valley, Turtle Island, Old Ankh Town, Mirage Mountain, and Kicia Bay.
- Includes nine independently generated Anonymous Letter missions at Oasis, Happy Bay, Eastwind, Saffron Island, Al'Nilem, Old Ankh Town, Aestra Abbey, Turtle Island, and Dead Cove.
- Offers the Palace Gate mission from Fort Aestrin, Al'Ankh Academy, Oasis, Mirage Mountain, Dragon Cliffs, Kicia Bay, Happy Bay, or Chronos once the origin region reaches reputation level 7 and the destination is within that level's vanilla distance limit.
- Selects each Registered Letter's origin only from its configured spawn ports that also satisfy vanilla reputation-distance limits.
- Keeps each Registered Letter mission globally unique each day and persists accepted-mission metadata and daily claims in `GameState.modData`.
- Pressing `F4` cycles through five Registered or Anonymous Letter spawn statuses at a time while logging the complete list.
- Registered Letter missions spawn one 1-pound letter, pay one fixed Gold Lion through the normal mission reward system, and award eight times regular-mail reputation when on time.
- Anonymous Letter missions unlock at reputation level 8, show no destination or map, display delivery coordinates, pay five Gold Lions when on time, and award ten times regular-mail reputation when on time.
- Anonymous Letter items show only their due day when pointed at; Registered, regular, and Express Mail retain their existing hover details.
- Registered Letter missions award no reputation one day late and apply `-5000` reputation two or more days late.
- Anonymous Letter missions pay one Gold Lion and award no reputation one day late; two or more days late pays no gold and applies `-8000` reputation.
- Uses Express Mail deadline logic with base speed `3.0f`; the effective speed is `15.75 km/h` (`8.50 knots`).
- Accepts day deliveries from 07:00 through 18:00 and night deliveries from 19:00 through 05:00 local game time, supports mission-specific Anonymous Letter delivery windows, and blocks letter delivery at the normal port office.
- Shows each Registered Letter mission's delivery hours between the Due and Distance rows in mission details.
- Shows each configured delivery location in mission details and displays its origin-specific dialogue near head height until the player exits the separate dialogue area.
- Caches a persistent copy of the vanilla rumor dialogue UI so remote delivery locations do not depend on a nearby active tavern NPC.
- Categorizes Pirate Hideout and eight additional remote deliveries as Anonymous Letter missions.
- Keeps every Anonymous Letter delivery trigger active beyond its reference port's normal island-loading distance and aligned with floating-origin and horizon-height adjustments.
- Uses port-to-port distance plus the final office-to-location leg for every Anonymous Letter deadline, distance, and reputation reward.
- Wraps letter dialogue at vanilla-style word boundaries with a 40-character limit, then sizes the bubble horizontally while preserving its original height.
- Uses matte, lit alpha-cutout materials for the Express Mail stamp and Registered Letter seal.
- Prevents NANDTweaks from adding its mission-goods decal to Registered Letters while leaving its regular and Express Mail decals unchanged.
- Allows mission-spawned Registered Letters to be stored in player inventory slots and empty crates.

The Registered Letter prefab intentionally does not include `QuestItem`; route identity is stored separately by the mod.

## Build

The project targets .NET Standard 2.0 and uses the Sailwind assemblies in the adjacent workspace game folder by default:

```powershell
dotnet build .\PostalExpansion.csproj -c Release
```

The project has no external package dependencies. If a restricted environment cannot read the user-level NuGet configuration, restore from the existing local package cache and then build without restoring:

```powershell
dotnet restore .\PostalExpansion.csproj --source "$env:USERPROFILE\.nuget\packages"
dotnet build .\PostalExpansion.csproj -c Release --no-restore
```

To use another Sailwind installation:

```powershell
dotnet build .\PostalExpansion.csproj -c Release -p:GameDir='C:\path\to\Sailwind\'
```

The normal build does not overwrite the installed mod. To build and copy the DLL, PDB, and seal asset into the configured BepInEx plugin folder:

```powershell
dotnet build .\PostalExpansion.csproj -c Release -p:InstallToSailwind=true
```

Release output is written to `bin\Release\netstandard2.0\`.

## Validation

The project is validated by a clean Release compile. No automated Unity scene tests are included.

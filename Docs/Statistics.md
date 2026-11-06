# Binary match statistics

The RA and Dune 2000 parsers share the Westwood `stats.dmp` packet reader in
`MatchParserBase`. Packet lengths and field lengths determine how much data is
read. Neither the reader nor the object counter decoder assumes a fixed number
of infantry, vehicles, aircraft, buildings, or vessels.

RA object counter slots are associated with the ordered values in the game's
rules type lists:

| Packet category | Rules section |
| --- | --- |
| `IN` | `InfantryTypes` |
| `UN` | `VehicleTypes`, or `UnitTypes` when `VehicleTypes` is absent |
| `PL` | `AircraftTypes` |
| `BL` | `BuildingTypes` |
| `VS` | `VesselTypes` |

These prefixes are identifiers in the game statistics protocol. Individual
object IDs and the lengths of the type lists are loaded from INI files.
The suffixes `B`, `L`, and `K` select built, remaining, and killed counters.
Each category uses the same decoding code.

The parser reads `rules.ini` from the game directory, falling back to
`INI/rules.ini`. File names are matched without case sensitivity, including the
`RULES.INI` spelling used by the RA client package. Normal `[INISystem] BasedOn`
inheritance is supported by the INI reader. Type-list overrides are then read
from the scenario named by `[Settings] Scenario` in `spawn.ini`, or from
`spawnmap.ini` when no scenario is specified. Existing list keys retain their
positions when overridden; new keys are appended.

For example:

```ini
[VehicleTypes]
42=MOD_MCV
7=MOD_TANK
```

Counter slot 0 is associated with `MOD_MCV`, and slot 1 with `MOD_TANK`.
The numeric key names are not used as array indices. Reordering or extending
the list changes the association without requiring client source changes.
The effective list must describe the engine's statistics array order; these
metadata associations do not change the engine's own object indices.

Classic RA rules, including the files in `cncnet-ra-client-package` PR #67, do
not contain `*Types` registries. There is no reliable way to reconstruct engine
array order from the order of individual `[MCV]`, `[E1]`, or other rule sections.
For these files, missing registries, blank or duplicate type IDs, and array
positions beyond the available list, the parser retains an anonymous indexed
counter. No counters are discarded or combined. Missing or unreadable rules
metadata does not prevent the packet's totals and match outcome from being read.

Rules list lengths do not replace packet lengths. A shorter or longer type
list is valid; a partial integer or mismatched built/remaining arrays is still
a malformed statistics packet. Results are applied only after validation.

Per-type counter names are used internally to aggregate the existing
`PlayerStatistics` fields. The statistics database format and UI remain
compatible with existing records. Game-specific outcome flags and Dune 2000's
`SCR` summaries remain in their respective parsers.

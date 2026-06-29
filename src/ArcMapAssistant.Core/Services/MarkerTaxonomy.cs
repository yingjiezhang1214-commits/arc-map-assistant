using ArcMapAssistant.Core.Models;

namespace ArcMapAssistant.Core.Services;

public static class MarkerTaxonomy
{
    public static readonly IReadOnlyDictionary<string, string> Groups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["containers"] = "Containers",
        ["electrical_tech"] = "Electrical & Tech",
        ["arc_enemies"] = "ARC Enemies",
        ["plants_resources"] = "Plants & Resources",
        ["movement"] = "Movement",
        ["extracts"] = "Extracts",
        ["spawns_locations"] = "Spawns & Locations",
        ["special_events"] = "Special & Events"
    };

    public static readonly IReadOnlyList<MarkerTypeDefinition> Types =
    [
        new() { Group = "containers", Type = "field_crate", DisplayName = "Field Crate" },
        new() { Group = "containers", Type = "field_depot", DisplayName = "Field Depot" },
        new() { Group = "containers", Type = "metal_crate", DisplayName = "Metal Crate" },
        new() { Group = "containers", Type = "weapon_case", DisplayName = "Weapon Case" },
        new() { Group = "containers", Type = "ammo_case", DisplayName = "Ammo Case" },
        new() { Group = "containers", Type = "grenade_case", DisplayName = "Grenade Case" },
        new() { Group = "containers", Type = "medical_bag", DisplayName = "Medical Bag" },
        new() { Group = "containers", Type = "raider_cache", DisplayName = "Raider Cache" },
        new() { Group = "containers", Type = "first_wave_cache", DisplayName = "First Wave Cache" },
        new() { Group = "containers", Type = "drawers", DisplayName = "Drawers" },
        new() { Group = "containers", Type = "cupboard", DisplayName = "Cupboard" },
        new() { Group = "containers", Type = "fridge", DisplayName = "Fridge" },
        new() { Group = "containers", Type = "lockers", DisplayName = "Lockers" },
        new() { Group = "containers", Type = "security_locker", DisplayName = "Security Locker" },
        new() { Group = "containers", Type = "suitcase", DisplayName = "Suitcase" },
        new() { Group = "containers", Type = "toolbox", DisplayName = "Toolbox" },
        new() { Group = "containers", Type = "trailer", DisplayName = "Trailer" },
        new() { Group = "containers", Type = "car", DisplayName = "Car" },
        new() { Group = "containers", Type = "bus", DisplayName = "Bus" },
        new() { Group = "containers", Type = "garbage_bins", DisplayName = "Garbage Bins" },
        new() { Group = "containers", Type = "shipping_container", DisplayName = "Shipping Container" },
        new() { Group = "containers", Type = "electrical_box", DisplayName = "Electrical Box" },
        new() { Group = "containers", Type = "generator", DisplayName = "Generator" },
        new() { Group = "containers", Type = "wicker_basket", DisplayName = "Wicker Basket" },
        new() { Group = "containers", Type = "arc_husk", DisplayName = "ARC Husk" },
        new() { Group = "containers", Type = "backpack", DisplayName = "Backpack" },
        new() { Group = "containers", Type = "nest", DisplayName = "Nest" },
        new() { Group = "containers", Type = "combat_supplies", DisplayName = "Combat Supplies" },

        new() { Group = "electrical_tech", Type = "search", DisplayName = "Search" },
        new() { Group = "electrical_tech", Type = "camera", DisplayName = "Camera" },
        new() { Group = "electrical_tech", Type = "metal_detector", DisplayName = "Metal Detector" },
        new() { Group = "electrical_tech", Type = "supply_call_station", DisplayName = "Supply Call Station" },

        new() { Group = "arc_enemies", Type = "arc_baron_husk", DisplayName = "ARC Baron Husk" },
        new() { Group = "arc_enemies", Type = "arc_rocketeer_husk", DisplayName = "ARC Rocketeer Husk" },
        new() { Group = "arc_enemies", Type = "arc_wasp_husk", DisplayName = "ARC Wasp Husk" },
        new() { Group = "arc_enemies", Type = "arc_courier", DisplayName = "ARC Courier" },
        new() { Group = "arc_enemies", Type = "arc_probe", DisplayName = "ARC Probe" },
        new() { Group = "arc_enemies", Type = "turret", DisplayName = "Turret" },
        new() { Group = "arc_enemies", Type = "arc_wasp", DisplayName = "ARC Wasp" },
        new() { Group = "arc_enemies", Type = "arc_rocketeer", DisplayName = "ARC Rocketeer" },
        new() { Group = "arc_enemies", Type = "arc_bombardier", DisplayName = "ARC Bombardier" },
        new() { Group = "arc_enemies", Type = "arc_snitch", DisplayName = "ARC Snitch" },
        new() { Group = "arc_enemies", Type = "arc_pop", DisplayName = "ARC Pop" },
        new() { Group = "arc_enemies", Type = "arc_sentinel", DisplayName = "ARC Sentinel" },
        new() { Group = "arc_enemies", Type = "arc_bastion", DisplayName = "ARC Bastion" },
        new() { Group = "arc_enemies", Type = "arc_surveyor", DisplayName = "ARC Surveyor" },
        new() { Group = "arc_enemies", Type = "arc_fireball", DisplayName = "ARC Fireball" },
        new() { Group = "arc_enemies", Type = "arc_leaper", DisplayName = "ARC Leaper" },
        new() { Group = "arc_enemies", Type = "arc_tick", DisplayName = "ARC Tick" },
        new() { Group = "arc_enemies", Type = "arc_assesor", DisplayName = "ARC Assesor" },
        new() { Group = "arc_enemies", Type = "arc_comet", DisplayName = "ARC Comet" },
        new() { Group = "arc_enemies", Type = "arc_firefly", DisplayName = "ARC Firefly" },
        new() { Group = "arc_enemies", Type = "arc_vaporizer", DisplayName = "ARC Vaporizer" },
        new() { Group = "arc_enemies", Type = "arc_shredder", DisplayName = "ARC Shredder" },
        new() { Group = "arc_enemies", Type = "arc_turbine", DisplayName = "ARC Turbine" },

        new() { Group = "plants_resources", Type = "agave", DisplayName = "Agave" },
        new() { Group = "plants_resources", Type = "apricot", DisplayName = "Apricot" },
        new() { Group = "plants_resources", Type = "prickly_pear", DisplayName = "Prickly Pear" },
        new() { Group = "plants_resources", Type = "great_mullein", DisplayName = "Great Mullein" },
        new() { Group = "plants_resources", Type = "lemon", DisplayName = "Lemon" },
        new() { Group = "plants_resources", Type = "candleberry", DisplayName = "Candleberry" },

        new() { Group = "movement", Type = "ladder", DisplayName = "Ladder" },
        new() { Group = "movement", Type = "zipline", DisplayName = "Zipline" },
        new() { Group = "movement", Type = "scissor_lift", DisplayName = "Scissor Lift" },
        new() { Group = "movement", Type = "roof_hatch", DisplayName = "Roof Hatch" },
        new() { Group = "movement", Type = "drawbridge", DisplayName = "Drawbridge" },
        new() { Group = "movement", Type = "shutters", DisplayName = "Shutters" },

        new() { Group = "extracts", Type = "hatch_extract", DisplayName = "Hatch Extract" },
        new() { Group = "extracts", Type = "metro_extract", DisplayName = "Metro Extract" },
        new() { Group = "extracts", Type = "metro_entrance", DisplayName = "Metro Entrance" },

        new() { Group = "spawns_locations", Type = "raider_spawn", DisplayName = "Raider Spawn" },

        new() { Group = "special_events", Type = "loose_loot", DisplayName = "Loose Loot" },
        new() { Group = "special_events", Type = "key", DisplayName = "Key" },
        new() { Group = "special_events", Type = "info", DisplayName = "Info" }
    ];

    public static IReadOnlyList<string> DefaultEnabledGroups => Groups.Keys.ToList();
    public static IReadOnlyList<string> DefaultEnabledTypes => Types.Select(type => type.Type).ToList();

    public static MarkerTypeDefinition? FindType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        var mappedType = MapLegacyType(type);
        return Types.FirstOrDefault(definition => string.Equals(definition.Type, mappedType, StringComparison.OrdinalIgnoreCase));
    }

    public static MarkerTypeDefinition GetDefaultType()
    {
        return Types.First(definition => string.Equals(definition.Type, "info", StringComparison.OrdinalIgnoreCase));
    }

    public static MarkerTypeDefinition NormalizeDebugSelection(string? group, string? type)
    {
        var knownType = FindType(type);
        if (knownType is not null)
        {
            return knownType;
        }

        if (!string.IsNullOrWhiteSpace(group))
        {
            var groupDefault = Types.FirstOrDefault(definition => string.Equals(definition.Group, group, StringComparison.OrdinalIgnoreCase));
            if (groupDefault is not null)
            {
                return groupDefault;
            }
        }

        return GetDefaultType();
    }

    public static MapPoint NormalizePoint(MapPoint point)
    {
        if (string.IsNullOrWhiteSpace(point.Group))
        {
            ApplyLegacyType(point);
        }

        var knownType = Types.FirstOrDefault(type => string.Equals(type.Type, point.Type, StringComparison.OrdinalIgnoreCase));
        if (knownType is not null)
        {
            point.Group = knownType.Group;
            if (string.IsNullOrWhiteSpace(point.DisplayName))
            {
                point.DisplayName = knownType.DisplayName;
            }
        }

        if (string.IsNullOrWhiteSpace(point.DisplayName))
        {
            point.DisplayName = ToDisplayName(point.Type);
        }

        return point;
    }

    public static IReadOnlyList<string> NormalizeEnabledTypes(IEnumerable<string>? enabledTypes)
    {
        var values = enabledTypes?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        if (values.Count == 0)
        {
            return DefaultEnabledTypes;
        }

        return values.Select(MapLegacyType).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void ApplyLegacyType(MapPoint point)
    {
        var mappedType = MapLegacyType(point.Type);
        point.Type = mappedType;

        var knownType = Types.FirstOrDefault(type => string.Equals(type.Type, mappedType, StringComparison.OrdinalIgnoreCase));
        if (knownType is not null)
        {
            point.Group = knownType.Group;
            point.DisplayName = knownType.DisplayName;
            return;
        }

        point.Group = "special_events";
    }

    private static string MapLegacyType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "high_value_loot" => "loose_loot",
            "quest" => "info",
            "extraction" => "hatch_extract",
            "keycard" => "key",
            "note" => "info",
            _ => type
        };
    }

    private static string ToDisplayName(string type)
    {
        return string.Join(' ', type.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}

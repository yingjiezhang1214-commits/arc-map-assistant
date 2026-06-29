namespace ArcMapAssistant.Core.Models;

public sealed class AppSettings
{
    public int ScreenWidth { get; set; } = 2560;
    public int ScreenHeight { get; set; } = 1440;
    public double OverlayOpacity { get; set; } = 0.75;
    public double MarkerSize { get; set; } = 18;
    public string HotkeyToggleOverlay { get; set; } = "F7";
    public string HotkeyReloadConfig { get; set; } = "F9";
    public string HotkeyRecordPoint { get; set; } = "F10";
    public string HotkeyCaptureScreenshot { get; set; } = "F11";
    public string HotkeyUndoDebugPoint { get; set; } = "F12";
    public string DebugPointGroup { get; set; } = "special_events";
    public string DebugPointType { get; set; } = "info";
    public string DebugPointDisplayName { get; set; } = "Info";
    public string CurrentMapId { get; set; } = "buried_ruins";
    public string PointCoordinateMode { get; set; } = "base_map";
    public bool ShowLabels { get; set; } = true;
    public bool EnableDebugMode { get; set; } = true;
    public bool SaveDebugScreenshots { get; set; }
    public int DetectionIntervalMs { get; set; } = 1000;
    public List<string> EnabledGroups { get; set; } = new()
    {
        "containers",
        "electrical_tech",
        "arc_enemies",
        "plants_resources",
        "movement",
        "extracts",
        "spawns_locations",
        "special_events"
    };
    public List<string> EnabledTypes { get; set; } = new()
    {
        "field_crate",
        "field_depot",
        "metal_crate",
        "weapon_case",
        "ammo_case",
        "grenade_case",
        "medical_bag",
        "raider_cache",
        "first_wave_cache",
        "drawers",
        "cupboard",
        "fridge",
        "lockers",
        "security_locker",
        "suitcase",
        "toolbox",
        "trailer",
        "car",
        "bus",
        "garbage_bins",
        "shipping_container",
        "electrical_box",
        "generator",
        "wicker_basket",
        "arc_husk",
        "backpack",
        "nest",
        "combat_supplies",
        "search",
        "camera",
        "metal_detector",
        "supply_call_station",
        "arc_baron_husk",
        "arc_rocketeer_husk",
        "arc_wasp_husk",
        "arc_courier",
        "arc_probe",
        "turret",
        "arc_wasp",
        "arc_rocketeer",
        "arc_bombardier",
        "arc_snitch",
        "arc_pop",
        "arc_sentinel",
        "arc_bastion",
        "arc_surveyor",
        "arc_fireball",
        "arc_leaper",
        "arc_tick",
        "arc_assesor",
        "arc_comet",
        "arc_firefly",
        "arc_vaporizer",
        "arc_shredder",
        "arc_turbine",
        "agave",
        "apricot",
        "prickly_pear",
        "great_mullein",
        "lemon",
        "candleberry",
        "ladder",
        "zipline",
        "scissor_lift",
        "roof_hatch",
        "drawbridge",
        "shutters",
        "hatch_extract",
        "metro_extract",
        "metro_entrance",
        "raider_spawn",
        "loose_loot",
        "key",
        "info"
    };
    public double ConfidenceThreshold { get; set; } = 0.75;
}

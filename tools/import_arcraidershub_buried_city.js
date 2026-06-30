const fs = require("fs");
const path = require("path");
const vm = require("vm");

const root = path.resolve(__dirname, "..");
const mapId = "buried_ruins";
const source = "arcraidershub";
const sourceMapId = "buried-city";
const sourceMapUrl = "https://arcraidershub.com/maps/buried-city";
const mapConfigUrl = "https://arcraidershub.com/data/maps/buried-city.json";
const markerScriptUrl = "https://arcraidershub.com/obfuscated-js/buried_city_markers-data.js";
const importedPointsPath = path.join(root, "assets", "maps", "buried_ruins", "imports", "arcraidershub_buried_city_points.json");
const importedSummaryPath = path.join(root, "assets", "maps", "buried_ruins", "imports", "arcraidershub_buried_city_summary.json");
const pointsPath = path.join(root, "config", "points.json");
const mapsPath = path.join(root, "config", "maps.json");

const typeGroups = {
  containers: [
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
    "combat_supplies"
  ],
  electrical_tech: [
    "search",
    "camera",
    "metal_detector",
    "supply_call_station"
  ],
  arc_enemies: [
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
    "arc_turbine"
  ],
  plants_resources: [
    "agave",
    "apricot",
    "prickly_pear",
    "great_mullein",
    "lemon",
    "candleberry"
  ],
  movement: [
    "ladder",
    "zipline",
    "scissor_lift",
    "roof_hatch",
    "drawbridge",
    "shutters"
  ],
  extracts: [
    "hatch_extract",
    "metro_extract",
    "metro_entrance"
  ],
  spawns_locations: [
    "raider_spawn"
  ],
  special_events: [
    "loose_loot",
    "key",
    "info"
  ]
};

const categoryOverrides = {
  sentinel: "arc_sentinel"
};

const typeToGroup = Object.entries(typeGroups).reduce((accumulator, [group, types]) => {
  for (const type of types) {
    accumulator[type] = group;
  }

  return accumulator;
}, {});

const priorityByGroup = {
  extracts: 2,
  special_events: 3,
  arc_enemies: 4,
  containers: 5,
  electrical_tech: 5,
  movement: 6,
  spawns_locations: 6,
  plants_resources: 7
};

function toLocalType(category) {
  const normalized = String(category ?? "").trim().toLowerCase().replaceAll("-", "_");
  return categoryOverrides[normalized] ?? normalized;
}

function toFallbackScreenCoordinate(mapCoordinate, mapSize, rectStart, rectSize) {
  return Number((rectStart + (mapCoordinate / mapSize) * rectSize).toFixed(3));
}

function toNumber(value) {
  return Number(Number(value).toFixed(3));
}

function buildNote(marker) {
  const parts = [
    `sourceUrl=${sourceMapUrl}`,
    `sourceMap=${sourceMapId}`,
    `sourceId=${marker.id}`,
    `sourceCategory=${marker.category}`,
    `originalCategory=${marker.originalCategory ?? ""}`,
    `level=${marker.level ?? ""}`,
    `difficulty=${marker.difficulty ?? ""}`,
    `color=${marker.color ?? ""}`
  ];

  if (marker.info) {
    parts.push(`info=${String(marker.info).replace(/\s+/g, " ").trim()}`);
  }

  return parts.join("; ");
}

async function readJson(url) {
  const response = await fetch(url, {
    headers: {
      "user-agent": "ArcMapAssistantDataImport/1.0"
    }
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch ${url}: ${response.status} ${response.statusText}`);
  }

  return response.json();
}

async function readText(url) {
  const response = await fetch(url, {
    headers: {
      "user-agent": "ArcMapAssistantDataImport/1.0"
    }
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch ${url}: ${response.status} ${response.statusText}`);
  }

  return response.text();
}

function extractMarkers(scriptText) {
  const sandbox = {
    window: {},
    self: {},
    globalThis: {},
    console: {
      log() {},
      warn() {},
      error() {}
    }
  };
  sandbox.globalThis = sandbox;
  sandbox.self = sandbox.window;

  vm.runInNewContext(scriptText, sandbox, {
    timeout: 5000,
    displayErrors: true
  });

  return sandbox.window.buried_city_markers_data
    ?? sandbox.buried_city_markers_data
    ?? sandbox.window.buried_city_markers
    ?? sandbox.buried_city_markers;
}

function updateMapsConfig(mapConfig) {
  const maps = JSON.parse(fs.readFileSync(mapsPath, "utf8"));
  const targetMap = maps.find((map) => map.mapId === mapId);
  if (!targetMap) {
    throw new Error(`Cannot find mapId '${mapId}' in ${mapsPath}`);
  }

  targetMap.displayName = "Buried City / Buried Ruins";
  targetMap.baseMapWidth = mapConfig.dimensions.width;
  targetMap.baseMapHeight = mapConfig.dimensions.height;

  fs.writeFileSync(mapsPath, `${JSON.stringify(maps, null, 2)}\n`, "utf8");
}

async function main() {
  const importedAt = new Date().toISOString();
  const mapConfig = await readJson(mapConfigUrl);
  const markerScript = await readText(markerScriptUrl);
  const markerData = extractMarkers(markerScript);
  const markers = markerData?.allMarkers;

  if (!Array.isArray(markers)) {
    throw new Error("Marker payload did not contain allMarkers[]");
  }

  const centerX = mapConfig.dimensions.width / 2;
  const centerY = mapConfig.dimensions.height / 2;
  const maps = JSON.parse(fs.readFileSync(mapsPath, "utf8"));
  const targetMap = maps.find((map) => map.mapId === mapId);
  const rect = targetMap?.fullViewScreenRect ?? targetMap?.defaultScreenMapRect ?? {
    left: 0,
    top: 0,
    width: mapConfig.dimensions.width,
    height: mapConfig.dimensions.height
  };
  const unknownTypes = new Set();

  const points = markers.map((marker) => {
    const type = toLocalType(marker.category);
    const group = typeToGroup[type];
    if (!group) {
      unknownTypes.add(`${marker.category} -> ${type}`);
    }

    const mapX = toNumber(Number(marker.x) + centerX);
    const mapY = toNumber(Number(marker.y) + centerY);
    const displayName = marker.originalCategory || marker.title || type;

    return {
      id: `${source}_${sourceMapId}_${marker.id}`.replaceAll("-", "_"),
      name: marker.title || displayName,
      group: group ?? "special_events",
      type,
      displayName,
      mapId,
      mapX,
      mapY,
      x: toFallbackScreenCoordinate(mapX, mapConfig.dimensions.width, rect.left, rect.width),
      y: toFallbackScreenCoordinate(mapY, mapConfig.dimensions.height, rect.top, rect.height),
      confidence: 1,
      priority: priorityByGroup[group] ?? 5,
      enabled: true,
      source,
      createdAt: importedAt,
      updatedAt: importedAt,
      note: buildNote(marker)
    };
  });

  if (unknownTypes.size > 0) {
    throw new Error(`Unknown marker categories:\n${Array.from(unknownTypes).join("\n")}`);
  }

  const categoryCounts = points.reduce((accumulator, point) => {
    accumulator[point.type] = (accumulator[point.type] ?? 0) + 1;
    return accumulator;
  }, {});

  const summary = {
    importedAt,
    source,
    sourceMapId,
    sourceMapUrl,
    mapConfigUrl,
    markerScriptUrl,
    mapId,
    sourceDimensions: mapConfig.dimensions,
    coordinateTransform: {
      mapX: "source x + source width / 2",
      mapY: "source y + source height / 2"
    },
    totalMarkers: points.length,
    categoryCounts
  };

  fs.writeFileSync(importedPointsPath, `${JSON.stringify(points, null, 2)}\n`, "utf8");
  fs.writeFileSync(importedSummaryPath, `${JSON.stringify(summary, null, 2)}\n`, "utf8");
  fs.writeFileSync(pointsPath, `${JSON.stringify(points, null, 2)}\n`, "utf8");
  updateMapsConfig(mapConfig);

  console.log(`Imported ${points.length} markers from ${sourceMapUrl}`);
  console.log(`Wrote ${path.relative(root, importedPointsPath)}`);
  console.log(`Wrote ${path.relative(root, importedSummaryPath)}`);
  console.log(`Updated ${path.relative(root, pointsPath)}`);
  console.log(`Updated ${path.relative(root, mapsPath)}`);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});

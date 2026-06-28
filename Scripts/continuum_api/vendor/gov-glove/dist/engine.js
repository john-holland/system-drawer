'use strict';

function clamp(v, lo, hi) {
  return Math.max(lo, Math.min(hi, v));
}

function calculateTaxImpact(params) {
  const income = Number(params.income || 50000);
  const includeLobbyists = params.includeLobbyists !== false;
  const baseRate = 0.22;
  const lobbyistRate = includeLobbyists ? baseRate * 0.9 : baseRate;
  const baseTax = income * baseRate;
  const lobbyistTax = income * lobbyistRate;
  return {
    baseTaxBurden: baseTax / income,
    lobbyistTaxBurden: lobbyistTax / income,
    baseTotalTax: baseTax,
    lobbyistTotalTax: lobbyistTax,
    lobbyistDelta: lobbyistTax - baseTax,
    serviceImpact: {
      healthcare: includeLobbyists ? -0.05 : 0,
      elderlyCare: includeLobbyists ? -0.03 : 0,
      welfare: includeLobbyists ? -0.02 : 0,
    },
  };
}

function processLobbyistImpacts(params) {
  const activity = Number(params.lobbyistActivity || 0.5);
  const stability = clamp(1 - activity * 0.15, 0.2, 1);
  return {
    lobbyistActivity: activity,
    congressStability: stability,
    taxRate: 0.07 * (1 + activity * 0.1),
    healthcareCoverage: clamp(0.85 - activity * 0.08, 0.4, 0.95),
    elderlyCareCoverage: clamp(0.85 - activity * 0.06, 0.4, 0.95),
    welfareBenefits: clamp(0.7 - activity * 0.05, 0.3, 0.9),
    stateBudgetDelta: -activity * 500000,
  };
}

function generateScenario(params) {
  const preset = params.presetId || 'default';
  const base = processLobbyistImpacts({ lobbyistActivity: 0.3 });
  if (preset === 'lobbyists_congress_on_crack') {
    return processLobbyistImpacts({ lobbyistActivity: 2.5 });
  }
  return base;
}

function computeZoning(params) {
  const citySizeSqm = Number(params.citySizeSqm || 1000000);
  const annualBudgetUsd = Number(params.annualBudgetUsd || 10000000);
  const allowDebt = Boolean(params.allowDebt);
  const commodityIndices = params.commodityIndices || {};
  const zoneDocument = params.zoneDocument || { zones: [] };
  const existingBuildings = params.existingBuildings || [];

  const commodityCost = Object.values(commodityIndices).reduce((s, v) => s + (Number(v) - 1) * 0.05, 0);
  const existingOpex = existingBuildings.reduce((s, b) => s + Number(b.opexUsd || b.opex_usd || 0), 0);
  const effectiveBudget = annualBudgetUsd - existingOpex - commodityCost * annualBudgetUsd * 0.1;

  const zones = (zoneDocument.zones || []).length
    ? zoneDocument.zones
    : [
        { id: 'residential_low', propertyClass: 'private', minAreaShare: 0.35, budgetLineShare: 0.12, maxFAR: 1.2 },
        { id: 'commercial_core', propertyClass: 'commercial', minAreaShare: 0.15, budgetLineShare: 0.22, maxFAR: 4.0 },
        { id: 'public_services', propertyClass: 'public', minAreaShare: 0.1, budgetLineShare: 0.28, maxFAR: 0.8 },
      ];

  const shareSum = zones.reduce((s, z) => s + Number(z.minAreaShare || 0.1), 0) || 1;
  const allocations = zones.map((z) => {
    const share = Number(z.minAreaShare || 0.1) / shareSum;
    const areaSqm = citySizeSqm * share;
    const budgetShareUsd = effectiveBudget * Number(z.budgetLineShare || 0.1);
    const slots = Math.max(1, Math.floor(areaSqm / 2000));
    return {
      zoneId: z.id,
      propertyClass: z.propertyClass,
      areaSqm,
      maxFAR: Number(z.maxFAR || 1),
      buildingSlots: slots,
      budgetShareUsd,
    };
  });

  const side = Math.sqrt(citySizeSqm);
  const popTier = Math.log10(Math.max(citySizeSqm, 1));
  const cityScapeProfile = {
    spatialBounds: { centerX: 0, centerZ: 0, widthM: side, depthM: side },
    sliceCount: clamp(Math.floor(16 + popTier * 8), 16, 64),
    gridResX: clamp(Math.floor(8 + popTier * 4), 8, 32),
    gridResY: clamp(Math.floor(8 + popTier * 4), 8, 32),
    gridResZ: clamp(Math.floor(8 + popTier * 4), 8, 32),
    gridResT: 32,
    zoneDensityWeights: Object.fromEntries(allocations.map((a) => [a.propertyClass, a.areaSqm / citySizeSqm])),
    plannedBuildings: allocations.flatMap((a) => {
      const types = (zones.find((z) => z.id === a.zoneId) || {}).defaultBuildingTypes || [];
      const typeId = types[0] || 'city_hall';
      return [{ zoneId: a.zoneId, buildingTypeId: typeId, count: Math.min(a.buildingSlots, 12) }];
    }),
  };

  const minBudget = allocations.reduce((s, a) => s + a.budgetShareUsd, 0) + existingOpex;

  return {
    allocations,
    maxCitySizeSqm: allowDebt ? null : citySizeSqm,
    requiredBudgetUsd: minBudget,
    effectiveBudgetUsd: effectiveBudget,
    cityScapeProfile,
    featureVector: {
      taxRate: 0.07,
      healthcareCoverage: 0.85,
      lobbyistActivity: Number(params.lobbyistActivity || 0.3),
    },
  };
}

module.exports = {
  calculateTaxImpact,
  processLobbyistImpacts,
  generateScenario,
  computeZoning,
};

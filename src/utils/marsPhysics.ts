import { StormEvaluation } from '../types/telemetry';
import { evaluateStormTrigger } from './csvTelemetry';

export interface EngineeringConfig {
  regolithCm: number; // 0 to 60 cm compacted regolith
  waterIceCm: number; // 0 to 25 cm hydrogen-rich water ice liner
  panelDustPct: number; // 0 to 80% dust accumulation on solar array
}

export interface DerivedMarsEnvironment {
  evaluation: StormEvaluation;
  opticalDepthTau: number;
  ambientIrradianceWm2: number;
  netPanelYieldWm2: number;
  rawRadiationMsvDay: number;
  shieldedRadiationMsvDay: number;
  arealShieldingGcm2: number;
  attenuationPct: number;
  scrubberPowerDrawPct: number;
  radarAxes: {
    label: string;
    value: number; // 0 - 100
    unitReadout: string;
  }[];
}

// Mars solar constant at ~1.52 AU (~590 W/m^2)
const MARS_TOA_IRRADIANCE = 590;

export function computeMarsEnvironment(
  pressurePa: number,
  flareClass: string,
  config: EngineeringConfig
): DerivedMarsEnvironment {
  const evaluation = evaluateStormTrigger(pressurePa, flareClass);
  const flare = evaluation.flareClass.toUpperCase();

  // Atmospheric dust optical depth (tau) rises sharply below the 800 Pa storm threshold
  // Clear Martian sky ~0.45 tau; regional/global dust event ~2.4 - 3.6 tau
  const pressureDeficit = Math.max(0, 820 - pressurePa);
  const opticalDepthTau = Number(
    (0.45 + (evaluation.lowPressure ? 1.85 + pressureDeficit * 0.015 : pressureDeficit * 0.004)).toFixed(2)
  );

  // Beer-Lambert solar attenuation at ~35 deg solar zenith angle (cos(35 deg) ≈ 0.819)
  const airMassFactor = 1 / 0.819;
  const ambientIrradianceWm2 = Number(
    (MARS_TOA_IRRADIANCE * Math.exp(-0.42 * opticalDepthTau * airMassFactor)).toFixed(1)
  );

  // Clean panels in a dark sky still yield less power ("Irradiance Matters")
  const panelCleanFactor = Math.max(0.05, 1 - config.panelDustPct / 100);
  const panelEfficiency = 0.28; // High-efficiency multi-junction space PV
  const netPanelYieldWm2 = Number(
    (ambientIrradianceWm2 * panelCleanFactor * panelEfficiency).toFixed(1)
  );

  // Radiation environment derived from flare class (Curiosity RAD baseline ~0.67 mSv/day)
  let rawRadiationMsvDay = 0.67;
  if (flare === 'C') rawRadiationMsvDay = 3.85;
  else if (flare === 'M') rawRadiationMsvDay = 14.2;
  else if (flare === 'X') rawRadiationMsvDay = 42.5;

  // Mass = Shielding: regolith (~1.6 g/cm^3) + water ice (~0.92 g/cm^3)
  // Water ice provides ~1.45x proton/secondary neutron stopping effectiveness per unit areal mass
  const regolithAreal = config.regolithCm * 1.6;
  const iceAreal = config.waterIceCm * 0.92;
  const arealShieldingGcm2 = Number((regolithAreal + iceAreal).toFixed(1));
  const effectiveMass = regolithAreal + iceAreal * 1.45;

  // Exponential attenuation of SEP / GCR component
  const attenuationFactor = Math.exp(-effectiveMass / 42);
  const shieldedRadiationMsvDay = Number(
    Math.max(0.18, rawRadiationMsvDay * attenuationFactor).toFixed(2)
  );
  const attenuationPct = Number(
    Math.min(99, (1 - shieldedRadiationMsvDay / rawRadiationMsvDay) * 100).toFixed(1)
  );

  // Habitat O2 scrubber & pressure compressor load increases when outside pressure falls
  const scrubberPowerDrawPct = Number(
    Math.min(95, Math.max(18, 32 + (880 - Math.min(950, pressurePa)) * 0.22)).toFixed(0)
  );

  const solarScore = Math.min(100, Math.round((netPanelYieldWm2 / 145) * 100));
  const airMarginScore = Math.max(10, Math.min(100, Math.round(100 - scrubberPowerDrawPct * 0.72)));
  const radSafetyScore = Math.max(
    8,
    Math.min(100, Math.round(100 - (shieldedRadiationMsvDay / 12) * 100))
  );
  const dustClearanceScore = Math.round(100 - config.panelDustPct);
  const massIndexScore = Math.min(100, Math.round((arealShieldingGcm2 / 119) * 100));

  return {
    evaluation,
    opticalDepthTau,
    ambientIrradianceWm2,
    netPanelYieldWm2,
    rawRadiationMsvDay,
    shieldedRadiationMsvDay,
    arealShieldingGcm2,
    attenuationPct,
    scrubberPowerDrawPct,
    radarAxes: [
      {
        label: 'SOLAR YIELD',
        value: solarScore,
        unitReadout: `${netPanelYieldWm2} W/m²`,
      },
      {
        label: 'O₂ MARGIN',
        value: airMarginScore,
        unitReadout: `${scrubberPowerDrawPct}% load`,
      },
      {
        label: 'RAD SHIELD',
        value: radSafetyScore,
        unitReadout: `${shieldedRadiationMsvDay} mSv/d`,
      },
      {
        label: 'ARRAY CLEAN',
        value: dustClearanceScore,
        unitReadout: `${100 - config.panelDustPct}%`,
      },
      {
        label: 'MASS ARMOR',
        value: massIndexScore,
        unitReadout: `${arealShieldingGcm2} g/cm²`,
      },
    ],
  };
}

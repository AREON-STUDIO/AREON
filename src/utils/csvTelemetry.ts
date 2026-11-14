import { SolEditorialBrief, StormEvaluation, TelemetryRow } from '../types/telemetry';

export const PRESSURE_THRESHOLD_PA = 800;

export function parseTelemetryCsv(rawCsv: string): TelemetryRow[] {
  const lines = rawCsv
    .trim()
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);

  if (lines.length < 2) {
    throw new Error('Telemetry CSV must include a header row and at least one Sol record.');
  }

  const headers = lines[0].split(',').map((h) => h.trim().toLowerCase());
  const dayIdx = headers.indexOf('day');
  const solIdx = headers.indexOf('sol');
  const pressureIdx = headers.indexOf('pressure_pa');
  const flareIdx = headers.indexOf('flare_class');

  if (solIdx === -1 || pressureIdx === -1 || flareIdx === -1) {
    throw new Error('CSV header missing required columns: sol, pressure_pa, flare_class.');
  }

  return lines.slice(1).map((line, index) => {
    const cols = line.split(',').map((c) => c.trim());
    const sol = Number(cols[solIdx]);
    const pressurePa = Number(cols[pressureIdx]);
    const day = dayIdx !== -1 ? Number(cols[dayIdx]) : index + 1;
    const flareClass = cols[flareIdx] || 'none';

    if (!Number.isFinite(sol) || !Number.isFinite(pressurePa)) {
      throw new Error(`Invalid numeric telemetry value on CSV row ${index + 2}.`);
    }

    return {
      day: Number.isFinite(day) ? day : index + 1,
      sol,
      pressurePa,
      flareClass,
    };
  });
}

export function evaluateStormTrigger(
  pressurePa: number,
  flareClass: string
): StormEvaluation {
  const normalizedFlare = flareClass.trim() || 'none';
  const lowPressure = pressurePa < PRESSURE_THRESHOLD_PA;
  const hasFlare = normalizedFlare.toLowerCase() !== 'none';

  return {
    pressurePa,
    flareClass: normalizedFlare,
    lowPressure,
    hasFlare,
    stormTriggered: lowPressure || hasFlare,
    pressureDeltaFromThreshold: Number((pressurePa - PRESSURE_THRESHOLD_PA).toFixed(2)),
  };
}

const KNOWN_SOL_BRIEFS: Record<number, SolEditorialBrief> = {
  800: {
    sol: 800,
    title: 'Sol 800 — The Baseline',
    summary:
      'Clear skies. Learn the mechanics: clean panels, patch the shield, and collect data.',
    challengeType: 'Nominal Baseline',
  },
  852: {
    sol: 852,
    title: 'Sol 852 — The Invisible Threat',
    summary:
      'High pressure, but a C-class flare hits. Learn the difference between dust and radiation storms.',
    challengeType: 'Solar Radiation Event',
  },
  2085: {
    sol: 2085,
    title: 'Sol 2085 — The Dark Sky',
    summary:
      'Low pressure + X-class flare. The ultimate test of resource management.',
    challengeType: 'Dual Dust & Radiation Storm',
  },
};

export function getSolBrief(row: TelemetryRow): SolEditorialBrief {
  if (KNOWN_SOL_BRIEFS[row.sol]) {
    return KNOWN_SOL_BRIEFS[row.sol];
  }
  const evaluation = evaluateStormTrigger(row.pressurePa, row.flareClass);
  return {
    sol: row.sol,
    title: `Sol ${row.sol} — Live Telemetry Scenario`,
    summary: evaluation.stormTriggered
      ? `Active storm condition detected (${row.pressurePa.toFixed(2)} Pa, Flare: ${row.flareClass}). Balance air, power, and shielding.`
      : `Clear Martian atmospheric window (${row.pressurePa.toFixed(2)} Pa, Flare: ${row.flareClass}). Nominal solar power generation.`,
    challengeType: evaluation.stormTriggered ? 'Dynamic Storm' : 'Nominal Window',
  };
}

export async function downloadOfflinePcBundle(rawCsv: string): Promise<void> {
  try {
    const res = await fetch('/game.html', { cache: 'no-store' });
    const gameHtml = res.ok ? await res.text() : '';
    const safeCsvLiteral = JSON.stringify(rawCsv);

    // Inject the fetched CSV as the offline fallback inside the standalone HTML package
    const standaloneHtml = gameHtml
      ? gameHtml.replace(
          /const FALLBACK_CSV = .*?;/,
          `const FALLBACK_CSV = ${safeCsvLiteral};`
        )
      : `<!doctype html><html><body><pre>${rawCsv}</pre></body></html>`;

    const blob = new Blob([standaloneHtml], { type: 'text/html;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'AREON-Offline-Mission-Build.html';
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  } catch {
    const blob = new Blob([rawCsv], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'data.csv';
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  }
}

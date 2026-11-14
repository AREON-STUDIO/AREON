import React, { useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  Check,
  CheckCircle2,
  Copy,
  RefreshCw,
  RotateCcw,
} from 'lucide-react';
import { TelemetryRow } from '../types/telemetry';
import {
  PRESSURE_THRESHOLD_PA,
} from '../utils/csvTelemetry';
import {
  computeMarsEnvironment,
  EngineeringConfig,
} from '../utils/marsPhysics';
import { TelemetryRadarChart } from './TelemetryRadarChart';

interface MissionControlSectionProps {
  rawCsv: string;
  rows: TelemetryRow[];
  selectedSol: number;
  customPressureInput: string;
  loading: boolean;
  error: string | null;
  lastFetchedAt: string | null;
  isCsvSandboxActive: boolean;
  onSelectSol: (sol: number) => void;
  onCustomPressureChange: (value: string) => void;
  onResetCustomPressure: () => void;
  onRefetchCsv: () => void;
  onApplyCsvSandbox: (editedCsv: string) => string | null;
}

export const MissionControlSection: React.FC<MissionControlSectionProps> = ({
  rawCsv,
  rows,
  selectedSol,
  customPressureInput,
  loading,
  error,
  lastFetchedAt,
  isCsvSandboxActive,
  onSelectSol,
  onCustomPressureChange,
  onResetCustomPressure,
  onRefetchCsv,
  onApplyCsvSandbox,
}) => {
  const [copied, setCopied] = useState(false);
  const [hoveredSol, setHoveredSol] = useState<number | null>(null);
  const [terminalTab, setTerminalTab] = useState<'inspect' | 'sandbox'>('inspect');
  const [sandboxDraft, setSandboxDraft] = useState<string>(rawCsv);
  const [sandboxError, setSandboxError] = useState<string | null>(null);

  const [engConfig, setEngConfig] = useState<EngineeringConfig>({
    regolithCm: 28,
    waterIceCm: 10,
    panelDustPct: 15,
  });

  useEffect(() => {
    setSandboxDraft(rawCsv);
  }, [rawCsv]);

  const selectedRow = useMemo(
    () => rows.find((r) => r.sol === selectedSol) ?? rows[0] ?? null,
    [rows, selectedSol]
  );

  const parsedCustom = Number(customPressureInput);
  const activePressure =
    customPressureInput.trim() !== '' && Number.isFinite(parsedCustom)
      ? parsedCustom
      : (selectedRow?.pressurePa ?? 887.38);

  const isOverridden =
    selectedRow !== null &&
    Math.abs(activePressure - selectedRow.pressurePa) > 0.001;

  const env = useMemo(
    () =>
      computeMarsEnvironment(
        activePressure,
        selectedRow?.flareClass ?? 'none',
        engConfig
      ),
    [activePressure, selectedRow, engConfig]
  );

  const csvLines = useMemo(
    () =>
      rawCsv
        .trim()
        .split(/\r?\n/)
        .filter(Boolean),
    [rawCsv]
  );

  const handleCopyCsv = async () => {
    try {
      await navigator.clipboard.writeText(rawCsv);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1800);
    } catch {
      // Clipboard API unavailable
    }
  };

  const handleApplySandbox = () => {
    const err = onApplyCsvSandbox(sandboxDraft);
    setSandboxError(err);
  };

  // Dual-Axis Pressure & Solar Irradiance SVG Geometry
  const chartW = 580;
  const chartH = 230;
  const padL = 52;
  const padR = 48;
  const padT = 26;
  const padB = 34;
  const plotW = chartW - padL - padR;
  const plotH = chartH - padT - padB;

  const minP = 650;
  const maxP = 1050;
  const yForPressure = (p: number) => {
    const clamped = Math.max(minP, Math.min(maxP, p));
    return padT + ((maxP - clamped) / (maxP - minP)) * plotH;
  };

  const minIrr = 0;
  const maxIrr = 500;
  const yForIrradiance = (wm2: number) => {
    const clamped = Math.max(minIrr, Math.min(maxIrr, wm2));
    return padT + ((maxIrr - clamped) / (maxIrr - minIrr)) * plotH;
  };

  const chartPoints = useMemo(() => {
    const count = Math.max(1, rows.length);
    return rows.map((row, idx) => {
      const effectivePressure =
        row.sol === selectedSol ? activePressure : row.pressurePa;
      const derived = computeMarsEnvironment(
        effectivePressure,
        row.flareClass,
        engConfig
      );
      const x =
        count === 1
          ? padL + plotW / 2
          : padL + (idx / (count - 1)) * plotW;
      return {
        ...row,
        effectivePressure,
        ambientIrradiance: derived.ambientIrradianceWm2,
        x,
        yP: yForPressure(effectivePressure),
        yI: yForIrradiance(derived.ambientIrradianceWm2),
      };
    });
  }, [rows, selectedSol, activePressure, engConfig, plotW]);

  const thresholdY = yForPressure(PRESSURE_THRESHOLD_PA);
  const pressurePolyline = chartPoints
    .map((pt) => `${pt.x.toFixed(1)},${pt.yP.toFixed(1)}`)
    .join(' ');
  const irradiancePolyline = chartPoints
    .map((pt) => `${pt.x.toFixed(1)},${pt.yI.toFixed(1)}`)
    .join(' ');

  const hoveredPoint =
    chartPoints.find((pt) => pt.sol === hoveredSol) ??
    chartPoints.find((pt) => pt.sol === selectedSol) ??
    chartPoints[0];

  return (
    <section
      id="mission-control"
      className="border-b border-white/[0.08] bg-[#0B0E14] py-16 md:py-24"
    >
      <div className="mx-auto max-w-[1200px] px-6">
        {/* Section Header */}
        <div className="mb-10 flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-xs font-mono uppercase tracking-wider text-[#C89B6A]">
              Live Mission Control Telemetry Workbench
            </p>
            <h2
              className="mt-1.5 font-display text-3xl font-bold tracking-tight text-[#F1F4F8] md:text-4xl"
              style={{ textWrap: 'balance' }}
            >
              The Data is the Level Design.
            </h2>
            <p className="mt-2 max-w-2xl text-sm leading-relaxed text-[#9AA4B8]">
              Every atmospheric pressure curve, solar irradiance yield, radiation dose, and storm alert is computed at runtime from{' '}
              <span className="font-mono text-[#F1F4F8]">fetch(&apos;/data.csv&apos;)</span>.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            {lastFetchedAt && (
              <span className="text-xs font-mono text-[#9AA4B8]">
                {isCsvSandboxActive ? 'Sandbox Override Active' : `Synced ${lastFetchedAt}`}
              </span>
            )}
            <button
              type="button"
              onClick={() => {
                setSandboxError(null);
                onRefetchCsv();
              }}
              disabled={loading}
              className="inline-flex items-center gap-2 rounded-lg border border-white/15 bg-[#111622] px-3.5 py-2 text-xs font-medium text-[#F1F4F8] transition-colors hover:border-[#C89B6A]/60 hover:bg-[#171D2C] cursor-pointer whitespace-nowrap"
            >
              <RefreshCw
                className={`h-3.5 w-3.5 text-[#C89B6A] ${loading ? 'animate-spin' : ''}`}
                aria-hidden="true"
              />
              <span>{isCsvSandboxActive ? 'Restore /data.csv' : 'Refetch data.csv'}</span>
            </button>
          </div>
        </div>

        {error && (
          <div
            role="alert"
            className="mb-6 rounded-lg border border-[#E06C5A]/40 bg-[#E06C5A]/10 px-4 py-3 text-sm text-[#F1F4F8]"
          >
            Telemetry read warning: {error}
          </div>
        )}

        {/* Primary Asymmetric Console: Left CSV Terminal + Right Trigger & Dual-Axis Chart */}
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-12 lg:items-stretch">
          {/* LEFT: Raw data.csv Terminal & Sandbox */}
          <div className="flex flex-col justify-between rounded-xl border border-white/[0.1] bg-[#0D111A] p-5 lg:col-span-5">
            <div>
              <div className="flex flex-wrap items-center justify-between gap-2 border-b border-white/[0.08] pb-3.5">
                <div className="flex items-center gap-1 rounded-lg border border-white/10 bg-[#07090E] p-1">
                  <button
                    type="button"
                    onClick={() => setTerminalTab('inspect')}
                    className={`rounded-md px-2.5 py-1 text-xs font-medium transition-colors cursor-pointer whitespace-nowrap ${
                      terminalTab === 'inspect'
                        ? 'bg-[#C89B6A] text-[#0B0E14] font-semibold'
                        : 'text-[#9AA4B8] hover:text-[#F1F4F8]'
                    }`}
                  >
                    Raw data.csv
                  </button>
                  <button
                    type="button"
                    onClick={() => setTerminalTab('sandbox')}
                    className={`rounded-md px-2.5 py-1 text-xs font-medium transition-colors cursor-pointer whitespace-nowrap ${
                      terminalTab === 'sandbox'
                        ? 'bg-[#C89B6A] text-[#0B0E14] font-semibold'
                        : 'text-[#9AA4B8] hover:text-[#F1F4F8]'
                    }`}
                  >
                    Live CSV Sandbox
                  </button>
                </div>

                <button
                  type="button"
                  onClick={handleCopyCsv}
                  className="inline-flex items-center gap-1.5 rounded-md border border-white/10 bg-white/[0.04] px-2.5 py-1.5 text-xs font-medium text-[#CBD5E1] transition-colors hover:border-[#C89B6A]/50 hover:text-[#F1F4F8] cursor-pointer whitespace-nowrap"
                >
                  {copied ? (
                    <>
                      <Check className="h-3.5 w-3.5 text-[#6FC3E8]" aria-hidden="true" />
                      <span>Copied</span>
                    </>
                  ) : (
                    <>
                      <Copy className="h-3.5 w-3.5" aria-hidden="true" />
                      <span>Copy</span>
                    </>
                  )}
                </button>
              </div>

              {terminalTab === 'inspect' ? (
                <div className="mt-4 overflow-x-auto rounded-lg border border-white/[0.07] bg-[#07090E] p-4 font-mono text-xs leading-6">
                  {loading && csvLines.length === 0 ? (
                    <div className="text-[#9AA4B8]">Fetching /data.csv...</div>
                  ) : (
                    csvLines.map((line, idx) => {
                      const isHeader = idx === 0;
                      const rowSol = !isHeader ? Number(line.split(',')[1]) : null;
                      const isSelectedLine = rowSol === selectedSol;

                      return (
                        <div
                          key={`${idx}-${line}`}
                          onClick={() => {
                            if (rowSol && Number.isFinite(rowSol)) {
                              onSelectSol(rowSol);
                            }
                          }}
                          className={`flex items-center gap-4 rounded px-2 py-0.5 transition-colors ${
                            isHeader
                              ? 'text-[#9AA4B8]'
                              : isSelectedLine
                                ? 'bg-[#C89B6A]/18 text-[#F1F4F8]'
                                : 'text-[#CBD5E1] hover:bg-white/[0.04] cursor-pointer'
                          }`}
                        >
                          <span className="w-5 select-none text-right text-[#64748B] tabular-nums">
                            {String(idx + 1).padStart(2, '0')}
                          </span>
                          <span className="whitespace-pre tabular-nums">{line}</span>
                        </div>
                      );
                    })
                  )}
                </div>
              ) : (
                <div className="mt-4 space-y-3">
                  <p className="text-xs leading-relaxed text-[#9AA4B8]">
                    Edit or append a Sol row below to verify that both the dashboard and playable simulation derive levels directly from CSV at runtime:
                  </p>
                  <textarea
                    value={sandboxDraft}
                    onChange={(e) => setSandboxDraft(e.target.value)}
                    rows={5}
                    aria-label="Editable CSV telemetry sandbox"
                    className="w-full rounded-lg border border-white/15 bg-[#07090E] p-3 font-mono text-xs leading-6 text-[#F1F4F8] focus:border-[#C89B6A] focus:outline-none"
                  />
                  {sandboxError && (
                    <p className="text-xs text-[#E06C5A]">{sandboxError}</p>
                  )}
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <button
                      type="button"
                      onClick={() =>
                        setSandboxDraft((prev) =>
                          prev.includes('2100')
                            ? prev
                            : `${prev.trim()}\n4,2100,745.50,M`
                        )
                      }
                      className="rounded-md border border-white/12 bg-white/[0.04] px-2.5 py-1.5 font-mono text-xs text-[#CBD5E1] hover:border-[#C89B6A]/50 cursor-pointer"
                    >
                      + Append Sol 2100
                    </button>
                    <button
                      type="button"
                      onClick={handleApplySandbox}
                      className="rounded-md bg-[#C89B6A] px-3.5 py-1.5 text-xs font-semibold text-[#0B0E14] hover:bg-[#D8AD7E] cursor-pointer"
                    >
                      Apply CSV to Portal &amp; Sim
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Derived Planetary Telemetry Strip */}
            {selectedRow && (
              <div className="mt-5 border-t border-white/[0.08] pt-4">
                <div className="text-xs font-mono uppercase tracking-wider text-[#9AA4B8]">
                  Derived Martian Physics · Sol {selectedRow.sol}
                </div>
                <div className="mt-3 grid grid-cols-2 gap-4 sm:grid-cols-4 lg:grid-cols-2">
                  <div>
                    <div className="text-[11px] text-[#9AA4B8]">Optical Depth (τ)</div>
                    <div className="mt-0.5 font-mono text-lg font-semibold text-[#F1F4F8] tabular-nums">
                      {env.opticalDepthTau.toFixed(2)}{' '}
                      <span className="text-xs font-normal text-[#9AA4B8]">tau</span>
                    </div>
                  </div>
                  <div>
                    <div className="text-[11px] text-[#9AA4B8]">Surface Irradiance</div>
                    <div className="mt-0.5 font-mono text-lg font-semibold text-[#FFD166] tabular-nums">
                      {env.ambientIrradianceWm2.toFixed(1)}{' '}
                      <span className="text-xs font-normal text-[#9AA4B8]">W/m²</span>
                    </div>
                  </div>
                  <div>
                    <div className="text-[11px] text-[#9AA4B8]">Unshielded Dose</div>
                    <div className="mt-0.5 font-mono text-lg font-semibold text-[#E06C5A] tabular-nums">
                      {env.rawRadiationMsvDay.toFixed(2)}{' '}
                      <span className="text-xs font-normal text-[#9AA4B8]">mSv/d</span>
                    </div>
                  </div>
                  <div>
                    <div className="text-[11px] text-[#9AA4B8]">Shielded Dome Dose</div>
                    <div className="mt-0.5 font-mono text-lg font-semibold text-[#6FC3E8] tabular-nums">
                      {env.shieldedRadiationMsvDay.toFixed(2)}{' '}
                      <span className="text-xs font-normal text-[#9AA4B8]">mSv/d</span>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* RIGHT: Storm Trigger Gate, Sol Selector, Custom Pressure & Dual-Axis Chart */}
          <div className="flex flex-col justify-between rounded-xl border border-white/[0.1] bg-[#111622] p-5 lg:col-span-7">
            <div>
              {/* Storm Logic Formula Banner */}
              <div className="flex flex-col gap-3 border-b border-white/[0.08] pb-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <span className="text-xs font-mono uppercase tracking-wider text-[#9AA4B8]">
                    Storm Trigger Logic
                  </span>
                  <div className="mt-1 font-mono text-sm font-semibold text-[#F1F4F8]">
                    Trigger = (<span className="text-[#6FC3E8]">Pressure &lt; 800</span>) OR (
                    <span className="text-[#E06C5A]">Flare != &quot;none&quot;</span>)
                  </div>
                </div>

                <div
                  className={`inline-flex items-center gap-2 rounded-lg border px-3.5 py-2 font-mono text-xs font-semibold whitespace-nowrap shrink-0 ${
                    env.evaluation.stormTriggered
                      ? 'border-[#E06C5A]/50 bg-[#E06C5A]/15 text-[#F8B4AA]'
                      : 'border-[#6FC3E8]/50 bg-[#6FC3E8]/12 text-[#A7E0F7]'
                  }`}
                >
                  {env.evaluation.stormTriggered ? (
                    <AlertTriangle className="h-4 w-4 text-[#E06C5A]" aria-hidden="true" />
                  ) : (
                    <CheckCircle2 className="h-4 w-4 text-[#6FC3E8]" aria-hidden="true" />
                  )}
                  <span>
                    {env.evaluation.stormTriggered
                      ? 'TRIGGER: TRUE (STORM ACTIVE)'
                      : 'TRIGGER: FALSE (CLEAR SKIES)'}
                  </span>
                </div>
              </div>

              {/* Controls Row */}
              <div className="mt-5 grid grid-cols-1 gap-4 md:grid-cols-12 md:items-end">
                <div className="md:col-span-6">
                  <label className="block text-xs font-medium text-[#CBD5E1]">
                    Select Curiosity Rover Sol
                  </label>
                  <div
                    role="group"
                    aria-label="Sol selector"
                    className="mt-2 flex flex-wrap gap-2"
                  >
                    {rows.map((row) => {
                      const active = row.sol === selectedSol;
                      return (
                        <button
                          key={row.sol}
                          type="button"
                          onClick={() => onSelectSol(row.sol)}
                          className={`min-h-[40px] rounded-lg border px-3.5 py-2 font-mono text-xs font-semibold transition-colors cursor-pointer whitespace-nowrap ${
                            active
                              ? 'border-[#C89B6A] bg-[#C89B6A] text-[#0B0E14]'
                              : 'border-white/12 bg-[#0B0E14]/70 text-[#CBD5E1] hover:border-[#C89B6A]/50 hover:text-[#F1F4F8]'
                          }`}
                        >
                          Sol {row.sol}
                        </button>
                      );
                    })}
                  </div>
                </div>

                {/* Judge-proof Custom Pressure Input */}
                <div className="md:col-span-6">
                  <div className="flex items-center justify-between">
                    <label
                      htmlFor="custom-pressure-input"
                      className="block text-xs font-medium text-[#CBD5E1]"
                    >
                      Input Custom Pressure (Pa)
                    </label>
                    {isOverridden && (
                      <button
                        type="button"
                        onClick={onResetCustomPressure}
                        className="inline-flex items-center gap-1 text-xs font-medium text-[#C89B6A] hover:underline cursor-pointer"
                      >
                        <RotateCcw className="h-3 w-3" aria-hidden="true" />
                        <span>Reset to CSV</span>
                      </button>
                    )}
                  </div>

                  <div className="mt-2 flex items-center gap-2">
                    <input
                      id="custom-pressure-input"
                      type="number"
                      step="0.01"
                      min="0"
                      max="1500"
                      value={customPressureInput}
                      onChange={(e) => onCustomPressureChange(e.target.value)}
                      placeholder="e.g. 772.22"
                      className="min-h-[40px] w-full rounded-lg border border-white/15 bg-[#07090E] px-3 py-2 font-mono text-sm text-[#F1F4F8] tabular-nums focus:border-[#C89B6A] focus:outline-none"
                    />
                    <button
                      type="button"
                      onClick={() => onCustomPressureChange('950.00')}
                      className="min-h-[40px] rounded-lg border border-white/12 bg-[#0B0E14]/70 px-3 py-2 font-mono text-xs text-[#CBD5E1] transition-colors hover:border-[#C89B6A]/60 hover:text-[#F1F4F8] cursor-pointer whitespace-nowrap shrink-0"
                    >
                      Try 950.00
                    </button>
                  </div>
                </div>
              </div>

              {/* Boolean Gate Evaluation Strip */}
              <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div className="rounded-lg border border-white/[0.08] bg-[#0B0E14]/70 px-3.5 py-2.5 font-mono text-xs">
                  <div className="text-[#9AA4B8]">Condition A: Pressure &lt; 800 Pa</div>
                  <div className="mt-1 flex items-center justify-between">
                    <span className="text-[#F1F4F8] tabular-nums">
                      {activePressure.toFixed(2)} &lt; 800.00
                    </span>
                    <span
                      className={
                        env.evaluation.lowPressure
                          ? 'font-semibold text-[#E06C5A]'
                          : 'font-semibold text-[#6FC3E8]'
                      }
                    >
                      {env.evaluation.lowPressure ? 'TRUE (Low Pressure)' : 'FALSE (Nominal)'}
                    </span>
                  </div>
                </div>

                <div className="rounded-lg border border-white/[0.08] bg-[#0B0E14]/70 px-3.5 py-2.5 font-mono text-xs">
                  <div className="text-[#9AA4B8]">Condition B: Flare != &quot;none&quot;</div>
                  <div className="mt-1 flex items-center justify-between">
                    <span className="text-[#F1F4F8]">
                      &quot;{env.evaluation.flareClass}&quot; != &quot;none&quot;
                    </span>
                    <span
                      className={
                        env.evaluation.hasFlare
                          ? 'font-semibold text-[#E06C5A]'
                          : 'font-semibold text-[#6FC3E8]'
                      }
                    >
                      {env.evaluation.hasFlare ? 'TRUE (Flare Hit)' : 'FALSE (Quiet Sun)'}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Dual-Axis Atmospheric Pressure & Solar Irradiance SVG Chart */}
            <div className="mt-5 rounded-lg border border-white/[0.08] bg-[#07090E] p-4">
              <div className="mb-2 flex flex-wrap items-center justify-between gap-2 text-xs">
                <div className="flex flex-wrap items-center gap-4 font-mono">
                  <span className="text-[#6FC3E8]">— Pressure (Pa)</span>
                  <span className="text-[#FFD166]">-- Irradiance (W/m²)</span>
                </div>
                {hoveredPoint && (
                  <span className="font-mono text-[#F1F4F8] tabular-nums">
                    Sol {hoveredPoint.sol}: {hoveredPoint.effectivePressure.toFixed(2)} Pa ·{' '}
                    {hoveredPoint.ambientIrradiance.toFixed(1)} W/m²
                  </span>
                )}
              </div>

              <svg
                viewBox={`0 0 ${chartW} ${chartH}`}
                className="w-full cursor-crosshair overflow-visible"
                role="img"
                aria-label="Dual-axis atmospheric pressure and solar irradiance chart"
              >
                <defs>
                  <linearGradient id="stormZoneGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#E06C5A" stopOpacity="0.14" />
                    <stop offset="100%" stopColor="#E06C5A" stopOpacity="0.02" />
                  </linearGradient>
                </defs>

                {/* Horizontal Grid Lines + Left Pressure Axis + Right Irradiance Axis */}
                {[
                  { p: 700, irr: 125 },
                  { p: 800, irr: 250 },
                  { p: 900, irr: 375 },
                  { p: 1000, irr: 500 },
                ].map(({ p, irr }) => {
                  const y = yForPressure(p);
                  const isThresh = p === PRESSURE_THRESHOLD_PA;
                  return (
                    <g key={p}>
                      <line
                        x1={padL}
                        x2={chartW - padR}
                        y1={y}
                        y2={y}
                        stroke={isThresh ? '#E06C5A' : 'rgba(255,255,255,0.08)'}
                        strokeWidth={isThresh ? 1.5 : 1}
                        strokeDasharray={isThresh ? '5 4' : undefined}
                      />
                      <text
                        x={padL - 8}
                        y={y + 4}
                        textAnchor="end"
                        className="fill-[#6FC3E8] font-mono text-[10px]"
                      >
                        {p}
                      </text>
                      <text
                        x={chartW - padR + 8}
                        y={y + 4}
                        textAnchor="start"
                        className="fill-[#FFD166] font-mono text-[10px]"
                      >
                        {irr}
                      </text>
                    </g>
                  );
                })}

                <rect
                  x={padL}
                  y={thresholdY}
                  width={plotW}
                  height={padT + plotH - thresholdY}
                  fill="url(#stormZoneGrad)"
                />
                <text
                  x={chartW - padR - 6}
                  y={thresholdY - 6}
                  textAnchor="end"
                  className="fill-[#E06C5A] font-mono text-[10px]"
                >
                  800 Pa Dust Threshold
                </text>

                {/* Solar Irradiance Secondary Model Line */}
                {chartPoints.length > 1 && (
                  <polyline
                    fill="none"
                    stroke="#FFD166"
                    strokeWidth={1.75}
                    strokeDasharray="4 4"
                    strokeLinejoin="round"
                    points={irradiancePolyline}
                    style={{ transition: 'all 260ms cubic-bezier(0.16, 1, 0.3, 1)' }}
                  />
                )}

                {/* Atmospheric Pressure Primary Curve */}
                {chartPoints.length > 1 && (
                  <polyline
                    fill="none"
                    stroke="#6FC3E8"
                    strokeWidth={2.5}
                    strokeLinejoin="round"
                    strokeLinecap="round"
                    points={pressurePolyline}
                    style={{ transition: 'all 260ms cubic-bezier(0.16, 1, 0.3, 1)' }}
                  />
                )}

                {chartPoints.map((pt) => {
                  const isActive = pt.sol === selectedSol;
                  const belowThreshold = pt.effectivePressure < PRESSURE_THRESHOLD_PA;
                  const nodeColor = belowThreshold ? '#E06C5A' : '#6FC3E8';

                  return (
                    <g
                      key={pt.sol}
                      onMouseEnter={() => setHoveredSol(pt.sol)}
                      onMouseLeave={() => setHoveredSol(null)}
                      onClick={() => onSelectSol(pt.sol)}
                      className="cursor-pointer"
                    >
                      {isActive && (
                        <line
                          x1={pt.x}
                          x2={pt.x}
                          y1={padT}
                          y2={padT + plotH}
                          stroke="rgba(200, 155, 106, 0.45)"
                          strokeWidth={1.5}
                          strokeDasharray="3 3"
                        />
                      )}

                      {/* Irradiance node */}
                      <circle
                        cx={pt.x}
                        cy={pt.yI}
                        r={3.5}
                        fill="#FFD166"
                        stroke="#07090E"
                        strokeWidth={1.5}
                        style={{ transition: 'cy 260ms cubic-bezier(0.16, 1, 0.3, 1)' }}
                      />

                      {isActive && (
                        <circle
                          cx={pt.x}
                          cy={pt.yP}
                          r={10}
                          fill="none"
                          stroke="#C89B6A"
                          strokeWidth={1.5}
                          style={{ transition: 'cy 260ms cubic-bezier(0.16, 1, 0.3, 1)' }}
                        />
                      )}

                      <circle
                        cx={pt.x}
                        cy={pt.yP}
                        r={isActive ? 5.5 : 4.5}
                        fill={nodeColor}
                        stroke="#07090E"
                        strokeWidth={2}
                        style={{ transition: 'cy 260ms cubic-bezier(0.16, 1, 0.3, 1)' }}
                      />

                      <text
                        x={pt.x}
                        y={pt.yP - 14}
                        textAnchor="middle"
                        className={`font-mono text-[10px] ${
                          isActive ? 'fill-[#F1F4F8] font-semibold' : 'fill-[#9AA4B8]'
                        }`}
                        style={{ transition: 'y 260ms cubic-bezier(0.16, 1, 0.3, 1)' }}
                      >
                        {pt.effectivePressure.toFixed(2)} Pa
                      </text>

                      <text
                        x={pt.x}
                        y={chartH - 8}
                        textAnchor="middle"
                        className={`font-mono text-[11px] ${
                          isActive ? 'fill-[#C89B6A] font-semibold' : 'fill-[#9AA4B8]'
                        }`}
                      >
                        Sol {pt.sol}
                      </text>
                    </g>
                  );
                })}
              </svg>
            </div>
          </div>
        </div>

        {/* Habitat Engineering Calibration & Radar Envelope Workbench */}
        <div className="mt-6 grid grid-cols-1 gap-6 rounded-xl border border-white/[0.1] bg-[#111622] p-6 lg:grid-cols-12 lg:items-center">
          <div className="lg:col-span-7">
            <div className="text-xs font-mono uppercase tracking-wider text-[#C89B6A]">
              Habitat Shielding &amp; Irradiance Engineering
            </div>
            <h3 className="mt-1 font-display text-xl font-bold text-[#F1F4F8]">
              Test Mass Shielding &amp; Dust Attenuation on Sol {selectedSol}
            </h3>
            <p className="mt-1.5 text-sm text-[#9AA4B8]">
              Adjust physical regolith overburden, water ice lining, and solar array dust to observe how real engineering mass counters the active Sol telemetry.
            </p>

            <div className="mt-6 space-y-5">
              {/* Slider 1: Regolith Overburden */}
              <div>
                <div className="flex items-center justify-between text-xs">
                  <label htmlFor="regolith-slider" className="font-medium text-[#CBD5E1]">
                    Compacted Regolith Overburden (1.6 g/cm³)
                  </label>
                  <span className="font-mono font-semibold text-[#C89B6A] tabular-nums">
                    {engConfig.regolithCm} cm ({(engConfig.regolithCm * 1.6).toFixed(1)} g/cm²)
                  </span>
                </div>
                <input
                  id="regolith-slider"
                  type="range"
                  min={0}
                  max={60}
                  step={2}
                  value={engConfig.regolithCm}
                  onChange={(e) =>
                    setEngConfig((c) => ({ ...c, regolithCm: Number(e.target.value) }))
                  }
                  className="mt-2 w-full accent-[#C89B6A] cursor-pointer"
                />
              </div>

              {/* Slider 2: Water Ice Liner */}
              <div>
                <div className="flex items-center justify-between text-xs">
                  <label htmlFor="ice-slider" className="font-medium text-[#CBD5E1]">
                    Subsurface Water Ice Liner (Hydrogen-rich attenuation)
                  </label>
                  <span className="font-mono font-semibold text-[#6FC3E8] tabular-nums">
                    {engConfig.waterIceCm} cm ({(engConfig.waterIceCm * 0.92).toFixed(1)} g/cm²)
                  </span>
                </div>
                <input
                  id="ice-slider"
                  type="range"
                  min={0}
                  max={25}
                  step={1}
                  value={engConfig.waterIceCm}
                  onChange={(e) =>
                    setEngConfig((c) => ({ ...c, waterIceCm: Number(e.target.value) }))
                  }
                  className="mt-2 w-full accent-[#6FC3E8] cursor-pointer"
                />
              </div>

              {/* Slider 3: Solar Array Dust Coverage */}
              <div>
                <div className="flex items-center justify-between text-xs">
                  <label htmlFor="dust-slider" className="font-medium text-[#CBD5E1]">
                    Solar Array Dust Coverage (0% = freshly brushed)
                  </label>
                  <span className="font-mono font-semibold text-[#FFD166] tabular-nums">
                    {engConfig.panelDustPct}% dust · Net {env.netPanelYieldWm2.toFixed(1)} W/m²
                  </span>
                </div>
                <input
                  id="dust-slider"
                  type="range"
                  min={0}
                  max={80}
                  step={5}
                  value={engConfig.panelDustPct}
                  onChange={(e) =>
                    setEngConfig((c) => ({ ...c, panelDustPct: Number(e.target.value) }))
                  }
                  className="mt-2 w-full accent-[#FFD166] cursor-pointer"
                />
              </div>
            </div>

            <div className="mt-6 grid grid-cols-3 gap-4 border-t border-white/[0.08] pt-4 font-mono text-xs">
              <div>
                <div className="text-[#9AA4B8]">Total Shield Mass</div>
                <div className="mt-0.5 text-sm font-semibold text-[#F1F4F8] tabular-nums">
                  {env.arealShieldingGcm2.toFixed(1)} g/cm²
                </div>
              </div>
              <div>
                <div className="text-[#9AA4B8]">Dose Attenuation</div>
                <div className="mt-0.5 text-sm font-semibold text-[#E06C5A] tabular-nums">
                  -{env.attenuationPct}%
                </div>
              </div>
              <div>
                <div className="text-[#9AA4B8]">O₂ Scrubber Load</div>
                <div className="mt-0.5 text-sm font-semibold text-[#6FC3E8] tabular-nums">
                  {env.scrubberPowerDrawPct}%
                </div>
              </div>
            </div>
          </div>

          <div className="flex flex-col items-center justify-center rounded-xl border border-white/[0.08] bg-[#07090E] p-5 lg:col-span-5">
            <div className="mb-2 text-center font-mono text-xs uppercase tracking-wider text-[#9AA4B8]">
              Habitat Calibration Envelope
            </div>
            <TelemetryRadarChart
              axes={env.radarAxes}
              stormActive={env.evaluation.stormTriggered}
            />
          </div>
        </div>
      </div>
    </section>
  );
};

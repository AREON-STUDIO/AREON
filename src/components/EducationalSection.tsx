import React, { useState } from 'react';
import {
  ArrowUpRight,
  BookOpen,
  Lock,
  ShieldCheck,
  Sparkles,
  X,
} from 'lucide-react';
import { TelemetryRow } from '../types/telemetry';
import { evaluateStormTrigger, getSolBrief } from '../utils/csvTelemetry';

interface EducationalSectionProps {
  rows: TelemetryRow[];
  selectedSol: number;
  onInspectSol: (sol: number) => void;
  
}

export const EducationalSection: React.FC<EducationalSectionProps> = ({
  rows,
  selectedSol,
  onInspectSol,
}) => {
  const [mcqChoice, setMcqChoice] = useState<number | null>(null);
  const [showGuruHint, setShowGuruHint] = useState(false);
  const [gateOpen, setGateOpen] = useState(false);
  const [gateAnswer, setGateAnswer] = useState('');
  const [gateStatus, setGateStatus] = useState<'pending' | 'verified' | 'retry'>('pending');

  const sampleOptions = [
    'Dust scatters sunlight in the sky, lowering irradiance before light reaches the panels.',
    'Solar panels lose all wiring connections whenever air pressure drops.',
    'Mars stops rotating during low-pressure dust storms.',
  ];

  const handleVerifyGate = () => {
    if (gateAnswer.trim() === '21') {
      setGateStatus('verified');
    } else {
      setGateStatus('retry');
    }
  };

  return (
    <>
      {/* SECTION 4 — The Educational Breakdown */}
      <section
        id="scenarios"
        className="border-b border-white/[0.08] bg-[#0B0E14] py-16 md:py-24"
      >
        <div className="mx-auto max-w-[1200px] px-6">
          <div className="mb-10 max-w-2xl">
            <p className="text-xs font-mono uppercase tracking-wider text-[#C89B6A]">
              Educational Breakdown
            </p>
            <h2
              className="mt-1.5 font-display text-3xl font-bold tracking-tight text-[#F1F4F8] md:text-4xl"
              style={{ textWrap: 'balance' }}
            >
              Curiosity Sol Scenarios. Real Survival Lessons.
            </h2>
            <p className="mt-2 text-sm leading-relaxed text-[#9AA4B8]">
              Select any scenario below to inspect its live telemetry parameters in Mission Control and load it into the habitat simulation.
            </p>
          </div>

          <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
            {rows.map((row) => {
              const brief = getSolBrief(row);
              const evalResult = evaluateStormTrigger(row.pressurePa, row.flareClass);
              const isSelected = row.sol === selectedSol;

              return (
                <article
                  key={row.sol}
                  className={`flex flex-col justify-between rounded-xl border p-6 transition-colors ${
                    isSelected
                      ? 'border-[#C89B6A]/70 bg-[#141A28]'
                      : 'border-white/[0.1] bg-[#111622] hover:border-white/25'
                  }`}
                >
                  <div>
                    <div className="flex items-center justify-between gap-2 text-xs font-mono text-[#9AA4B8]">
                      <span>{brief.challengeType}</span>
                      <span aria-hidden="true">·</span>
                      <span className="tabular-nums">Day {row.day}</span>
                    </div>

                    <h3 className="mt-3 font-display text-xl font-bold text-[#F1F4F8]">
                      {brief.title}
                    </h3>

                    <p className="mt-3 text-sm leading-relaxed text-[#CBD5E1]">
                      {brief.summary}
                    </p>
                  </div>

                  <div className="mt-6 border-t border-white/[0.08] pt-4">
                    <div className="flex items-center justify-between font-mono text-xs text-[#9AA4B8] tabular-nums">
                      <span>{row.pressurePa.toFixed(2)} Pa</span>
                      <span>Flare: {row.flareClass}</span>
                      <span
                        className={
                          evalResult.stormTriggered ? 'text-[#E06C5A]' : 'text-[#6FC3E8]'
                        }
                      >
                        {evalResult.stormTriggered ? 'Storm: TRUE' : 'Storm: FALSE'}
                      </span>
                    </div>

                    <button
                      type="button"
                      onClick={() => onInspectSol(row.sol)}
                      className="mt-4 inline-flex w-full items-center justify-between rounded-lg border border-white/12 bg-[#0B0E14]/70 px-3.5 py-2.5 text-xs font-semibold text-[#F1F4F8] transition-colors hover:border-[#C89B6A] hover:text-[#C89B6A] cursor-pointer"
                    >
                      <span>Inspect Sol {row.sol} Telemetry</span>
                      <ArrowUpRight className="h-4 w-4" aria-hidden="true" />
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        </div>
      </section>

      {/* SECTION 5 — The No-Fail Guarantee (Parents & Educators) */}
      <section
        id="no-fail"
        className="border-b border-white/[0.08] bg-[#0D111A] py-16 md:py-24"
      >
        <div className="mx-auto max-w-[1200px] px-6">
          <div className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:items-start">
            <div className="lg:col-span-7">
              <p className="text-xs font-mono uppercase tracking-wider text-[#C89B6A]">
                Parents &amp; Educators
              </p>
              <h2
                className="mt-1.5 font-display text-3xl font-bold tracking-tight text-[#F1F4F8] md:text-4xl"
                style={{ textWrap: 'balance' }}
              >
                We believe kids learn best when they aren&apos;t afraid to fail.
              </h2>

              <div className="mt-8 grid grid-cols-1 gap-6 sm:grid-cols-2">
                <div className="rounded-xl border border-white/[0.08] bg-[#111622] p-5">
                  <ShieldCheck className="h-5 w-5 text-[#6FC3E8]" aria-hidden="true" />
                  <h3 className="mt-3 font-display text-base font-bold text-[#F1F4F8]">
                    No Game Overs
                  </h3>
                  <p className="mt-1.5 text-sm leading-relaxed text-[#9AA4B8]">
                    Gentle auto-rescue stabilizes the dome if meters empty, keeping ages 7–12 curious rather than frustrated.
                  </p>
                </div>

                <div className="rounded-xl border border-white/[0.08] bg-[#111622] p-5">
                  <BookOpen className="h-5 w-5 text-[#FFD166]" aria-hidden="true" />
                  <h3 className="mt-3 font-display text-base font-bold text-[#F1F4F8]">
                    Learning MCQs
                  </h3>
                  <p className="mt-1.5 text-sm leading-relaxed text-[#9AA4B8]">
                    Wrong answers teach; they do not punish. Every rescue turns into a gentle science discovery moment.
                  </p>
                </div>

                <div className="flex flex-col justify-between rounded-xl border border-white/[0.08] bg-[#111622] p-5">
                  <div>
                    <Lock className="h-5 w-5 text-[#C89B6A]" aria-hidden="true" />
                    <h3 className="mt-3 font-display text-base font-bold text-[#F1F4F8]">
                      Parental Gate
                    </h3>
                    <p className="mt-1.5 text-sm leading-relaxed text-[#9AA4B8]">
                      A safe, compliant entry screen protects young learners with zero ads, trackers, or open-web links.
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={() => {
                      setGateAnswer('');
                      setGateStatus('pending');
                      setGateOpen(true);
                    }}
                    className="mt-3 self-start text-xs font-semibold text-[#C89B6A] hover:underline cursor-pointer"
                  >
                    Preview Parental Gate
                  </button>
                </div>

                <div className="rounded-xl border border-white/[0.08] bg-[#111622] p-5">
                  <Sparkles className="h-5 w-5 text-[#E06C5A]" aria-hidden="true" />
                  <h3 className="mt-3 font-display text-base font-bold text-[#F1F4F8]">
                    AstroGuru
                  </h3>
                  <p className="mt-1.5 text-sm leading-relaxed text-[#9AA4B8]">
                    Helpful AI hints nudge kids toward planetary physics principles without spoiling the puzzle.
                  </p>
                </div>
              </div>
            </div>

            {/* Interactive Sample Learning MCQ & AstroGuru Preview */}
            <div className="rounded-xl border border-white/[0.1] bg-[#111622] p-6 lg:col-span-5">
              <div className="text-xs font-mono uppercase tracking-wider text-[#C89B6A]">
                Interactive Pedagogy Preview
              </div>
              <h3 className="mt-2 font-display text-lg font-bold text-[#F1F4F8]">
                Try a No-Fail Learning Check
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-[#CBD5E1]">
                During Sol 2085, your solar panels are spotless, yet power still recovers slowly. Why?
              </p>

              <div className="mt-4 flex flex-col gap-2.5">
                {sampleOptions.map((option, idx) => {
                  const chosen = mcqChoice === idx;
                  const isCorrect = idx === 0;

                  return (
                    <button
                      key={option}
                      type="button"
                      onClick={() => setMcqChoice(idx)}
                      className={`rounded-lg border px-3.5 py-2.5 text-left text-xs leading-relaxed transition-colors cursor-pointer ${
                        chosen
                          ? isCorrect
                            ? 'border-[#6FC3E8] bg-[#6FC3E8]/15 text-[#F1F4F8]'
                            : 'border-[#FFD166] bg-[#FFD166]/12 text-[#F1F4F8]'
                          : 'border-white/10 bg-[#0B0E14]/70 text-[#CBD5E1] hover:border-[#C89B6A]/50'
                      }`}
                    >
                      {option}
                    </button>
                  );
                })}
              </div>

              {mcqChoice !== null && (
                <div className="mt-4 rounded-lg border border-white/10 bg-[#0B0E14] p-3.5 text-xs leading-relaxed text-[#CBD5E1]">
                  {mcqChoice === 0 ? (
                    <span className="text-[#6FC3E8]">
                      Correct! Airborne dust blocks sunlight before it reaches the dome—so irradiance matters even on clean panels.
                    </span>
                  ) : (
                    <span className="text-[#FFD166]">
                      No penalty! Think about what happens when dust fills the Martian sky—even clean panels receive less sunlight (lower irradiance).
                    </span>
                  )}
                </div>
              )}

              <div className="mt-4 border-t border-white/[0.08] pt-4">
                <button
                  type="button"
                  onClick={() => setShowGuruHint((v) => !v)}
                  className="text-xs font-semibold text-[#C89B6A] hover:underline cursor-pointer"
                >
                  {showGuruHint ? 'Hide AstroGuru Hint' : 'Ask AstroGuru for a Hint'}
                </button>
                {showGuruHint && (
                  <p className="mt-2 text-xs leading-relaxed text-[#9AA4B8]">
                    AstroGuru: &quot;Look up at the sky in Sol 2085—when air pressure falls below 800 Pa, swirling dust dims the Sun like heavy fog!&quot;
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* SECTION 6 — The Scientific Defense + NASA Analog Site Comparison */}
      <section
        id="science"
        className="border-b border-white/[0.08] bg-[#0B0E14] py-16 md:py-24"
      >
        <div className="mx-auto max-w-[1200px] px-6">
          <div className="mb-10 max-w-2xl">
            <p className="text-xs font-mono uppercase tracking-wider text-[#C89B6A]">
              Scientific Defense
            </p>
            <h2
              className="mt-1.5 font-display text-3xl font-bold tracking-tight text-[#F1F4F8] md:text-4xl"
              style={{ textWrap: 'balance' }}
            >
              Real Science, Simplified.
            </h2>
          </div>

          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
            <div className="rounded-xl border border-white/[0.08] bg-[#111622] p-5">
              <div className="font-mono text-xs text-[#C89B6A]">01 · Analog Dataset</div>
              <h3 className="mt-2 font-display text-lg font-bold text-[#F1F4F8]">
                Gale Crater to Arcadia Planitia
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-[#9AA4B8]">
                Real Curiosity REMS data from Gale Crater is used as an analog for Arcadia Planitia.
              </p>
            </div>

            <div className="rounded-xl border border-white/[0.08] bg-[#111622] p-5">
              <div className="font-mono text-xs text-[#6FC3E8]">02 · Dual Weather</div>
              <h3 className="mt-2 font-display text-lg font-bold text-[#F1F4F8]">
                Dust &amp; Solar Hazards
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-[#9AA4B8]">
                Models both atmospheric dust storms (pressure drops) and invisible solar radiation flares.
              </p>
            </div>

            <div className="rounded-xl border border-white/[0.08] bg-[#111622] p-5">
              <div className="font-mono text-xs text-[#E06C5A]">03 · Mass = Shielding</div>
              <h3 className="mt-2 font-display text-lg font-bold text-[#F1F4F8]">
                Regolith &amp; Water Ice
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-[#9AA4B8]">
                Habitat shielding relies on real compacted Martian regolith and water ice—not &quot;magic&quot; barriers.
              </p>
            </div>

            <div className="rounded-xl border border-white/[0.08] bg-[#111622] p-5">
              <div className="font-mono text-xs text-[#FFD166]">04 · Irradiance Matters</div>
              <h3 className="mt-2 font-display text-lg font-bold text-[#F1F4F8]">
                Dark Sky Power Limits
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-[#9AA4B8]">
                Clean solar panels in a dust-darkened sky still yield significantly less electrical power.
              </p>
            </div>
          </div>

          {/* NASA Planetary Site Analog Matrix */}
          <div className="mt-10 overflow-x-auto rounded-xl border border-white/[0.1] bg-[#111622]">
            <table className="w-full text-left text-xs">
              <thead className="border-b border-white/[0.08] bg-[#0D111A] font-mono uppercase tracking-wider text-[#9AA4B8]">
                <tr>
                  <th className="px-5 py-3.5">Planetary Parameter</th>
                  <th className="px-5 py-3.5 text-[#C89B6A]">Gale Crater (Curiosity REMS Source)</th>
                  <th className="px-5 py-3.5 text-[#6FC3E8]">Arcadia Planitia (AREON Habitat Site)</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-white/[0.06] font-mono text-[#CBD5E1] tabular-nums">
                <tr>
                  <td className="px-5 py-3 font-sans font-medium text-[#F1F4F8]">Areocentric Coordinates</td>
                  <td className="px-5 py-3">4.5895° S, 137.4417° E</td>
                  <td className="px-5 py-3">46.7000° N, 188.0000° E</td>
                </tr>
                <tr>
                  <td className="px-5 py-3 font-sans font-medium text-[#F1F4F8]">Datum Elevation</td>
                  <td className="px-5 py-3">-4.50 km (Aeolis Palus basin)</td>
                  <td className="px-5 py-3">-3.90 km (Lowland basin)</td>
                </tr>
                <tr>
                  <td className="px-5 py-3 font-sans font-medium text-[#F1F4F8]">Pressure Analog Range</td>
                  <td className="px-5 py-3">740 – 930 Pa (Seasonal CO₂ cycle)</td>
                  <td className="px-5 py-3">Comparable lowland pressure column</td>
                </tr>
                <tr>
                  <td className="px-5 py-3 font-sans font-medium text-[#F1F4F8]">ISRU Shielding Resource</td>
                  <td className="px-5 py-3 font-sans">Basaltic regolith &amp; hydrated clays</td>
                  <td className="px-5 py-3 font-sans">Shallow subsurface water-ice sheets + regolith</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </section>

      {/* Interactive Parental Gate Preview Modal */}
      {gateOpen && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm"
          onClick={() => setGateOpen(false)}
        >
          <div
            className="w-full max-w-md rounded-xl border border-white/15 bg-[#111622] p-6 text-[#F1F4F8]"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-white/[0.08] pb-3">
              <h3 className="font-display text-lg font-bold">Parent &amp; Educator Gate</h3>
              <button
                type="button"
                onClick={() => setGateOpen(false)}
                aria-label="Close parental gate"
                className="rounded-md p-1 text-[#9AA4B8] hover:bg-white/10 hover:text-[#F1F4F8] cursor-pointer"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
            <p className="mt-3 text-xs leading-relaxed text-[#CBD5E1]">
              To keep young astronauts (ages 7–12) safe from external links and settings changes, adults confirm access with a quick verification step:
            </p>
            <div className="mt-4 rounded-lg border border-white/10 bg-[#0B0E14] p-4">
              <label htmlFor="parent-gate-input" className="block font-mono text-xs text-[#C89B6A]">
                What is 7 × 3?
              </label>
              <div className="mt-2 flex gap-2">
                <input
                  id="parent-gate-input"
                  type="text"
                  value={gateAnswer}
                  onChange={(e) => setGateAnswer(e.target.value)}
                  placeholder="Enter answer"
                  className="w-full rounded-lg border border-white/15 bg-[#111622] px-3 py-2 font-mono text-sm text-[#F1F4F8] focus:border-[#C89B6A] focus:outline-none"
                />
                <button
                  type="button"
                  onClick={handleVerifyGate}
                  className="rounded-lg bg-[#C89B6A] px-4 py-2 text-xs font-semibold text-[#0B0E14] hover:bg-[#D8AD7E] cursor-pointer whitespace-nowrap"
                >
                  Verify
                </button>
              </div>
              {gateStatus === 'verified' && (
                <p className="mt-2.5 text-xs text-[#6FC3E8]">
                  Adult access verified. External NASA PDS links and telemetry settings unlocked.
                </p>
              )}
              {gateStatus === 'retry' && (
                <p className="mt-2.5 text-xs text-[#FFD166]">
                  Not quite—try 7 × 3 to unlock adult mission settings.
                </p>
              )}
            </div>
          </div>
        </div>
      )}
    </>
  );
};

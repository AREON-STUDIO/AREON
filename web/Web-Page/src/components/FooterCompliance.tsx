import React, { useState } from 'react';
import { X } from 'lucide-react';

type ComplianceTab = 'build-log' | 'ai-disclosure' | 'rules' | null;

export const FooterCompliance: React.FC = () => {
  const [activeModal, setActiveModal] = useState<ComplianceTab>(null);

  return (
    <footer className="bg-[#080B10] py-14 text-sm text-[#9AA4B8]">
      <div className="mx-auto max-w-[1200px] px-6">
        <div className="grid grid-cols-1 gap-10 border-b border-white/[0.08] pb-10 md:grid-cols-3">
          {/* NASA PDS Attribution */}
          <div>
            <h3 className="font-display text-sm font-semibold text-[#F1F4F8]">
              NASA PDS Attribution
            </h3>
            <p className="mt-2.5 text-xs leading-relaxed text-[#9AA4B8]">
              Telemetry grounded in archival observations from the NASA Planetary Data System (PDS) and the Mars Science Laboratory Curiosity Rover Environmental Monitoring Station (REMS) science team.
            </p>
          </div>

          {/* Team Credits */}
          <div>
            <h3 className="font-display text-sm font-semibold text-[#F1F4F8]">
              Mission Team Credits
            </h3>
            <ul className="mt-2.5 space-y-1.5 text-xs text-[#9AA4B8]">
              <li>Lead Architect · Mission Systems &amp; Telemetry Logic</li>
              <li>Unity Tech Lead (Sudharsan) · WebGL Simulation &amp; CSV Ingestion</li>
              <li>Level / Art Lead · Martian Dome &amp; Environmental Design</li>
              <li>Mission Control Web Lead (Meghna) · Command Portal &amp; Live Dashboard</li>
            </ul>
          </div>

          {/* Hackathon Compliance */}
          <div>
            <h3 className="font-display text-sm font-semibold text-[#F1F4F8]">
              Hackathon Compliance
            </h3>
            <p className="mt-2.5 text-xs leading-relaxed text-[#9AA4B8]">
              Inspect project provenance, single-source CSV verification, and educational AI disclosures:
            </p>
            <div className="mt-3 flex flex-wrap gap-3 text-xs font-medium">
              <button
                type="button"
                onClick={() => setActiveModal('build-log')}
                className="text-[#C89B6A] hover:underline cursor-pointer"
              >
                48-Hour Build Log
              </button>
              <span aria-hidden="true">·</span>
              <button
                type="button"
                onClick={() => setActiveModal('ai-disclosure')}
                className="text-[#C89B6A] hover:underline cursor-pointer"
              >
                AI Voice &amp; Hint Disclosure
              </button>
              <span aria-hidden="true">·</span>
              <button
                type="button"
                onClick={() => setActiveModal('rules')}
                className="text-[#C89B6A] hover:underline cursor-pointer"
              >
                Rule Adherence
              </button>
            </div>
          </div>
        </div>

        <div className="mt-6 flex flex-col items-start justify-between gap-3 text-xs text-[#64748B] sm:flex-row sm:items-center">
          <span>AREON Command Center · Hackathon Final Build</span>
          <span className="font-mono">Single Source of Truth: /data.csv</span>
        </div>
      </div>

      {/* Compliance Disclosure Modal */}
      {activeModal && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 p-4 backdrop-blur-sm"
          onClick={() => setActiveModal(null)}
        >
          <div
            className="w-full max-w-lg rounded-xl border border-white/15 bg-[#111622] p-6 text-[#F1F4F8]"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-white/[0.08] pb-3">
              <h3 className="font-display text-lg font-bold">
                {activeModal === 'build-log'
                  ? '48-Hour Development Build Log'
                  : activeModal === 'ai-disclosure'
                    ? 'AI Voice & AstroGuru Hint Disclosure'
                    : 'Hackathon Rule Adherence'}
              </h3>
              <button
                type="button"
                onClick={() => setActiveModal(null)}
                aria-label="Close disclosure"
                className="rounded-md p-1 text-[#9AA4B8] hover:bg-white/10 hover:text-[#F1F4F8] cursor-pointer"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            {activeModal === 'build-log' && (
              <div className="mt-4 space-y-2.5 text-xs leading-relaxed text-[#CBD5E1]">
                <p>
                  <strong className="text-[#C89B6A]">Hours 0–8 (Foundation):</strong> Configured React &amp; Tailwind command portal, animated Mars dome hero, and 16:9 WebGL container.
                </p>
                <p>
                  <strong className="text-[#C89B6A]">Hours 8–16 (Mission Control):</strong> Implemented runtime <span className="font-mono">fetch(&apos;/data.csv&apos;)</span>, raw terminal inspector, interactive pressure graph, and judge-proof custom pressure testing.
                </p>
                <p>
                  <strong className="text-[#C89B6A]">Hours 16–24 (Learning Sections):</strong> Built Sol 800/852/2085 breakdowns, No-Fail Pedagogy preview, and Scientific Defense modules.
                </p>
                <p>
                  <strong className="text-[#C89B6A]">Hours 24–36 (Game Integration):</strong> Wired browser-playable Mars Dome simulation inside the iframe with decompression progress bar.
                </p>
                <p>
                  <strong className="text-[#C89B6A]">Hours 36–48 (Final QA):</strong> Verified NASA PDS attributions, WCAG contrast, responsive layout, and zero hardcoded logic.
                </p>
              </div>
            )}

            {activeModal === 'ai-disclosure' && (
              <div className="mt-4 space-y-3 text-xs leading-relaxed text-[#CBD5E1]">
                <p>
                  AREON&apos;s in-game AstroGuru assistant provides age-appropriate (7–12) Socratic hints about Martian dust storms, solar flares, and regolith shielding without giving away puzzle answers.
                </p>
                <p>
                  All environmental triggers, pressure readings, and storm thresholds remain 100% deterministic and parsed directly from NASA Curiosity REMS telemetry (<span className="font-mono">/data.csv</span>).
                </p>
              </div>
            )}

            {activeModal === 'rules' && (
              <div className="mt-4 space-y-3 text-xs leading-relaxed text-[#CBD5E1]">
                <p>
                  <strong>Single Source of Truth:</strong> The portal does not hardcode Sol storm states. Both the Mission Control dashboard and browser simulation fetch and parse <span className="font-mono">/data.csv</span> at runtime using <span className="font-mono">Trigger = (Pressure &lt; 800) OR (Flare != &quot;none&quot;)</span>.
                </p>
                <p>
                  <strong>Child Safety:</strong> Compliant no-fail design with zero ads, zero external trackers, and a parental gate architecture.
                </p>
              </div>
            )}
          </div>
        </div>
      )}
    </footer>
  );
};

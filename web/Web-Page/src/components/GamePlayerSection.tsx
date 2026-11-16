
import React, { useRef, useState } from 'react';
import { Maximize2, Play, RotateCcw, Square } from 'lucide-react';
import { GamePlayerState, TelemetryRow } from '../types/telemetry';

interface GamePlayerSectionProps {
  playerState: GamePlayerState;
  rows: TelemetryRow[];
  selectedSol: number;
  onStartRequest: () => void;
  onReturnStandby: () => void;
  onCompleteLoading: () => void;
}

const GAME_EMBED_URL =
  'https://itch.io/embed-upload/20583875?color=333333';

export const GamePlayerSection: React.FC<GamePlayerSectionProps> = ({
  playerState,
  onStartRequest,
  onReturnStandby,
}) => {
  const [iframeKey, setIframeKey] = useState(0);
  const containerRef = useRef<HTMLDivElement | null>(null);

  const handleRestart = () => {
    setIframeKey((key) => key + 1);
  };

  const handleFullscreen = () => {
    if (!containerRef.current) return;

    if (document.fullscreenElement) {
      document.exitFullscreen().catch(() => {});
    } else {
      containerRef.current.requestFullscreen().catch(() => {});
    }
  };

  return (
    <section
      id="game-player"
      className="border-b border-white/[0.08] bg-[#0B0E14] py-16 md:py-20"
    >
      <div className="mx-auto max-w-[1200px] px-6">
        <div className="mb-8 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-xs font-mono uppercase tracking-wider text-[#C89B6A]">
              Interactive Game Hub
            </p>

            <h2 className="mt-1.5 font-display text-2xl font-bold tracking-tight text-[#F1F4F8] md:text-3xl">
              Play AREON Directly in Your Browser
            </h2>
          </div>

          <p className="text-sm text-[#9AA4B8]">
            Unity WebGL · Powered by itch.io
          </p>
        </div>

        <div
          ref={containerRef}
          className="mx-auto w-full max-w-5xl overflow-hidden rounded-xl border border-white/[0.1] bg-[#111622]"
        >
          <div className="relative aspect-video w-full bg-[#0E131D]">
            {playerState !== 'active' ? (
              <div className="flex h-full w-full flex-col items-center justify-center p-6 text-center">
                <p className="font-mono text-xs uppercase tracking-widest text-[#C89B6A]">
                  System Standby
                </p>

                <h3 className="mt-3 font-display text-2xl font-bold text-[#F1F4F8] md:text-3xl">
                  Mars Dome Ready for Launch
                </h3>

                <p className="mt-2.5 max-w-md text-sm leading-relaxed text-[#9AA4B8]">
                  Manage Air, Power, and Regolith Shielding across real Curiosity Rover Sols.
                  No game overs—auto-rescue drones keep learning safe.
                </p>

                <button
                  type="button"
                  onClick={onStartRequest}
                  className="mt-7 inline-flex min-h-[44px] items-center gap-2.5 rounded-lg bg-[#C89B6A] px-6 py-3 text-sm font-semibold text-[#0B0E14] transition-colors hover:bg-[#D8AD7E] cursor-pointer"
                >
                  <Play className="h-4 w-4 fill-current" />
                  Start Simulation
                </button>
              </div>
            ) : (
              <iframe
  key={iframeKey}
  src={GAME_EMBED_URL}
  title="AREON - Surviving Mars"
  className="absolute inset-0 h-full w-full border-0"
  allow="fullscreen; autoplay; gamepad"
  allowFullScreen
  loading="lazy"
/>
            )}
          </div>

          <div className="flex flex-wrap items-center justify-between gap-4 border-t border-white/[0.08] bg-[#111622] px-4 py-3 text-xs">
            <div className="flex items-center gap-3 font-mono text-[#9AA4B8]">
              STATE:{' '}
              <strong className="font-semibold text-[#F1F4F8]">
                {playerState === 'active' ? 'ACTIVE' : 'STANDBY'}
              </strong>
              <span>·</span>
              UNITY WEBGL
            </div>

            <div className="flex items-center gap-2">
              {playerState === 'active' && (
                <>
                  <button
                    type="button"
                    onClick={handleRestart}
                    className="inline-flex items-center gap-1.5 rounded-md border border-white/10 bg-white/[0.04] px-3 py-1.5 font-medium text-[#CBD5E1] hover:text-white cursor-pointer"
                  >
                    <RotateCcw className="h-3.5 w-3.5" />
                    Restart
                  </button>

                  <button
                    type="button"
                    onClick={onReturnStandby}
                    className="inline-flex items-center gap-1.5 rounded-md border border-white/10 bg-white/[0.04] px-3 py-1.5 font-medium text-[#CBD5E1] hover:text-white cursor-pointer"
                  >
                    <Square className="h-3.5 w-3.5" />
                    Standby
                  </button>
                </>
              )}

              <button
                type="button"
                onClick={handleFullscreen}
                className="inline-flex items-center gap-1.5 rounded-md border border-white/10 bg-white/[0.04] px-3 py-1.5 font-medium text-[#CBD5E1] hover:text-white cursor-pointer"
              >
                <Maximize2 className="h-3.5 w-3.5" />
                Fullscreen
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

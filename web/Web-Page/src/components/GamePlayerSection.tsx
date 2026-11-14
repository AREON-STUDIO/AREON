import React, { useEffect, useRef, useState } from 'react';
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

export const GamePlayerSection: React.FC<GamePlayerSectionProps> = ({
  playerState,
  rows,
  selectedSol,
  onStartRequest,
  onReturnStandby,
  onCompleteLoading,
}) => {
  const [progress, setProgress] = useState(0);
  const [iframeKey, setIframeKey] = useState(0);
  const containerRef = useRef<HTMLDivElement | null>(null);
  const iframeRef = useRef<HTMLIFrameElement | null>(null);

  // Smooth WebGL decompression loading progress
  useEffect(() => {
    if (playerState !== 'loading') {
      setProgress(0);
      return;
    }

    let current = 0;
    const interval = window.setInterval(() => {
      current += current < 55 ? 9 : current < 88 ? 6 : 4;
      if (current >= 100) {
        setProgress(100);
        window.clearInterval(interval);
        window.setTimeout(() => {
          onCompleteLoading();
        }, 220);
      } else {
        setProgress(current);
      }
    }, 85);

    return () => window.clearInterval(interval);
  }, [playerState, onCompleteLoading]);

  // Sync Sol changes from Dashboard into the active iframe game
  useEffect(() => {
    if (playerState === 'active' && iframeRef.current?.contentWindow) {
      iframeRef.current.contentWindow.postMessage(
        { type: 'SELECT_SOL', sol: selectedSol },
        '*'
      );
    }
  }, [selectedSol, playerState]);

  // Sync Live CSV Sandbox row updates into the active iframe game
  useEffect(() => {
    if (playerState === 'active' && iframeRef.current?.contentWindow && rows.length > 0) {
      iframeRef.current.contentWindow.postMessage(
        { type: 'SYNC_CSV_ROWS', rows, selectedSol },
        '*'
      );
    }
  }, [rows, selectedSol, playerState]);

  const handleRestart = () => {
    setIframeKey((k) => k + 1);
  };

  const handleFullscreen = () => {
    if (!containerRef.current) return;
    if (document.fullscreenElement) {
      document.exitFullscreen().catch(() => {});
    } else {
      containerRef.current.requestFullscreen().catch(() => {});
    }
  };

  const loadingStage =
    progress < 35
      ? 'Fetching WebGL runtime assets...'
      : progress < 75
        ? 'Decompressing Mars Dome WebGL build...'
        : 'Syncing Curiosity REMS /data.csv telemetry...';

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
            <h2
              className="mt-1.5 font-display text-2xl font-bold tracking-tight text-[#F1F4F8] md:text-3xl"
              style={{ textWrap: 'balance' }}
            >
              Play AREON Directly in Your Browser
            </h2>
          </div>

          <p className="text-sm text-[#9AA4B8]">
            Zero ZIP downloads · Synced to <span className="font-mono text-[#F1F4F8]">/data.csv</span>
          </p>
        </div>

        <div
          ref={containerRef}
          className="mx-auto w-full max-w-5xl overflow-hidden rounded-xl border border-white/[0.1] bg-[#111622]"
        >
          <div className="relative aspect-video w-full bg-[#0E131D]">
            {playerState === 'idle' && (
              <div className="flex h-full w-full flex-col items-center justify-center p-6 text-center">
                <p className="font-mono text-xs uppercase tracking-widest text-[#C89B6A]">
                  System Standby
                </p>
                <h3 className="mt-3 font-display text-2xl font-bold text-[#F1F4F8] md:text-3xl">
                  Mars Dome Ready for Launch
                </h3>
                <p className="mt-2.5 max-w-md text-sm leading-relaxed text-[#9AA4B8]">
                  Manage Air, Power, and Regolith Shielding across real Curiosity Rover Sols. No game overs—auto-rescue drones keep learning safe.
                </p>

                <button
                  type="button"
                  onClick={onStartRequest}
                  className="mt-7 inline-flex min-h-[44px] items-center gap-2.5 rounded-lg bg-[#C89B6A] px-6 py-3 text-sm font-semibold text-[#0B0E14] transition-colors hover:bg-[#D8AD7E] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#C89B6A] cursor-pointer"
                >
                  <Play className="h-4 w-4 fill-current" aria-hidden="true" />
                  <span>Start Simulation</span>
                </button>
              </div>
            )}

            {playerState === 'loading' && (
              <div className="flex h-full w-full flex-col items-center justify-center p-8 text-center">
                <div className="w-full max-w-md">
                  <div className="flex items-center justify-between text-xs font-mono text-[#CBD5E1]">
                    <span>{loadingStage}</span>
                    <span className="tabular-nums text-[#C89B6A]">{progress}%</span>
                  </div>
                  <div
                    role="progressbar"
                    aria-valuenow={progress}
                    aria-valuemin={0}
                    aria-valuemax={100}
                    aria-label="WebGL decompression progress"
                    className="mt-3 h-2.5 w-full overflow-hidden rounded-full bg-white/[0.08]"
                  >
                    <div
                      className="h-full rounded-full bg-[#C89B6A] transition-all duration-150 ease-out"
                      style={{ width: `${progress}%` }}
                    />
                  </div>
                  <p className="mt-4 text-xs text-[#9AA4B8]">
                    Streaming compressed WebGL assets for low-bandwidth hackathon Wi-Fi.
                  </p>
                </div>
              </div>
            )}

            {playerState === 'active' && (
              <iframe
                key={iframeKey}
                ref={iframeRef}
                src="/game.html"
                title="AREON Mars Survival WebGL Build"
                className="h-full w-full border-0"
                allow="fullscreen"
                onLoad={() => {
                  if (iframeRef.current?.contentWindow && rows.length > 0) {
                    iframeRef.current.contentWindow.postMessage(
                      { type: 'SYNC_CSV_ROWS', rows, selectedSol },
                      '*'
                    );
                  }
                }}
              />
            )}
          </div>

          <div className="flex flex-wrap items-center justify-between gap-4 border-t border-white/[0.08] bg-[#111622] px-4 py-3 text-xs">
            <div className="flex items-center gap-3 font-mono text-[#9AA4B8]">
              <span>
                STATE:{' '}
                <strong className="font-semibold text-[#F1F4F8]">
                  {playerState === 'idle'
                    ? 'STANDBY'
                    : playerState === 'loading'
                      ? 'DECOMPRESSING'
                      : 'ACTIVE'}
                </strong>
              </span>
              <span aria-hidden="true">·</span>
              <span>
                ACTIVE SOL: <strong className="text-[#C89B6A]">{selectedSol}</strong>
              </span>
            </div>

            <div className="flex items-center gap-2">
              {playerState === 'active' && (
                <>
                  <button
                    type="button"
                    onClick={handleRestart}
                    className="inline-flex items-center gap-1.5 rounded-md border border-white/10 bg-white/[0.04] px-3 py-1.5 font-medium text-[#CBD5E1] transition-colors hover:border-[#C89B6A]/50 hover:text-[#F1F4F8] cursor-pointer whitespace-nowrap"
                  >
                    <RotateCcw className="h-3.5 w-3.5" aria-hidden="true" />
                    <span>Restart</span>
                  </button>
                  <button
                    type="button"
                    onClick={onReturnStandby}
                    className="inline-flex items-center gap-1.5 rounded-md border border-white/10 bg-white/[0.04] px-3 py-1.5 font-medium text-[#CBD5E1] transition-colors hover:border-[#E06C5A]/50 hover:text-[#F1F4F8] cursor-pointer whitespace-nowrap"
                  >
                    <Square className="h-3.5 w-3.5" aria-hidden="true" />
                    <span>Standby</span>
                  </button>
                </>
              )}
              <button
                type="button"
                onClick={handleFullscreen}
                className="inline-flex items-center gap-1.5 rounded-md border border-white/10 bg-white/[0.04] px-3 py-1.5 font-medium text-[#CBD5E1] transition-colors hover:border-[#C89B6A]/50 hover:text-[#F1F4F8] cursor-pointer whitespace-nowrap"
              >
                <Maximize2 className="h-3.5 w-3.5" aria-hidden="true" />
                <span>Fullscreen</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

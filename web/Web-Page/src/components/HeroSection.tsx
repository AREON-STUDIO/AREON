import React, { useEffect, useRef, useState } from 'react';
import { Download, Play } from 'lucide-react';

interface HeroSectionProps {
  onPlayClick: () => void;
  onDownloadPc: () => Promise<void>;
}

export const HeroSection: React.FC<HeroSectionProps> = ({
  onPlayClick,
  onDownloadPc,
}) => {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [downloading, setDownloading] = useState(false);

  // Seamless 10-second looping animated Mars dome interior with warm lighting & floating dust motes
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const prefersReducedMotion = window.matchMedia(
      '(prefers-reduced-motion: reduce)'
    ).matches;

    let frameId = 0;
    const motes = Array.from({ length: 56 }, (_, i) => ({
      phase: (i / 56) * Math.PI * 2,
      baseX: ((i * 97) % 100) / 100,
      baseY: ((i * 53) % 100) / 100,
      ampX: 0.015 + (i % 5) * 0.005,
      ampY: 0.018 + (i % 4) * 0.006,
      size: 1.2 + (i % 3) * 0.85,
      warm: i % 3 !== 0,
    }));

    const resize = () => {
      const parent = canvas.parentElement;
      if (!parent) return;
      const dpr = Math.min(window.devicePixelRatio || 1, 2);
      canvas.width = Math.max(320, Math.floor(parent.clientWidth * dpr));
      canvas.height = Math.max(320, Math.floor(parent.clientHeight * dpr));
    };

    resize();
    window.addEventListener('resize', resize);

    const CYCLE_MS = 10000; // Exact 10-second seamless loop
    const start = performance.now();

    const render = (now: number) => {
      const w = canvas.width;
      const h = canvas.height;
      const elapsed = prefersReducedMotion ? 0 : (now - start) % CYCLE_MS;
      const t = (elapsed / CYCLE_MS) * Math.PI * 2;

      // Deep space and Mars twilight sky outside the dome
      const bgGrad = ctx.createLinearGradient(0, 0, 0, h);
      bgGrad.addColorStop(0, '#0B0E14');
      bgGrad.addColorStop(0.52, '#141724');
      bgGrad.addColorStop(0.84, '#2C1D18');
      bgGrad.addColorStop(1, '#0B0E14');
      ctx.fillStyle = bgGrad;
      ctx.fillRect(0, 0, w, h);

      // Subtle breathing warm interior dome light (10s seamless cycle)
      const lightPulse = 0.22 + Math.sin(t) * 0.035;
      const domeCenterX = w * 0.68;
      const domeCenterY = h * 0.56;
      const domeRadius = Math.min(w, h) * 0.54;

      const warmGlow = ctx.createRadialGradient(
        domeCenterX,
        domeCenterY - domeRadius * 0.15,
        domeRadius * 0.05,
        domeCenterX,
        domeCenterY,
        domeRadius * 1.15
      );
      warmGlow.addColorStop(0, `rgba(200, 155, 106, ${lightPulse})`);
      warmGlow.addColorStop(0.55, 'rgba(200, 155, 106, 0.07)');
      warmGlow.addColorStop(1, 'rgba(11, 14, 20, 0)');
      ctx.fillStyle = warmGlow;
      ctx.fillRect(0, 0, w, h);

      // Geodesic dome structural arcs
      ctx.save();
      ctx.strokeStyle = 'rgba(200, 155, 106, 0.18)';
      ctx.lineWidth = Math.max(1, w * 0.0011);

      // Outer dome rib
      ctx.beginPath();
      ctx.arc(domeCenterX, h * 0.86, domeRadius * 0.92, Math.PI, 0);
      ctx.stroke();

      // Inner geodesic lattice curves
      for (let i = 1; i <= 5; i++) {
        const angle = Math.PI + (Math.PI * i) / 6;
        ctx.beginPath();
        ctx.moveTo(domeCenterX, h * 0.86);
        ctx.lineTo(
          domeCenterX + Math.cos(angle) * domeRadius * 0.92,
          h * 0.86 + Math.sin(angle) * domeRadius * 0.92
        );
        ctx.stroke();
      }

      // Concentric dome rings
      ctx.strokeStyle = 'rgba(111, 195, 232, 0.08)';
      ctx.beginPath();
      ctx.arc(domeCenterX, h * 0.86, domeRadius * 0.62, Math.PI, 0);
      ctx.stroke();

      // Martian horizon ridge silhouette
      ctx.fillStyle = 'rgba(22, 16, 17, 0.85)';
      ctx.beginPath();
      ctx.moveTo(0, h * 0.82);
      ctx.lineTo(w * 0.18, h * 0.77);
      ctx.lineTo(w * 0.42, h * 0.8);
      ctx.lineTo(w * 0.68, h * 0.73);
      ctx.lineTo(w * 0.88, h * 0.78);
      ctx.lineTo(w, h * 0.75);
      ctx.lineTo(w, h);
      ctx.lineTo(0, h);
      ctx.closePath();
      ctx.fill();
      ctx.restore();

      // Seamless looping dust motes floating inside warm dome air
      for (const m of motes) {
        const x =
          (m.baseX + Math.sin(t + m.phase) * m.ampX) * w;
        const y =
          (m.baseY + Math.cos(t + m.phase * 1.3) * m.ampY) * h;
        const alpha = 0.25 + 0.2 * Math.sin(t + m.phase);
        ctx.fillStyle = m.warm
          ? `rgba(255, 209, 102, ${alpha})`
          : `rgba(200, 155, 106, ${alpha})`;
        ctx.beginPath();
        ctx.arc(x, y, m.size, 0, Math.PI * 2);
        ctx.fill();
      }

      if (!prefersReducedMotion) {
        frameId = requestAnimationFrame(render);
      }
    };

    frameId = requestAnimationFrame(render);
    return () => {
      cancelAnimationFrame(frameId);
      window.removeEventListener('resize', resize);
    };
  }, []);

  const handleDownload = async () => {
    setDownloading(true);
    try {
      await onDownloadPc();
    } finally {
      setDownloading(false);
    }
  };

  return (
    <section
      id="top"
      className="relative overflow-hidden border-b border-white/[0.08] bg-[#0B0E14]"
    >
      {/* Seamless 10-second looping animated Mars dome interior */}
      <canvas
        ref={canvasRef}
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 h-full w-full"
      />

      {/* Measured contrast scrim */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 bg-gradient-to-r from-[#0B0E14]/92 via-[#0B0E14]/70 to-[#0B0E14]/35"
      />

      <div className="relative mx-auto max-w-[1200px] px-6 py-20 md:py-28">
        <div className="max-w-2xl">
          <p className="mb-4 text-xs font-medium tracking-wider text-[#C89B6A]">
            Arcadia Planitia Habitat · Curiosity REMS Analog
          </p>

          <h1
            className="font-display font-bold tracking-tight text-[#F1F4F8] leading-[1.08]"
            style={{
              fontSize: 'clamp(2.25rem, 4.4vw, 3.75rem)',
              textWrap: 'balance',
            }}
          >
            AREON: Surviving Mars with Real NASA Data.
          </h1>

          <p className="mt-5 max-w-xl text-base md:text-lg leading-relaxed text-[#CBD5E1]">
            A no-fail, educational survival game for ages 7–12, driven entirely
            by Curiosity Rover telemetry.
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-4">
            <button
              type="button"
              onClick={onPlayClick}
              className="inline-flex min-h-[44px] items-center gap-2.5 rounded-lg bg-[#C89B6A] px-6 py-3 text-sm font-semibold text-[#0B0E14] transition-colors hover:bg-[#D8AD7E] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#C89B6A] whitespace-nowrap shrink-0 cursor-pointer"
            >
              <Play className="h-4 w-4 fill-current" aria-hidden="true" />
              <span>Play in Browser</span>
            </button>

            <button
              type="button"
              onClick={handleDownload}
              disabled={downloading}
              className="inline-flex min-h-[44px] items-center gap-2.5 rounded-lg border border-white/15 bg-[#111622]/85 px-5 py-3 text-sm font-medium text-[#F1F4F8] transition-colors hover:border-[#C89B6A]/60 hover:bg-[#171D2C] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#C89B6A] disabled:opacity-60 whitespace-nowrap shrink-0 cursor-pointer"
            >
              <Download className="h-4 w-4 text-[#C89B6A]" aria-hidden="true" />
              <span>{downloading ? 'Preparing Bundle...' : 'Download for PC'}</span>
            </button>
          </div>
        </div>

        {/* Dedicated habitat telemetry channel legend (Blue / Gold / Rose reserved strictly for indicators) */}
        <div className="mt-16 grid grid-cols-1 gap-6 border-t border-white/[0.08] pt-8 sm:grid-cols-3">
          <div>
            <div className="flex items-center gap-2 text-xs font-mono uppercase tracking-wider text-[#6FC3E8]">
              <span>Air Meter · O₂ Scrubber</span>
            </div>
            <p className="mt-1.5 text-sm text-[#9AA4B8]">
              Atmospheric pressure below 800 Pa triggers dust storms and taxes dome air life support.
            </p>
          </div>

          <div>
            <div className="flex items-center gap-2 text-xs font-mono uppercase tracking-wider text-[#FFD166]">
              <span>Power · Solar Array</span>
            </div>
            <p className="mt-1.5 text-sm text-[#9AA4B8]">
              Dust accumulation and darkened skies cut solar irradiance even when panels are brushed.
            </p>
          </div>

          <div>
            <div className="flex items-center gap-2 text-xs font-mono uppercase tracking-wider text-[#E06C5A]">
              <span>Radiation · Regolith Shield</span>
            </div>
            <p className="mt-1.5 text-sm text-[#9AA4B8]">
              C-class and X-class solar flares demand real mass shielding with regolith and water ice.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
};

import React from 'react';
import { Play } from 'lucide-react';

interface NavbarProps {
  onPlayClick: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({ onPlayClick }) => {
  return (
    <header className="sticky top-0 z-40 border-b border-white/[0.08] bg-[#0B0E14]/90 backdrop-blur-md">
      <div className="mx-auto flex max-w-[1200px] items-center justify-between gap-8 px-6 py-3.5">
        {/* Zone 1: Single-element brand wordmark */}
        <a
          href="#top"
          className="font-display text-lg font-bold tracking-tight text-[#F1F4F8] whitespace-nowrap shrink-0 focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-[#C89B6A]"
        >
          AREON
        </a>

        {/* Zone 2: Concise single-line navigation links */}
        <nav
          aria-label="Primary navigation"
          className="hidden md:flex items-center gap-7 text-sm font-medium text-[#9AA4B8]"
        >
          <a
            href="#game-player"
            className="hover:text-[#F1F4F8] hover:underline underline-offset-4 decoration-[#C89B6A] transition-colors whitespace-nowrap shrink-0"
          >
            Game Hub
          </a>
          <a
            href="#mission-control"
            className="hover:text-[#F1F4F8] hover:underline underline-offset-4 decoration-[#C89B6A] transition-colors whitespace-nowrap shrink-0"
          >
            Mission Control
          </a>
          <a
            href="#scenarios"
            className="hover:text-[#F1F4F8] hover:underline underline-offset-4 decoration-[#C89B6A] transition-colors whitespace-nowrap shrink-0"
          >
            Scenarios
          </a>
          <a
            href="#no-fail"
            className="hover:text-[#F1F4F8] hover:underline underline-offset-4 decoration-[#C89B6A] transition-colors whitespace-nowrap shrink-0"
          >
            No-Fail Design
          </a>
          <a
            href="#science"
            className="hover:text-[#F1F4F8] hover:underline underline-offset-4 decoration-[#C89B6A] transition-colors whitespace-nowrap shrink-0"
          >
            Science
          </a>
        </nav>

        {/* Zone 3: Single primary action */}
        <div className="flex items-center shrink-0">
          <button
            type="button"
            onClick={onPlayClick}
            className="inline-flex items-center gap-2 rounded-lg bg-[#C89B6A] px-4 py-2 text-xs font-semibold text-[#0B0E14] transition-colors hover:bg-[#D8AD7E] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#C89B6A] whitespace-nowrap shrink-0 cursor-pointer"
          >
            <Play className="h-3.5 w-3.5 fill-current" aria-hidden="true" />
            <span>Play in Browser</span>
          </button>
        </div>
      </div>
    </header>
  );
};

import React from 'react';

interface RadarAxis {
  label: string;
  value: number;
  unitReadout: string;
}

interface TelemetryRadarChartProps {
  axes: RadarAxis[];
  stormActive: boolean;
}

export const TelemetryRadarChart: React.FC<TelemetryRadarChartProps> = ({
  axes,
  stormActive,
}) => {
  const size = 270;
  const cx = size / 2;
  const cy = size / 2;
  const maxR = 82;
  const count = axes.length;

  const pointAt = (index: number, pct: number) => {
    const angle = -Math.PI / 2 + (index * 2 * Math.PI) / count;
    const r = (Math.max(0, Math.min(100, pct)) / 100) * maxR;
    return {
      x: cx + Math.cos(angle) * r,
      y: cy + Math.sin(angle) * r,
    };
  };

  const rings = [25, 50, 75, 100];
  const polygonPoints = axes
    .map((axis, idx) => {
      const p = pointAt(idx, axis.value);
      return `${p.x.toFixed(1)},${p.y.toFixed(1)}`;
    })
    .join(' ');

  const accentStroke = stormActive ? '#E06C5A' : '#6FC3E8';
  const accentFill = stormActive ? 'rgba(224, 108, 90, 0.20)' : 'rgba(111, 195, 232, 0.20)';

  return (
    <div className="flex flex-col items-center">
      <svg
        viewBox={`0 0 ${size} ${size}`}
        className="w-full max-w-[270px] overflow-visible"
        role="img"
        aria-label="Habitat engineering survival envelope radar chart"
      >
        {/* Concentric calibration polygons */}
        {rings.map((ring) => {
          const pts = axes
            .map((_, idx) => {
              const p = pointAt(idx, ring);
              return `${p.x.toFixed(1)},${p.y.toFixed(1)}`;
            })
            .join(' ');
          return (
            <polygon
              key={ring}
              points={pts}
              fill="none"
              stroke="rgba(255,255,255,0.09)"
              strokeWidth={1}
            />
          );
        })}

        {/* Radial spoke lines */}
        {axes.map((_, idx) => {
          const outer = pointAt(idx, 100);
          return (
            <line
              key={idx}
              x1={cx}
              y1={cy}
              x2={outer.x}
              y2={outer.y}
              stroke="rgba(255,255,255,0.09)"
              strokeWidth={1}
            />
          );
        })}

        {/* Active calibration envelope */}
        <polygon
          points={polygonPoints}
          fill={accentFill}
          stroke={accentStroke}
          strokeWidth={2}
          strokeLinejoin="round"
          style={{ transition: 'all 240ms cubic-bezier(0.16, 1, 0.3, 1)' }}
        />

        {/* Vertex dots and axis labels */}
        {axes.map((axis, idx) => {
          const pt = pointAt(idx, axis.value);
          const labelPt = pointAt(idx, 126);
          const anchor =
            Math.abs(labelPt.x - cx) < 10
              ? 'middle'
              : labelPt.x > cx
                ? 'start'
                : 'end';

          return (
            <g key={axis.label}>
              <circle
                cx={pt.x}
                cy={pt.y}
                r={3.5}
                fill={accentStroke}
                stroke="#07090E"
                strokeWidth={1.5}
                style={{ transition: 'all 240ms cubic-bezier(0.16, 1, 0.3, 1)' }}
              />
              <text
                x={labelPt.x}
                y={labelPt.y - 3}
                textAnchor={anchor}
                className="fill-[#9AA4B8] font-mono text-[9px] tracking-wider"
              >
                {axis.label}
              </text>
              <text
                x={labelPt.x}
                y={labelPt.y + 9}
                textAnchor={anchor}
                className="fill-[#F1F4F8] font-mono text-[10px] font-semibold tabular-nums"
              >
                {axis.unitReadout}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
};

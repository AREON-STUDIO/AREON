export interface TelemetryRow {
  day: number;
  sol: number;
  pressurePa: number;
  flareClass: string;
}

export interface StormEvaluation {
  pressurePa: number;
  flareClass: string;
  lowPressure: boolean;
  hasFlare: boolean;
  stormTriggered: boolean;
  pressureDeltaFromThreshold: number;
}

export type GamePlayerState = 'idle' | 'loading' | 'active';

export interface SolEditorialBrief {
  sol: number;
  title: string;
  summary: string;
  challengeType: string;
}

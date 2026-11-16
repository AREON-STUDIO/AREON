import { useCallback, useEffect, useState } from 'react';
import { Navbar } from './components/Navbar';
import { HeroSection } from './components/HeroSection';
import { GamePlayerSection } from './components/GamePlayerSection';
import { MissionControlSection } from './components/MissionControlSection';
import { EducationalSection } from './components/EducationalSection';
import { FooterCompliance } from './components/FooterCompliance';
import { GamePlayerState, TelemetryRow } from './types/telemetry';
import { parseTelemetryCsv } from './utils/csvTelemetry';
export default function App() {
  const [rawCsv, setRawCsv] = useState<string>('');
  const [rows, setRows] = useState<TelemetryRow[]>([]);
  const [selectedSol, setSelectedSol] = useState<number>(800);
  const [customPressureInput, setCustomPressureInput] = useState<string>('887.38');
  const [loadingCsv, setLoadingCsv] = useState<boolean>(true);
  const [csvError, setCsvError] = useState<string | null>(null);
  const [lastFetchedAt, setLastFetchedAt] = useState<string | null>(null);
  const [isCsvSandboxActive, setIsCsvSandboxActive] = useState<boolean>(false);
  const [playerState, setPlayerState] = useState<GamePlayerState>('idle');

  const fetchTelemetryCsv = useCallback(async () => {
    setLoadingCsv(true);
    setCsvError(null);
    try {
      const response = await fetch('/data.csv', { cache: 'no-store' });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status} while fetching /data.csv`);
      }
      const text = await response.text();
      const parsed = parseTelemetryCsv(text);

      setRawCsv(text.trim());
      setRows(parsed);
      setIsCsvSandboxActive(false);
      setLastFetchedAt(
        new Date().toLocaleTimeString([], {
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
        })
      );

      setSelectedSol((prevSol) => {
        const match = parsed.find((r) => r.sol === prevSol) ?? parsed[0];
        if (match) {
          setCustomPressureInput(match.pressurePa.toFixed(2));
          return match.sol;
        }
        return prevSol;
      });
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Unable to parse /data.csv';
      setCsvError(message);
    } finally {
      setLoadingCsv(false);
    }
  }, []);

  useEffect(() => {
    fetchTelemetryCsv();
  }, [fetchTelemetryCsv]);

  const handleSelectSol = useCallback(
    (sol: number) => {
      setSelectedSol(sol);
      const found = rows.find((r) => r.sol === sol);
      if (found) {
        setCustomPressureInput(found.pressurePa.toFixed(2));
      }
    },
    [rows]
  );

  const handleResetCustomPressure = useCallback(() => {
    const found = rows.find((r) => r.sol === selectedSol);
    if (found) {
      setCustomPressureInput(found.pressurePa.toFixed(2));
    }
  }, [rows, selectedSol]);

  const handleApplyCsvSandbox = useCallback((editedCsv: string): string | null => {
    try {
      const parsed = parseTelemetryCsv(editedCsv);
      setRawCsv(editedCsv.trim());
      setRows(parsed);
      setIsCsvSandboxActive(true);
      const newest = parsed[parsed.length - 1];
      if (newest) {
        setSelectedSol(newest.sol);
        setCustomPressureInput(newest.pressurePa.toFixed(2));
      }
      return null;
    } catch (err) {
      return err instanceof Error ? err.message : 'Invalid CSV format.';
    }
  }, []);

const handlePlayInBrowser = useCallback(() => {
  const el = document.getElementById('game-player');

  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  setPlayerState('active');
}, []);

  const handleInspectSolFromEducation = useCallback(
    (sol: number) => {
      handleSelectSol(sol);
      const el = document.getElementById('mission-control');
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }
    },
    [handleSelectSol]
  );

  const handleDownloadPc = useCallback(() => {
  window.open(
    'https://github.com/AREON-STUDIO/AREON/releases/download/v1.0/Builds.zip',
    '_blank',
    'noopener,noreferrer'
  );
}, []);

  const handleCompleteLoading = useCallback(() => {
    setPlayerState('active');
  }, []);

  return (
    <div className="min-h-screen bg-[#0B0E14] text-[#F1F4F8]">
      <Navbar onPlayClick={handlePlayInBrowser} />

      <main>
        <HeroSection
          onPlayClick={handlePlayInBrowser}
          onDownloadPc={handleDownloadPc}
        />

        <GamePlayerSection
          playerState={playerState}
          rows={rows}
          selectedSol={selectedSol}
          onStartRequest={() => setPlayerState('loading')}
          onReturnStandby={() => setPlayerState('idle')}
          onCompleteLoading={handleCompleteLoading}
        />

        <MissionControlSection
          rawCsv={rawCsv}
          rows={rows}
          selectedSol={selectedSol}
          customPressureInput={customPressureInput}
          loading={loadingCsv}
          error={csvError}
          lastFetchedAt={lastFetchedAt}
          isCsvSandboxActive={isCsvSandboxActive}
          onSelectSol={handleSelectSol}
          onCustomPressureChange={setCustomPressureInput}
          onResetCustomPressure={handleResetCustomPressure}
          onRefetchCsv={fetchTelemetryCsv}
          onApplyCsvSandbox={handleApplyCsvSandbox}
        />

        <EducationalSection
          rows={rows}
          selectedSol={selectedSol}
          onInspectSol={handleInspectSolFromEducation}
        />
      </main>

      <FooterCompliance />
    </div>
  );
}

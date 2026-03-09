import { AggregatedSignal, IndicatorResult, SignalType } from '../types';
import { rsiIndicator } from '../indicators/rsi';
import { macdIndicator } from '../indicators/macd';
import { bollingerBandsIndicator } from '../indicators/bollinger';
import { emaIndicator } from '../indicators/ema';

const WEIGHTS = {
  rsi: 0.30,
  macd: 0.30,
  bollinger: 0.20,
  ema: 0.20,
};

function signalScore(result: IndicatorResult): number {
  // BUY = +1, SELL = -1, NEUTRAL = 0 — scaled by strength
  if (result.signal === 'BUY') return result.strength;
  if (result.signal === 'SELL') return -result.strength;
  return 0;
}

export function aggregateSignals(closePrices: number[]): AggregatedSignal {
  const rsi = rsiIndicator(closePrices);
  const macd = macdIndicator(closePrices);
  const bollinger = bollingerBandsIndicator(closePrices);
  const ema = emaIndicator(closePrices);

  const weightedScore =
    signalScore(rsi) * WEIGHTS.rsi +
    signalScore(macd) * WEIGHTS.macd +
    signalScore(bollinger) * WEIGHTS.bollinger +
    signalScore(ema) * WEIGHTS.ema;

  const confidence = Math.abs(weightedScore);

  let signal: SignalType = 'NEUTRAL';
  if (weightedScore > 0.1) signal = 'BUY';
  else if (weightedScore < -0.1) signal = 'SELL';

  return {
    signal,
    strength: confidence,
    confidence,
    components: { rsi, macd, bollinger, ema },
  };
}

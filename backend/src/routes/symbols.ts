import { Router, Request, Response, NextFunction } from 'express';
import { authMiddleware } from '../middleware/auth';
import { getAllSymbols, getSymbolByTicker, getPriceHistory } from '../services/symbolService';
import { aggregateSignals } from '../bot/signalAggregator';

const router = Router();

router.use(authMiddleware);

router.get('/', async (_req: Request, res: Response, next: NextFunction) => {
  try {
    const symbols = await getAllSymbols();
    res.json(symbols);
  } catch (err) {
    next(err);
  }
});

router.get('/:ticker', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const symbol = await getSymbolByTicker(req.params.ticker);
    res.json(symbol);
  } catch (err) {
    next(err);
  }
});

router.get('/:ticker/candles', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const limit = parseInt(req.query.limit as string) || 200;
    const candles = await getPriceHistory(req.params.ticker, limit);
    res.json(candles);
  } catch (err) {
    next(err);
  }
});

router.get('/:ticker/signals', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const candles = await getPriceHistory(req.params.ticker, 200);
    if (candles.length < 30) {
      res.json({ signal: 'NEUTRAL', strength: 0, confidence: 0, components: {} });
      return;
    }
    const prices = candles.map(c => c.close);
    const signal = aggregateSignals(prices);
    res.json(signal);
  } catch (err) {
    next(err);
  }
});

export default router;

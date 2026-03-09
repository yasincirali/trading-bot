import { Router, Request, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { z } from 'zod';
import { authMiddleware } from '../middleware/auth';
import { startBot, stopBot, isBotRunning } from '../bot/tradingEngine';
import { createError } from '../middleware/errorHandler';

const router = Router();
const prisma = new PrismaClient();

router.use(authMiddleware);

// io is injected via app.locals in server.ts
router.get('/status', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const config = await prisma.botConfig.findUnique({ where: { userId } });
    res.json({
      running: isBotRunning(userId),
      config,
      paperTrading: process.env.PAPER_TRADING_MODE !== 'false',
    });
  } catch (err) {
    next(err);
  }
});

router.post('/start', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const io = req.app.get('io');

    await prisma.botConfig.update({ where: { userId }, data: { enabled: true } });
    startBot(userId, io);

    res.json({ running: true, message: 'Bot started' });
  } catch (err) {
    next(err);
  }
});

router.post('/stop', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const io = req.app.get('io');

    await prisma.botConfig.update({ where: { userId }, data: { enabled: false } });
    stopBot(userId, io);

    res.json({ running: false, message: 'Bot stopped' });
  } catch (err) {
    next(err);
  }
});

const configSchema = z.object({
  confidenceThreshold: z.number().min(0).max(1).optional(),
  maxOrderSizeTry: z.number().positive().optional(),
  dailyLossLimitTry: z.number().positive().optional(),
  tickIntervalMs: z.number().int().min(1000).optional(),
  watchlist: z.array(z.string()).optional(),
});

router.get('/config', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const config = await prisma.botConfig.findUnique({ where: { userId: req.user!.userId } });
    if (!config) throw createError('Bot config not found', 404);
    res.json(config);
  } catch (err) {
    next(err);
  }
});

router.put('/config', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const body = configSchema.parse(req.body);
    const config = await prisma.botConfig.update({
      where: { userId: req.user!.userId },
      data: body,
    });
    res.json(config);
  } catch (err) {
    next(err);
  }
});

export default router;

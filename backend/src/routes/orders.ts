import { Router, Request, Response, NextFunction } from 'express';
import { z } from 'zod';
import { authMiddleware } from '../middleware/auth';
import { getOrders, placeManualOrder, cancelOrder } from '../services/tradingService';

const router = Router();

router.use(authMiddleware);

const orderSchema = z.object({
  ticker: z.string().min(1).max(10),
  type: z.enum(['BUY', 'SELL']),
  quantity: z.number().positive(),
  bankAdapter: z.enum(['denizbank', 'akbank', 'yapikredi']).optional(),
});

router.get('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const limit = parseInt(req.query.limit as string) || 50;
    const orders = await getOrders(req.user!.userId, limit);
    res.json(orders);
  } catch (err) {
    next(err);
  }
});

router.post('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const body = orderSchema.parse(req.body);
    const order = await placeManualOrder(
      req.user!.userId,
      body.ticker,
      body.type,
      body.quantity,
      body.bankAdapter
    );
    res.status(201).json(order);
  } catch (err) {
    next(err);
  }
});

router.delete('/:id', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const order = await cancelOrder(req.user!.userId, req.params.id);
    res.json(order);
  } catch (err) {
    next(err);
  }
});

export default router;

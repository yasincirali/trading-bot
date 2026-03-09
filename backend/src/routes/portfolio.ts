import { Router, Request, Response, NextFunction } from 'express';
import { authMiddleware } from '../middleware/auth';
import { getPortfolio } from '../services/tradingService';

const router = Router();

router.use(authMiddleware);

router.get('/', async (req: Request, res: Response, next: NextFunction) => {
  try {
    const portfolio = await getPortfolio(req.user!.userId);
    res.json(portfolio);
  } catch (err) {
    next(err);
  }
});

export default router;

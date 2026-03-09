import { Router } from 'express';
import authRoutes from './auth';
import symbolRoutes from './symbols';
import orderRoutes from './orders';
import portfolioRoutes from './portfolio';
import botRoutes from './bot';

const router = Router();

router.use('/auth', authRoutes);
router.use('/symbols', symbolRoutes);
router.use('/orders', orderRoutes);
router.use('/portfolio', portfolioRoutes);
router.use('/bot', botRoutes);

router.get('/health', (_req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

export default router;

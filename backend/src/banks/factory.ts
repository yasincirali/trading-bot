import { BankAdapter } from './baseAdapter';
import { DenizBankAdapter } from './adapters/denizbank';
import { AkbankAdapter } from './adapters/akbank';
import { YapiKrediAdapter } from './adapters/yapikredi';
import { BankName } from '../types';

const instances = new Map<BankName, BankAdapter>();

export function getBankAdapter(bank: BankName = 'mock'): BankAdapter {
  const mode = process.env.BANK_MODE ?? 'mock';

  if (mode === 'live') {
    console.warn('[BankFactory] BANK_MODE=live but live adapters are not implemented. Falling back to mock.');
  }

  const key = bank === 'mock' ? 'denizbank' : bank;

  if (!instances.has(key as BankName)) {
    switch (key) {
      case 'akbank':
        instances.set('akbank', new AkbankAdapter());
        break;
      case 'yapikredi':
        instances.set('yapikredi', new YapiKrediAdapter());
        break;
      case 'denizbank':
      default:
        instances.set('denizbank', new DenizBankAdapter());
        break;
    }
  }

  return instances.get(key as BankName)!;
}

export function getDefaultAdapter(): BankAdapter {
  return getBankAdapter('denizbank');
}

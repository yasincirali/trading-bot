import * as signalR from '@microsoft/signalr';
import { BotTickEvent, PriceUpdateEvent } from '../types';

let connection: signalR.HubConnection | null = null;

function buildConnection(): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl('/tradingHub', {
      accessTokenFactory: () => localStorage.getItem('accessToken') ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();
}

export function getConnection(): signalR.HubConnection {
  if (!connection) connection = buildConnection();
  return connection;
}

export async function connectSocket(_token: string): Promise<void> {
  const conn = getConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start();
    console.log('[SignalR] Connected');
  }
}

export function disconnectSocket(): void {
  connection?.stop();
  connection = null;
}

export function onBotTick(cb: (event: BotTickEvent) => void) {
  getConnection().on('BotTick', cb);
  return () => getConnection().off('BotTick', cb);
}

export function onBotStatus(cb: (status: { running: boolean }) => void) {
  getConnection().on('BotStatus', cb);
  return () => getConnection().off('BotStatus', cb);
}

export function onPriceUpdate(cb: (event: PriceUpdateEvent) => void) {
  getConnection().on('PriceUpdate', cb);
  return () => getConnection().off('PriceUpdate', cb);
}

export function subscribeSymbol(ticker: string) {
  getConnection().invoke('SubscribeSymbol', ticker).catch(console.error);
}

export function unsubscribeSymbol(ticker: string) {
  getConnection().invoke('UnsubscribeSymbol', ticker).catch(console.error);
}

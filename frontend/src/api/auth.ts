import client from './client';
import { User, AuthTokens } from '../types';

export const authApi = {
  async register(email: string, password: string, name: string, phone?: string) {
    const { data } = await client.post<{ user: User; tokens: AuthTokens }>('/auth/register', {
      email, password, name, phone,
    });
    return data;
  },

  async login(email: string, password: string) {
    const { data } = await client.post<{ user: User; tokens: AuthTokens }>('/auth/login', {
      email, password,
    });
    return data;
  },

  async me() {
    const { data } = await client.get<User>('/auth/me');
    return data;
  },
};

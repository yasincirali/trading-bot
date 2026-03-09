import { BotConfigForm } from '../components/bot/BotConfigForm';
import { useAuthStore } from '../stores/authStore';

export function SettingsPage() {
  const { user } = useAuthStore();

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold text-gray-100">Settings</h1>
        <p className="text-gray-500 text-sm mt-0.5">Configure bot behavior</p>
      </div>

      <BotConfigForm />

      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wide mb-4">Account</h2>
        <div className="space-y-2 text-sm">
          <div className="flex gap-4">
            <span className="text-gray-500 w-24">Name</span>
            <span className="text-gray-200">{user?.name}</span>
          </div>
          <div className="flex gap-4">
            <span className="text-gray-500 w-24">Email</span>
            <span className="text-gray-200">{user?.email}</span>
          </div>
          <div className="flex gap-4">
            <span className="text-gray-500 w-24">Phone</span>
            <span className="text-gray-200">{user?.phone ?? '—'}</span>
          </div>
          <div className="flex gap-4">
            <span className="text-gray-500 w-24">Role</span>
            <span className="text-gray-200">{user?.role}</span>
          </div>
        </div>
      </div>
    </div>
  );
}

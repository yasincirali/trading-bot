import { useUiStore } from '../../stores/uiStore';

export function ToastContainer() {
  const { toasts, removeToast } = useUiStore();

  if (toasts.length === 0) return null;

  return (
    <div className="fixed top-4 right-4 z-50 flex flex-col gap-2">
      {toasts.map(toast => (
        <div
          key={toast.id}
          onClick={() => removeToast(toast.id)}
          className={`flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg cursor-pointer min-w-64 max-w-sm text-sm font-medium transition-all
            ${toast.type === 'success' ? 'bg-green-900 text-green-200 border border-green-700' :
              toast.type === 'error' ? 'bg-red-900 text-red-200 border border-red-700' :
              toast.type === 'warning' ? 'bg-yellow-900 text-yellow-200 border border-yellow-700' :
              'bg-blue-900 text-blue-200 border border-blue-700'}`}
        >
          <span className="flex-1">{toast.message}</span>
          <span className="text-xs opacity-60">✕</span>
        </div>
      ))}
    </div>
  );
}

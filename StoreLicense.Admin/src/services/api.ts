
import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5263',
  headers: {
    'Content-Type': 'application/json',
  },
});

// إضافة رمز المصادقة تلقائياً إلى الطلبات
api.interceptors.request.use(
  (config) => {
    const token = sessionStorage.getItem('admin_access_token');

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => Promise.reject(error),
);

export default api;


import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
} from 'react-router-dom';

import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Subscriptions from './pages/Subscriptions';
import SubscriptionDetails from './pages/SubscriptionDetails';
import Customers from './pages/Customers';
import SubscriptionPlans from './pages/SubscriptionPlans';
import AdminLayout from './layouts/AdminLayout';
import Devices from './pages/Devices';
import Admins from './pages/Admins';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

function ProtectedRoute({ children }: ProtectedRouteProps) {
  const token = sessionStorage.getItem('admin_access_token');

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  return (
    <AdminLayout>
      {children}
    </AdminLayout>
  );
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />

        <Route
          path="/"
          element={
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          }
        />

        <Route
          path="/subscriptions"
          element={
            <ProtectedRoute>
              <Subscriptions />
            </ProtectedRoute>
          }
        />

        <Route
          path="/subscriptions/:id"
          element={
            <ProtectedRoute>
              <SubscriptionDetails />
            </ProtectedRoute>
          }
        />

        <Route
          path="/customers"
          element={
            <ProtectedRoute>
              <Customers />
            </ProtectedRoute>
          }
        />

        <Route
          path="/plans"
          element={
            <ProtectedRoute>
              <SubscriptionPlans />
            </ProtectedRoute>
          }
        />


        <Route
          path="/devices"
          element={
            <ProtectedRoute>
              <Devices />
            </ProtectedRoute>
          }
        />

        <Route
          path="/admins"
          element={
            <ProtectedRoute>
              <Admins />
            </ProtectedRoute>
          }
        />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;

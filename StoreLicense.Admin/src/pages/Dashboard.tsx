
import { useCallback, useEffect, useState } from 'react';

import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material';

import {
  PeopleAltRounded,
  SubscriptionsRounded,
  DevicesRounded,
  CheckCircleRounded,
  AutorenewRounded,
} from '@mui/icons-material';

import api from '../services/api';

type Status = number | string;

interface Subscriber {
  id: string;
  storeName: string;
  ownerName: string;
  status: Status;
}

interface Subscription {
  subscriptionId: string;
  status: Status;
  registeredDevicesCount: number;
  activeDevicesCount: number;
}

interface DashboardStatistics {
  totalSubscribers: number;
  activeSubscribers: number;
  totalSubscriptions: number;
  activeSubscriptions: number;
  totalDevices: number;
  activeDevices: number;
}

const initialStatistics: DashboardStatistics = {
  totalSubscribers: 0,
  activeSubscribers: 0,
  totalSubscriptions: 0,
  activeSubscriptions: 0,
  totalDevices: 0,
  activeDevices: 0,
};

function isActive(status: Status): boolean {
  return String(status) === '1' || String(status) === 'Active';
}

export default function Dashboard() {
  const [statistics, setStatistics] =
    useState<DashboardStatistics>(initialStatistics);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadDashboard = useCallback(async () => {
    try {
      setLoading(true);
      setError('');

      const [subscribersResponse, subscriptionsResponse] =
        await Promise.all([
          api.get<Subscriber[]>('/api/subscribers'),
          api.get<Subscription[]>('/api/subscriptions'),
        ]);

      const subscribers = subscribersResponse.data;
      const subscriptions = subscriptionsResponse.data;

      setStatistics({
        totalSubscribers: subscribers.length,

        activeSubscribers: subscribers.filter(
          (item) => isActive(item.status),
        ).length,

        totalSubscriptions: subscriptions.length,

        activeSubscriptions: subscriptions.filter(
          (item) => isActive(item.status),
        ).length,

        totalDevices: subscriptions.reduce(
          (total, item) =>
            total + (item.registeredDevicesCount || 0),
          0,
        ),

        activeDevices: subscriptions.reduce(
          (total, item) =>
            total + (item.activeDevicesCount || 0),
          0,
        ),
      });
    } catch {
      setError('تعذر تحميل بيانات لوحة المعلومات. تحقق من اتصال الخادم وحاول مرة أخرى.');
    } finally {
      setLoading(false);
    }
  }, []);
  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);
  const cards = [
    {
      title: 'إجمالي العملاء',
      value: statistics.totalSubscribers,
      description: 'جميع العملاء المسجلين',
      icon: <PeopleAltRounded />,
      color: '#2563EB',
      background: '#EFF6FF',
    },
    {
      title: 'العملاء النشطون',
      value: statistics.activeSubscribers,
      description: 'العملاء الذين يمكنهم استخدام النظام',
      icon: <CheckCircleRounded />,
      color: '#059669',
      background: '#ECFDF5',
    },
    {
      title: 'إجمالي الاشتراكات',
      value: statistics.totalSubscriptions,
      description: 'جميع الاشتراكات المسجلة',
      icon: <SubscriptionsRounded />,
      color: '#7C3AED',
      background: '#F5F3FF',
    },
    {
      title: 'الاشتراكات النشطة',
      value: statistics.activeSubscriptions,
      description: 'الاشتراكات النشطة حاليًا',
      icon: <CheckCircleRounded />,
      color: '#059669',
      background: '#ECFDF5',
    },
    {
      title: 'الأجهزة المسجلة',
      value: statistics.totalDevices,
      description: 'إجمالي الأجهزة المرتبطة بالاشتراكات',
      icon: <DevicesRounded />,
      color: '#D97706',
      background: '#FFFBEB',
    },
    {
      title: 'الأجهزة النشطة',
      value: statistics.activeDevices,
      description: 'الأجهزة التي لا يزال تفعيلها قائمًا',
      icon: <CheckCircleRounded />,
      color: '#0891B2',
      background: '#ECFEFF',
    },
  ];

  return (
    <Stack spacing={3}>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 2,
        }}
      >
        <Box>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            نظرة عامة
          </Typography>

          <Typography
            color="text.secondary"
            sx={{ fontSize: 14, mt: 0.5 }}
          >
            ملخص بيانات العملاء والاشتراكات والأجهزة في النظام.
          </Typography>
        </Box>

        <Button
          variant="outlined"
          startIcon={<AutorenewRounded />}
          onClick={() => void loadDashboard()}
          disabled={loading}
        >
          تحديث البيانات
        </Button>
      </Box>

      {error && (
        <Alert
          severity="error"
          action={
            <Button
              color="inherit"
              size="small"
              onClick={() => void loadDashboard()}
            >
              إعادة المحاولة
            </Button>
          }
        >
          {error}
        </Alert>
      )}

      {loading ? (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            minHeight: 250,
          }}
        >
          <CircularProgress />
        </Box>
      ) : !error ? (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: {
              xs: '1fr',
              sm: 'repeat(2, 1fr)',
              lg: 'repeat(3, 1fr)',
            },
            gap: 2,
          }}
        >
          {cards.map((item) => (
            <Card
              key={item.title}
              elevation={0}
              sx={{
                border: '1px solid',
                borderColor: 'divider',
                borderRadius: 3,
              }}
            >
              <CardContent sx={{ p: 2.5 }}>
                <Stack
                  direction="row"
                  sx={{
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    mb: 2,
                  }}
                >
                  <Avatar
                    variant="rounded"
                    sx={{
                      bgcolor: item.background,
                      color: item.color,
                      width: 46,
                      height: 46,
                    }}
                  >
                    {item.icon}
                  </Avatar>
                </Stack>
                <Typography
                  color="text.secondary"
                  sx={{ fontSize: 13 }}
                >
                  {item.title}
                </Typography>
                <Typography
                  variant="h4"
                  sx={{
                    fontWeight: 700,
                    mt: 0.5,
                    mb: 0.5,
                  }}
                >
                  {item.value.toLocaleString('ar-YE')}
                </Typography>
                <Typography
                  color="text.secondary"
                  sx={{ fontSize: 11 }}
                >
                  {item.description}
                </Typography>
              </CardContent>
            </Card>
          ))}
        </Box>
      ) : null}
    </Stack>
  );
}

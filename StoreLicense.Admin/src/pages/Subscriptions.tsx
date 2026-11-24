
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputAdornment,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';

import {
  AddRounded,
  AutorenewRounded,
  SearchRounded,
  SubscriptionsRounded,
  CheckCircleRounded,
  PendingActionsRounded,
  EventBusyRounded,
  ContentCopyRounded,
} from '@mui/icons-material';

import api from '../services/api';

type SubscriptionStatus = number | string;

interface Subscription {
  subscriptionId: string;
  subscriberId: string;
  storeName: string;
  ownerName: string;
  planId: string | null;
  planName: string | null;
  isLifetime: boolean;
  durationDays: number | null;
  maxDevices: number;
  registeredDevicesCount: number;
  activeDevicesCount: number;
  status: SubscriptionStatus;
  startDate: string | null;
  endDate: string | null;
  createdAt: string;
}

interface Subscriber {
  id: string;
  storeName: string;
  ownerName: string;
  phone: string | null;
  status: number | string;
}

interface SubscriptionPlan {
  id: string;
  name: string;
  durationDays: number | null;
  isLifetime: boolean;
  defaultMaxDevices: number;
  isActive: boolean;
}

interface CreateSubscriptionResponse {
  subscriptionId: string;
  subscriberId: string;
  storeName: string;
  planId: string;
  planName: string;
  isLifetime: boolean;
  durationDays: number | null;
  maxDevices: number;
  status: SubscriptionStatus;
  startDate: string | null;
  endDate: string | null;
  activationCode: string;
  activationCodeExpiresAt: string | null;
  createdAt: string;
}

type StatusFilter = 'all' | '0' | '1' | '2' | '3' | '4';

const statusInfo: Record<
  string,
  { label: string; color: 'default' | 'success' | 'warning' | 'error' | 'info' }
> = {
  '0': { label: 'قيد الانتظار', color: 'warning' },
  '1': { label: 'نشط', color: 'success' },
  '2': { label: 'موقوف', color: 'error' },
  '3': { label: 'منتهي', color: 'default' },
  '4': { label: 'ملغي', color: 'error' },
  Pending: { label: 'قيد الانتظار', color: 'warning' },
  Active: { label: 'نشط', color: 'success' },
  Suspended: { label: 'موقوف', color: 'error' },
  Expired: { label: 'منتهي', color: 'default' },
  Cancelled: { label: 'ملغي', color: 'error' },
};

function getStatusKey(status: SubscriptionStatus): string {
  return String(status);
}

function getStatusInfo(status: SubscriptionStatus) {
  return statusInfo[getStatusKey(status)] ?? {
    label: 'غير معروف',
    color: 'default' as const,
  };
}

function formatDate(value: string | null): string {
  if (!value) return '—';

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) return '—';

  return new Intl.DateTimeFormat('ar-YE', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(date);
}

function getErrorMessage(err: unknown, fallback: string): string {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const axiosError = err as {
      response?: {
        status?: number;
        data?: {
          message?: string;
          title?: string;
          errors?: Record<string, string[]>;
        };
      };
    };

    const data = axiosError.response?.data;

    if (axiosError.response?.status === 401) {
      return 'انتهت جلسة تسجيل الدخول أو أن صلاحية الوصول غير متاحة.';
    }

    if (data?.message) return data.message;
    if (data?.title) return data.title;

    if (data?.errors) {
      const firstError = Object.values(data.errors).flat()[0];

      if (firstError) return firstError;
    }
  }

  return fallback;
}

function isActiveSubscriber(status: number | string): boolean {
  return status === 1 || status === '1' || status === 'Active';
}

export default function Subscriptions() {
  const navigate = useNavigate();

  const [subscriptions, setSubscriptions] = useState<Subscription[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');

  // إلغاء الاشتراك
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
  const [subscriptionToCancel, setSubscriptionToCancel] =
    useState<Subscription | null>(null);
  const [cancelling, setCancelling] = useState(false);
  const [cancelError, setCancelError] = useState('');

  // إنشاء اشتراك
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [subscribers, setSubscribers] = useState<Subscriber[]>([]);
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [loadingOptions, setLoadingOptions] = useState(false);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState('');

  const [subscriberId, setSubscriberId] = useState('');
  const [planId, setPlanId] = useState('');
  const [maxDevices, setMaxDevices] = useState('1');
  const [activationCodeExpiresAt, setActivationCodeExpiresAt] = useState('');

  // نتيجة إنشاء الاشتراك
  const [createdResult, setCreatedResult] =
    useState<CreateSubscriptionResponse | null>(null);
  const [copyMessage, setCopyMessage] = useState('');

  const loadSubscriptions = async () => {
    try {
      setLoading(true);
      setError('');

      const response = await api.get<Subscription[]>('/api/subscriptions');

      setSubscriptions(response.data);
    } catch (err: unknown) {
      setError(
        getErrorMessage(err, 'تعذر تحميل الاشتراكات من الخادم.'),
      );
    } finally {
      setLoading(false);
    }
  };

    const openCancelDialog = (subscription: Subscription) => {
    setSubscriptionToCancel(subscription);
    setCancelError('');
    setCancelDialogOpen(true);
  };

  const closeCancelDialog = () => {
    if (cancelling) return;

    setCancelDialogOpen(false);
    setSubscriptionToCancel(null);
    setCancelError('');
  };

    const handleCancelSubscription = async () => {
    if (!subscriptionToCancel) return;

    try {
      setCancelling(true);
      setCancelError('');
      await api.post(
        `/api/subscriptions/${subscriptionToCancel.subscriptionId}/cancel`,
      );

      setCancelDialogOpen(false);
      setSubscriptionToCancel(null);

      await loadSubscriptions();
    } catch (err: unknown) {
      setCancelError(
        getErrorMessage(err, 'تعذر إلغاء الاشتراك. حاول مرة أخرى.'),
      );
    } finally {
      setCancelling(false);
    }
  };

  useEffect(() => {
    void loadSubscriptions();
  }, []);

  const resetCreateForm = () => {
    setSubscriberId('');
    setPlanId('');
    setMaxDevices('1');
    setActivationCodeExpiresAt('');
    setCreateError('');
    setCreatedResult(null);
    setCopyMessage('');
  };

  const openCreateDialog = async () => {
    resetCreateForm();
    setCreateDialogOpen(true);
    setLoadingOptions(true);

    try {
      const [subscribersResponse, plansResponse] = await Promise.all([
        api.get<Subscriber[]>('/api/subscribers'),
        api.get<SubscriptionPlan[]>('/api/subscription-plans'),
      ]);

      const activeSubscribers = subscribersResponse.data.filter(
        (item) => isActiveSubscriber(item.status),
      );

      const activePlans = plansResponse.data.filter(
        (item) => item.isActive,
      );

      setSubscribers(activeSubscribers);
      setPlans(activePlans);
    } catch (err: unknown) {
      setCreateError(
        getErrorMessage(err, 'تعذر تحميل العملاء وخطط الاشتراك.'),
      );
    } finally {
      setLoadingOptions(false);
    }
  };

  const closeCreateDialog = () => {
    if (creating) return;

    setCreateDialogOpen(false);
    resetCreateForm();
  };

  const handlePlanChange = (selectedPlanId: string) => {
    setPlanId(selectedPlanId);

    const selectedPlan = plans.find(
      (item) => item.id === selectedPlanId,
    );

    if (selectedPlan) {
      setMaxDevices(String(selectedPlan.defaultMaxDevices));
    }
  };

  const handleCreateSubscription = async () => {
    setCreateError('');

    if (!subscriberId) {
      setCreateError('يرجى اختيار العميل.');
      return;
    }

    if (!planId) {
      setCreateError('يرجى اختيار خطة الاشتراك.');
      return;
    }

    const devicesCount = Number(maxDevices);

    if (
      !Number.isInteger(devicesCount) ||
      devicesCount < 1 ||
      devicesCount > 1000
    ) {
      setCreateError('عدد الأجهزة يجب أن يكون بين 1 و1000.');
      return;
    }

    try {
      setCreating(true);

      const request = {
        subscriberId,
        planId,
        maxDevices: devicesCount,
        activationCodeExpiresAt: activationCodeExpiresAt
        ? `${activationCodeExpiresAt}T23:59:59.000Z`
        : null,
      };

      const response = await api.post<CreateSubscriptionResponse>(
        '/api/subscriptions',
        request,
      );

      setCreatedResult(response.data);

      // تحديث القائمة بعد نجاح الإنشاء
      void loadSubscriptions();
    } catch (err: unknown) {
      setCreateError(
        getErrorMessage(err, 'تعذر إنشاء الاشتراك. حاول مرة أخرى.'),
      );
    } finally {
      setCreating(false);
    }
  };

  const handleCopyActivationCode = async () => {
    if (!createdResult?.activationCode) return;

    try {
      await navigator.clipboard.writeText(
        createdResult.activationCode,
      );

      setCopyMessage('تم نسخ رمز التفعيل بنجاح.');
    } catch {
      setCopyMessage('تعذر النسخ تلقائيًا. يمكنك تحديد الرمز ونسخه يدويًا.');
    }
  };

  const statistics = useMemo(() => ({
    total: subscriptions.length,
    active: subscriptions.filter(
      (item) =>
        getStatusKey(item.status) === '1' ||
        getStatusKey(item.status) === 'Active',
    ).length,
    pending: subscriptions.filter(
      (item) =>
        getStatusKey(item.status) === '0' ||
        getStatusKey(item.status) === 'Pending',
    ).length,
    expired: subscriptions.filter(
      (item) =>
        getStatusKey(item.status) === '3' ||
        getStatusKey(item.status) === 'Expired',
    ).length,
  }), [subscriptions]);

  const filteredSubscriptions = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();

    return subscriptions.filter((item) => {
      const matchesSearch =
        !normalizedSearch ||
        item.storeName.toLowerCase().includes(normalizedSearch) ||
        item.ownerName.toLowerCase().includes(normalizedSearch) ||
        (item.planName ?? '').toLowerCase().includes(normalizedSearch);

      const statusNames: Record<string, string> = {
        '0': 'Pending',
        '1': 'Active',
        '2': 'Suspended',
        '3': 'Expired',
        '4': 'Cancelled',
      };

      const matchesStatus =
        statusFilter === 'all' ||
        getStatusKey(item.status) === statusFilter ||
        getStatusKey(item.status) === statusNames[statusFilter];

      return matchesSearch && matchesStatus;
    });
  }, [subscriptions, search, statusFilter]);

  const statCards = [
    {
      title: 'إجمالي الاشتراكات',
      value: statistics.total,
      icon: <SubscriptionsRounded />,
      color: '#2563EB',
      background: '#EFF6FF',
    },
    {
      title: 'الاشتراكات النشطة',
      value: statistics.active,
      icon: <CheckCircleRounded />,
      color: '#059669',
      background: '#ECFDF5',
    },
    {
      title: 'قيد الانتظار',
      value: statistics.pending,
      icon: <PendingActionsRounded />,
      color: '#D97706',
      background: '#FFFBEB',
    },
    {
      title: 'الاشتراكات المنتهية',
      value: statistics.expired,
      icon: <EventBusyRounded />,
      color: '#64748B',
      background: '#F1F5F9',
    },
  ];

  return (
    <Stack spacing={3}>
      {/* Page heading */}
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{
          justifyContent: 'space-between',
          alignItems: { xs: 'stretch', sm: 'center' },
        }}
      >
        <Box>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            الاشتراكات
          </Typography>

          <Typography
            color="text.secondary"
            sx={{ fontSize: 14, mt: 0.5 }}
          >
            إدارة اشتراكات العملاء ومتابعة حالتها والأجهزة المرتبطة بها.
          </Typography>
        </Box>

        <Stack direction="row" spacing={1}>
          <Button
            variant="outlined"
            startIcon={<AutorenewRounded />}
            onClick={() => void loadSubscriptions()}
            disabled={loading}
          >
            تحديث
          </Button>

          <Button
            variant="contained"
            startIcon={<AddRounded />}
            onClick={() => void openCreateDialog()}
          >
            اشتراك جديد
          </Button>
        </Stack>
      </Stack>

      {/* Statistics */}
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: {
            xs: 'repeat(2, 1fr)',
            md: 'repeat(4, 1fr)',
          },
          gap: 2,
        }}
      >
        {statCards.map((item) => (
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
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  mb: 2,
                }}
              >
                <Avatar
                  variant="rounded"
                  sx={{
                    bgcolor: item.background,
                    color: item.color,
                    width: 44,
                    height: 44,
                  }}
                >
                  {item.icon}
                </Avatar>
              </Stack>

              <Typography color="text.secondary" sx={{ fontSize: 13 }}>
                {item.title}
              </Typography>

              <Typography variant="h4" sx={{ fontWeight: 700, mt: 0.5 }}>
                {item.value}
              </Typography>
            </CardContent>
          </Card>
        ))}
      </Box>

      {/* Subscriptions table */}
      <Card
        elevation={0}
        sx={{
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 3,
        }}
      >
        <CardContent sx={{ p: 2.5 }}>
          <Typography sx={{ fontWeight: 700, fontSize: 17, mb: 0.5 }}>
            قائمة الاشتراكات
          </Typography>

          <Typography
            color="text.secondary"
            sx={{ fontSize: 12, mb: 2.5 }}
          >
            جميع الاشتراكات المسجلة في النظام. اضغط على أي اشتراك لعرض تفاصيله.
          </Typography>

          {/* Search and filter */}
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1.5}
            sx={{ mb: 2.5 }}
          >
            <TextField
              fullWidth
              size="small"
              placeholder="ابحث باسم المتجر أو المالك أو الخطة..."
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchRounded color="action" />
                    </InputAdornment>
                  ),
                },
              }}
            />

            <FormControl
              size="small"
              sx={{ minWidth: { xs: '100%', sm: 190 } }}
            >
              <InputLabel id="subscription-status-filter">
                حالة الاشتراك
              </InputLabel>

              <Select
                labelId="subscription-status-filter"
                value={statusFilter}
                label="حالة الاشتراك"
                onChange={(event) =>
                  setStatusFilter(event.target.value as StatusFilter)
                }
              >
                <MenuItem value="all">جميع الحالات</MenuItem>
                <MenuItem value="0">قيد الانتظار</MenuItem>
                <MenuItem value="1">نشط</MenuItem>
                <MenuItem value="2">موقوف</MenuItem>
                <MenuItem value="3">منتهي</MenuItem>
                <MenuItem value="4">ملغي</MenuItem>
              </Select>
            </FormControl>
          </Stack>

          {error && (
            <Alert
              severity="error"
              sx={{ mb: 2 }}
              action={
                <Button
                  color="inherit"
                  size="small"
                  onClick={() => void loadSubscriptions()}
                >
                  إعادة المحاولة
                </Button>
              }
            >
              {error}
            </Alert>
          )}

          {loading ? (
            <Stack
              spacing={1.5}
              sx={{
                alignItems: 'center',
                justifyContent: 'center',
                py: 8,
              }}
            >
              <CircularProgress size={32} />

              <Typography color="text.secondary" sx={{ fontSize: 13 }}>
                جارٍ تحميل الاشتراكات...
              </Typography>
            </Stack>
          ) : (
            <TableContainer sx={{ overflowX: 'auto' }}>
              <Table sx={{ minWidth: 1200 }}>
                <TableHead>
                  <TableRow sx={{ bgcolor: '#F8FAFC' }}>
                    <TableCell sx={{ fontWeight: 700 }}>المتجر</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>صاحب المتجر</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>الخطة</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>الحالة</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>الأجهزة</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>تاريخ البداية</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>تاريخ الانتهاء</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>الإجراءات</TableCell>
                  </TableRow>
                </TableHead>

                <TableBody>
                  {filteredSubscriptions.map((item) => {
                    const status = getStatusInfo(item.status);

                    return (
                      <TableRow
                        key={item.subscriptionId}
                        hover
                        onClick={() =>
                          navigate(`/subscriptions/${item.subscriptionId}`)
                        }
                        sx={{
                          cursor: 'pointer',
                          '&:last-child td': { borderBottom: 0 },
                        }}
                      >
                        <TableCell>
                          <Typography sx={{ fontWeight: 600, fontSize: 13 }}>
                            {item.storeName}
                          </Typography>
                        </TableCell>

                        <TableCell>
                          <Typography sx={{ fontSize: 13 }}>
                            {item.ownerName}
                          </Typography>
                        </TableCell>

                        <TableCell>
                          <Typography sx={{ fontSize: 13 }}>
                            {item.planName || '—'}
                          </Typography>

                          {item.isLifetime && (
                            <Typography
                              color="success.main"
                              sx={{ fontSize: 11 }}
                            >
                              مدى الحياة
                            </Typography>
                          )}
                        </TableCell>

                        <TableCell>
                          <Chip
                            label={status.label}
                            color={status.color}
                            size="small"
                            variant="outlined"
                          />
                        </TableCell>

                        <TableCell>
                          <Typography sx={{ fontSize: 13, fontWeight: 600 }}>
                            {item.activeDevicesCount} / {item.maxDevices}
                          </Typography>

                          <Typography
                            color="text.secondary"
                            sx={{ fontSize: 11 }}
                          >
                            {item.registeredDevicesCount} مسجل
                          </Typography>
                        </TableCell>

                        <TableCell>
                          <Typography sx={{ fontSize: 12 }}>
                            {formatDate(item.startDate)}
                          </Typography>
                        </TableCell>

                        <TableCell>
                          <Typography sx={{ fontSize: 12 }}>
                            {item.isLifetime
                              ? 'مدى الحياة'
                              : formatDate(item.endDate)}
                          </Typography>
                        </TableCell>
                        <TableCell>
                        {['0', '1', 'Pending', 'Active'].includes(
                          getStatusKey(item.status),
                        ) ? (
                          <Button
                            size="small"
                            color="error"
                            variant="outlined"
                            startIcon={<EventBusyRounded />}
                            onClick={(event) => {
                              event.stopPropagation();
                              openCancelDialog(item);
                            }}
                          >
                            إلغاء الاشتراك
                          </Button>
                        ) : (
                          <Typography color="text.secondary" sx={{ fontSize: 12 }}>
                            —
                          </Typography>
                        )}
                      </TableCell>
                      </TableRow>
                    );
                  })}

                  {filteredSubscriptions.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={8}>
                        <Stack
                          spacing={1}
                          sx={{ alignItems: 'center', py: 6 }}
                        >
                          <SubscriptionsRounded
                            sx={{ fontSize: 42, color: '#CBD5E1' }}
                          />

                          <Typography sx={{ fontWeight: 600 }}>
                            لا توجد اشتراكات
                          </Typography>

                          <Typography
                            color="text.secondary"
                            sx={{ fontSize: 12 }}
                          >
                            {subscriptions.length === 0
                              ? 'لم يتم تسجيل أي اشتراك حتى الآن.'
                              : 'لم نعثر على نتائج تطابق البحث أو التصفية.'}
                          </Typography>
                        </Stack>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          {!loading && subscriptions.length > 0 && (
            <Typography
              color="text.secondary"
              sx={{ fontSize: 12, mt: 2 }}
            >
              عرض {filteredSubscriptions.length} من أصل {subscriptions.length} اشتراك.
            </Typography>
          )}
        </CardContent>
      </Card>

      {/* Create subscription dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={closeCreateDialog}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle sx={{ fontWeight: 700 }}>
          {createdResult ? 'تم إنشاء الاشتراك بنجاح' : 'إنشاء اشتراك جديد'}
        </DialogTitle>

        <DialogContent dividers>
          {createdResult ? (
            <Stack spacing={2.5}>
              <Alert severity="success">
                تم إنشاء الاشتراك ورمز التفعيل بنجاح.
              </Alert>

              <Box>
                <Typography color="text.secondary" sx={{ fontSize: 13 }}>
                  المتجر
                </Typography>

                <Typography sx={{ fontWeight: 700 }}>
                  {createdResult.storeName}
                </Typography>
              </Box>

              <Box>
                <Typography color="text.secondary" sx={{ fontSize: 13 }}>
                  خطة الاشتراك
                </Typography>

                <Typography sx={{ fontWeight: 700 }}>
                  {createdResult.planName}
                </Typography>
              </Box>

              <Box>
                <Typography color="text.secondary" sx={{ fontSize: 13 }}>
                  عدد الأجهزة المسموح بها
                </Typography>

                <Typography sx={{ fontWeight: 700 }}>
                  {createdResult.maxDevices}
                </Typography>
              </Box>

              <Box>
                <Typography color="text.secondary" sx={{ fontSize: 13, mb: 1 }}>
                  رمز التفعيل
                </Typography>

                <Stack
                  direction="row"
                  spacing={1}
                  sx={{ alignItems: 'center' }}
                >
                  <TextField
                    fullWidth
                    value={createdResult.activationCode}
                    size="small"
                    slotProps={{
                      input: {
                        readOnly: true,
                      },
                    }}
                  />

                  <Button
                    variant="contained"
                    color="primary"
                    onClick={() => void handleCopyActivationCode()}
                    startIcon={<ContentCopyRounded />}
                  >
                    نسخ
                  </Button>
                </Stack>

                {copyMessage && (
                  <Typography
                    color={copyMessage.startsWith('تم') ? 'success.main' : 'error.main'}
                    sx={{ fontSize: 12, mt: 1 }}
                  >
                    {copyMessage}
                  </Typography>
                )}
              </Box>

              {createdResult.activationCodeExpiresAt && (
                <Box>
                  <Typography color="text.secondary" sx={{ fontSize: 13 }}>
                    تاريخ انتهاء صلاحية رمز التفعيل
                  </Typography>

                  <Typography sx={{ fontWeight: 600 }}>
                    {formatDate(createdResult.activationCodeExpiresAt)}
                  </Typography>
                </Box>
              )}

              <Alert severity="info">
                احتفظ برمز التفعيل وأرسله إلى العميل ليتمكن من تفعيل اشتراكه.
              </Alert>
            </Stack>
          ) : (
            <Stack spacing={2.5} sx={{ pt: 1 }}>
              {createError && (
                <Alert severity="error">
                  {createError}
                </Alert>
              )}

              {loadingOptions ? (
                <Stack
                  spacing={1.5}
                  sx={{
                    alignItems: 'center',
                    justifyContent: 'center',
                    py: 5,
                  }}
                >
                  <CircularProgress size={30} />

                  <Typography color="text.secondary" sx={{ fontSize: 13 }}>
                    جارٍ تحميل العملاء وخطط الاشتراك...
                  </Typography>
                </Stack>
              ) : (
                <>
                  <FormControl fullWidth size="small">
                    <InputLabel id="create-subscriber-label">
                      العميل
                    </InputLabel>

                    <Select
                      labelId="create-subscriber-label"
                      value={subscriberId}
                      label="العميل"
                      onChange={(event) => setSubscriberId(event.target.value)}
                    >
                      {subscribers.map((subscriber) => (
                        <MenuItem key={subscriber.id} value={subscriber.id}>
                          {subscriber.storeName} - {subscriber.ownerName}
                          {subscriber.phone ? ` (${subscriber.phone})` : ''}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>

                  {subscribers.length === 0 && (
                    <Alert severity="warning">
                      لا يوجد عملاء نشطون. أضف عميلًا أو أعد تفعيل أحد العملاء أولًا.
                    </Alert>
                  )}

                  <FormControl fullWidth size="small">
                    <InputLabel id="create-plan-label">
                      خطة الاشتراك
                    </InputLabel>

                    <Select
                      labelId="create-plan-label"
                      value={planId}
                      label="خطة الاشتراك"
                      onChange={(event) =>
                        handlePlanChange(event.target.value)
                      }
                    >
                      {plans.map((plan) => (
                        <MenuItem key={plan.id} value={plan.id}>
                          {plan.name}
                          {plan.isLifetime
                            ? ' - مدى الحياة'
                            : plan.durationDays
                              ? ` - ${plan.durationDays} يوم`
                              : ''}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>

                  {plans.length === 0 && (
                    <Alert severity="warning">
                      لا توجد خطط اشتراك نشطة. أنشئ خطة أو أعد تفعيل إحدى الخطط أولًا.
                    </Alert>
                  )}

                  <TextField
                    fullWidth
                    size="small"
                    type="number"
                    label="الحد الأقصى للأجهزة"
                    value={maxDevices}
                    onChange={(event) => setMaxDevices(event.target.value)}
                    slotProps={{
                        htmlInput: {
                        min: 1,
                        max: 1000,
                        },
                    }}
                    helperText="يمكنك تعديل العدد الافتراضي المحدد في خطة الاشتراك."
                    />

                  <TextField
                    fullWidth
                    size="small"
                    type="date"
                    label="تاريخ انتهاء صلاحية رمز التفعيل"
                    value={activationCodeExpiresAt}
                    onChange={(event) =>
                      setActivationCodeExpiresAt(event.target.value)
                    }
                    slotProps={{
                      inputLabel: {
                        shrink: true,
                      },
                    }}
                    helperText="حقل اختياري. اتركه فارغًا إذا لم ترغب في تحديد تاريخ انتهاء للرمز."
                  />
                </>
              )}
            </Stack>
          )}
        </DialogContent>

        <DialogActions sx={{ px: 3, py: 2 }}>
          {createdResult ? (
            <>
              <Button
                variant="outlined"
                onClick={() => {
                  closeCreateDialog();
                  navigate(`/subscriptions/${createdResult.subscriptionId}`);
                }}
              >
                عرض تفاصيل الاشتراك
              </Button>

              <Button
                variant="contained"
                onClick={closeCreateDialog}
              >
                إغلاق
              </Button>
            </>
          ) : (
            <>
              <Button
                onClick={closeCreateDialog}
                disabled={creating}
                color="inherit"
              >
                إلغاء
              </Button>

              <Button
                variant="contained"
                onClick={() => void handleCreateSubscription()}
                disabled={
                  creating ||
                  loadingOptions ||
                  subscribers.length === 0 ||
                  plans.length === 0
                }
                startIcon={
                  creating ? (
                    <CircularProgress size={17} color="inherit" />
                  ) : (
                    <AddRounded />
                  )
                }
              >
                {creating ? 'جارٍ إنشاء الاشتراك...' : 'إنشاء الاشتراك'}
              </Button>
            </>
          )}
        </DialogActions>
            </Dialog>

      {/* Cancel subscription confirmation dialog */}
      <Dialog
        open={cancelDialogOpen}
        onClose={closeCancelDialog}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle sx={{ fontWeight: 700 }}>
          تأكيد إلغاء الاشتراك
        </DialogTitle>

        <DialogContent dividers>
          <Stack spacing={2}>
            <Alert severity="warning">
              هل أنت متأكد من إلغاء هذا الاشتراك؟ لا يمكن التراجع عن عملية
              الإلغاء من لوحة الإدارة.
            </Alert>

            {subscriptionToCancel && (
              <>
                <Box>
                  <Typography color="text.secondary" sx={{ fontSize: 13 }}>
                    المتجر
                  </Typography>

                  <Typography sx={{ fontWeight: 700 }}>
                    {subscriptionToCancel.storeName}
                  </Typography>
                </Box>

                <Box>
                  <Typography color="text.secondary" sx={{ fontSize: 13 }}>
                    صاحب المتجر
                  </Typography>

                  <Typography sx={{ fontWeight: 600 }}>
                    {subscriptionToCancel.ownerName}
                  </Typography>
                </Box>
              </>
            )}

            <Alert severity="error">
              سيترتب على الإلغاء:
              - إيقاف الاشتراك.
              - إلغاء تفعيل الأجهزة النشطة.
              - إبطال رموز التفعيل غير المستخدمة.
              - الاحتفاظ بسجلات الأجهزة والعمليات لأغراض التدقيق.
            </Alert>

            {cancelError && (
              <Alert severity="error">
                {cancelError}
              </Alert>
            )}
          </Stack>
        </DialogContent>

        <DialogActions sx={{ px: 3, py: 2 }}>
          <Button
            color="inherit"
            onClick={closeCancelDialog}
            disabled={cancelling}
          >
            تراجع
          </Button>

          <Button
            variant="contained"
            color="error"
            onClick={() => void handleCancelSubscription()}
            disabled={cancelling || !subscriptionToCancel}
            startIcon={
              cancelling
                ? <CircularProgress size={17} color="inherit" />
                : <EventBusyRounded />
            }
          >
            {cancelling ? 'جارٍ إلغاء الاشتراك...' : 'تأكيد الإلغاء'}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}

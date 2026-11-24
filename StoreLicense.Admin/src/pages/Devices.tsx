
import { useCallback, useEffect, useMemo, useState } from 'react';

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
  DevicesRounded,
  SearchRounded,
  CheckCircleRounded,
  BlockRounded,
  StoreRounded,
  PersonRounded,
  CalendarMonthRounded,
  AccessTimeRounded,
  RefreshRounded,
  WarningAmberRounded,
  SubscriptionsRounded,
} from '@mui/icons-material';

import api from '../services/api';

type EnumValue = number | string;

interface Subscription {
  subscriptionId: string;
  subscriberId: string;
  storeName: string;
  ownerName: string;
  planName: string | null;
  maxDevices: number;
  registeredDevicesCount: number;
  activeDevicesCount: number;
  status: EnumValue;
}

interface Device {
  id: string;
  subscriberId: string;
  subscriptionId: string;
  deviceName: string;
  deviceIdentifier: string;
  status: EnumValue;
  statusName: string;
  activatedAt: string;
  lastSeenAt: string | null;
}

type StatusFilter = 'all' | 'active' | 'revoked';

interface StatusInfo {
  label: string;
  color: 'success' | 'error' | 'default';
}

const getDeviceStatus = (status: EnumValue): 'active' | 'revoked' => {
  const value = String(status).toLowerCase();

  return value === '1' || value === 'active'
    ? 'active'
    : 'revoked';
};

const getStatusInfo = (status: EnumValue): StatusInfo => {
  if (getDeviceStatus(status) === 'active') {
    return {
      label: 'نشط',
      color: 'success',
    };
  }

  return {
    label: 'ملغى التفعيل',
    color: 'error',
  };
};

const formatDate = (value: string | null): string => {
  if (!value) {
    return 'لم يسجل بعد';
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return 'غير معروف';
  }

  return new Intl.DateTimeFormat('ar', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date);
};

const getErrorMessage = (
  error: unknown,
  fallback: string,
): string => {
  const responseError = error as {
    response?: {
      data?: {
        message?: string;
      };
    };
  };

  return responseError.response?.data?.message || fallback;
};

export default function Devices() {
  const [subscriptions, setSubscriptions] = useState<Subscription[]>([]);
  const [selectedSubscriptionId, setSelectedSubscriptionId] = useState('');

  const [devices, setDevices] = useState<Device[]>([]);

  const [loadingSubscriptions, setLoadingSubscriptions] = useState(true);
  const [loadingDevices, setLoadingDevices] = useState(false);

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');

  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [confirmOpen, setConfirmOpen] = useState(false);
  const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);
  const [revoking, setRevoking] = useState(false);

  // تحميل الاشتراكات
  const loadSubscriptions = useCallback(async () => {
    setLoadingSubscriptions(true);
    setError('');

    try {
      const response = await api.get<Subscription[]>('/api/subscriptions');
      const data = response.data;

      setSubscriptions(data);

      // اختيار أول اشتراك يحتوي على أجهزة، أو أول اشتراك متاح
      if (data.length > 0) {
        setSelectedSubscriptionId((current) => {
          if (
            current &&
            data.some((item) => item.subscriptionId === current)
          ) {
            return current;
          }

          const subscriptionWithDevices = data.find(
            (item) => item.registeredDevicesCount > 0
          );

          return (
            subscriptionWithDevices?.subscriptionId ||
            data[0].subscriptionId
          );
        });
      } else {
        setSelectedSubscriptionId('');
        setDevices([]);
      }
    } catch (err) {
      setError(
        getErrorMessage(err, 'تعذر تحميل قائمة الاشتراكات.')
      );
    } finally {
      setLoadingSubscriptions(false);
    }
  }, []);

  // تحميل أجهزة الاشتراك المحدد
  const loadDevices = useCallback(async () => {
    if (!selectedSubscriptionId) {
      setDevices([]);
      return;
    }

    setLoadingDevices(true);
    setError('');

    try {
      const response = await api.get<Device[]>(
        `/api/devices/subscription/${selectedSubscriptionId}`
      );

      setDevices(response.data);
    } catch (err) {
      setDevices([]);

      setError(
        getErrorMessage(err, 'تعذر تحميل أجهزة الاشتراك.')
      );
    } finally {
      setLoadingDevices(false);
    }
  }, [selectedSubscriptionId]);

  useEffect(() => {
    loadSubscriptions();
  }, [loadSubscriptions]);

  useEffect(() => {
    loadDevices();
  }, [loadDevices]);

  // الاشتراك المحدد
  const selectedSubscription = subscriptions.find(
    (item) => item.subscriptionId === selectedSubscriptionId
  );

  // الإحصائيات
  const statistics = useMemo(() => {
    const active = devices.filter(
      (device) => getDeviceStatus(device.status) === 'active'
    ).length;

    const revoked = devices.filter(
      (device) => getDeviceStatus(device.status) === 'revoked'
    ).length;

    return {
      total: devices.length,
      active,
      revoked,
    };
  }, [devices]);

  // البحث والتصفية
  const filteredDevices = useMemo(() => {
    return devices.filter((device) => {
      const searchValue = search.trim().toLowerCase();

      const matchesSearch =
        !searchValue ||
        device.deviceName.toLowerCase().includes(searchValue) ||
        device.deviceIdentifier.toLowerCase().includes(searchValue);

      const deviceStatus = getDeviceStatus(device.status);

      const matchesStatus =
        statusFilter === 'all' ||
        (statusFilter === 'active' && deviceStatus === 'active') ||
        (statusFilter === 'revoked' && deviceStatus === 'revoked');

      return matchesSearch && matchesStatus;
    });
  }, [devices, search, statusFilter]);

  // فتح نافذة تأكيد الإلغاء
  const handleOpenRevoke = (device: Device) => {
    setSelectedDevice(device);
    setConfirmOpen(true);
  };

  // إغلاق نافذة التأكيد
  const handleCloseRevoke = () => {
    if (revoking) return;

    setConfirmOpen(false);
    setSelectedDevice(null);
  };

  // إلغاء تفعيل الجهاز
  const handleRevoke = async () => {
    if (!selectedDevice) return;

    setRevoking(true);
    setError('');
    setSuccess('');

    try {
      await api.patch(
        `/api/devices/${selectedDevice.id}/revoke`
      );

      setSuccess(
        `تم إلغاء تفعيل الجهاز "${selectedDevice.deviceName}" بنجاح.`
      );

      setConfirmOpen(false);
      setSelectedDevice(null);

      await loadDevices();
      await loadSubscriptions();
    } catch (err) {
      setError(
        getErrorMessage(err, 'تعذر إلغاء تفعيل الجهاز.')
      );
    } finally {
      setRevoking(false);
    }
  };

  const handleRefresh = async () => {
    setSuccess('');
    await loadSubscriptions();
    await loadDevices();
  };

  return (
    <Box sx={{ pb: 4 }}>

      {/* عنوان الصفحة */}
      <Stack
        direction="row"
        spacing={2}
        sx={{
          mb: 3,
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
        }}
      >
        <Box>
          <Typography
            variant="h5"
            sx={{ fontWeight: 800, color: '#172033' }}
          >
            إدارة الأجهزة
          </Typography>

          <Typography
            variant="body2"
            color="text.secondary"
            sx={{ mt: 0.7 }}
          >
            عرض وإدارة الأجهزة المرتبطة باشتراكات العملاء
          </Typography>
        </Box>

        <Button
          variant="outlined"
          startIcon={<RefreshRounded />}
          onClick={handleRefresh}
          disabled={loadingSubscriptions || loadingDevices}
          sx={{ borderRadius: 2 }}
        >
          تحديث البيانات
        </Button>
      </Stack>

      {/* الرسائل */}
      {error && (
        <Alert
          severity="error"
          sx={{ mb: 2, borderRadius: 2 }}
          onClose={() => setError('')}
        >
          {error}
        </Alert>
      )}

      {success && (
        <Alert
          severity="success"
          sx={{ mb: 2, borderRadius: 2 }}
          onClose={() => setSuccess('')}
        >
          {success}
        </Alert>
      )}

      {/* اختيار الاشتراك */}
      <Card
        elevation={0}
        sx={{
          border: '1px solid #E8ECF3',
          borderRadius: 3,
          mb: 3,
        }}
      >
        <CardContent sx={{ p: 3 }}>
          <Stack
            direction="row"
            spacing={1}
            sx={{ mb: 2, alignItems: 'center' }}
          >
            <SubscriptionsRounded color="primary" />

            <Typography sx={{ fontWeight: 700 }}>
              اختر الاشتراك
            </Typography>
          </Stack>

          {loadingSubscriptions ? (
            <Stack
              direction="row"
              spacing={1}
              sx={{ alignItems: 'center' }}
            >
              <CircularProgress size={20} />
              <Typography color="text.secondary">
                جاري تحميل الاشتراكات...
              </Typography>
            </Stack>
          ) : subscriptions.length === 0 ? (
            <Alert severity="info">
              لا توجد اشتراكات متاحة حاليًا.
            </Alert>
          ) : (
            <FormControl fullWidth size="small">
              <InputLabel id="subscription-select-label">
                الاشتراك
              </InputLabel>

              <Select
                labelId="subscription-select-label"
                value={selectedSubscriptionId}
                label="الاشتراك"
                onChange={(event) => {
                  setSelectedSubscriptionId(event.target.value);
                  setSearch('');
                  setStatusFilter('all');
                  setSuccess('');
                }}
              >
                {subscriptions.map((subscription) => (
                  <MenuItem
                    key={subscription.subscriptionId}
                    value={subscription.subscriptionId}
                  >
                    {subscription.storeName} — {subscription.ownerName}
                    {' | '}
                    {subscription.planName || 'بدون خطة'}
                    {' | '}
                    الأجهزة: {subscription.registeredDevicesCount}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}

          {selectedSubscription && (
            <Stack
              direction="row"
              spacing={2}
              useFlexGap
              sx={{
                mt: 2,
                flexWrap: 'wrap',
              }}
            >
              <Chip
                icon={<StoreRounded />}
                label={selectedSubscription.storeName}
                variant="outlined"
              />

              <Chip
                icon={<PersonRounded />}
                label={selectedSubscription.ownerName}
                variant="outlined"
              />

              <Chip
                label={`الحد الأقصى للأجهزة: ${selectedSubscription.maxDevices}`}
                variant="outlined"
              />

              <Chip
                label={`الأجهزة النشطة: ${selectedSubscription.activeDevicesCount}`}
                color="success"
                variant="outlined"
              />
            </Stack>
          )}
        </CardContent>
      </Card>

      {/* الإحصائيات */}
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: {
            xs: '1fr',
            sm: 'repeat(3, 1fr)',
          },
          gap: 2,
          mb: 3,
        }}
      >
        <Card
          elevation={0}
          sx={{ border: '1px solid #E8ECF3', borderRadius: 3 }}
        >
          <CardContent>
            <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
              <Avatar sx={{ bgcolor: '#EFF6FF', color: '#2563EB' }}>
                <DevicesRounded />
              </Avatar>

              <Box>
                <Typography variant="body2" color="text.secondary">
                  إجمالي الأجهزة
                </Typography>

                <Typography variant="h5" sx={{ fontWeight: 800 }}>
                  {statistics.total}
                </Typography>
              </Box>
            </Stack>
          </CardContent>
        </Card>

        <Card
          elevation={0}
          sx={{ border: '1px solid #E8ECF3', borderRadius: 3 }}
        >
          <CardContent>
            <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
              <Avatar sx={{ bgcolor: '#ECFDF5', color: '#059669' }}>
                <CheckCircleRounded />
              </Avatar>

              <Box>
                <Typography variant="body2" color="text.secondary">
                  الأجهزة النشطة
                </Typography>

                <Typography variant="h5" sx={{ fontWeight: 800 }}>
                  {statistics.active}
                </Typography>
              </Box>
            </Stack>
          </CardContent>
        </Card>

        <Card
          elevation={0}
          sx={{ border: '1px solid #E8ECF3', borderRadius: 3 }}
        >
          <CardContent>
            <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
              <Avatar sx={{ bgcolor: '#FEF2F2', color: '#DC2626' }}>
                <BlockRounded />
              </Avatar>

              <Box>
                <Typography variant="body2" color="text.secondary">
                  الأجهزة الملغاة
                </Typography>

                <Typography variant="h5" sx={{ fontWeight: 800 }}>
                  {statistics.revoked}
                </Typography>
              </Box>
            </Stack>
          </CardContent>
        </Card>
      </Box>

      {/* جدول الأجهزة */}
      <Card
        elevation={0}
        sx={{
          border: '1px solid #E8ECF3',
          borderRadius: 3,
        }}
      >
        <CardContent sx={{ p: 3 }}>

          <Stack
            direction="row"
            spacing={2}
            sx={{
              mb: 3,
              alignItems: 'center',
              justifyContent: 'space-between',
              flexWrap: 'wrap',
            }}
          >
            <Box>
              <Typography sx={{ fontWeight: 800, fontSize: 17 }}>
                الأجهزة المسجلة
              </Typography>

              <Typography variant="body2" color="text.secondary">
                الأجهزة المرتبطة بالاشتراك المحدد
              </Typography>
            </Box>

            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={1.5}
              sx={{ width: { xs: '100%', sm: 'auto' } }}
            >
              <TextField
                size="small"
                placeholder="ابحث باسم الجهاز أو المعرّف..."
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                sx={{ minWidth: { sm: 260 } }}
                slotProps={{
                  input: {
                    startAdornment: (
                      <InputAdornment position="start">
                        <SearchRounded fontSize="small" />
                      </InputAdornment>
                    ),
                  },
                }}
              />

              <FormControl size="small" sx={{ minWidth: 150 }}>
                <InputLabel id="device-status-filter">
                  الحالة
                </InputLabel>

                <Select
                  labelId="device-status-filter"
                  value={statusFilter}
                  label="الحالة"
                  onChange={(event) =>
                    setStatusFilter(event.target.value as StatusFilter)
                  }
                >
                  <MenuItem value="all">جميع الحالات</MenuItem>
                  <MenuItem value="active">نشط</MenuItem>
                  <MenuItem value="revoked">ملغى التفعيل</MenuItem>
                </Select>
              </FormControl>
            </Stack>
          </Stack>

          {loadingDevices ? (
            <Stack
              spacing={2}
              sx={{
                alignItems: 'center',
                justifyContent: 'center',
                py: 7,
              }}
            >
              <CircularProgress />

              <Typography color="text.secondary">
                جاري تحميل الأجهزة...
              </Typography>
            </Stack>
          ) : !selectedSubscriptionId ? (
            <Alert severity="info">
              اختر اشتراكًا لعرض أجهزته.
            </Alert>
          ) : filteredDevices.length === 0 ? (
            <Box sx={{ textAlign: 'center', py: 7 }}>
              <DevicesRounded
                sx={{ fontSize: 55, color: '#CBD5E1', mb: 1 }}
              />

              <Typography sx={{ fontWeight: 700 }}>
                لا توجد أجهزة
              </Typography>

              <Typography
                variant="body2"
                color="text.secondary"
                sx={{ mt: 0.5 }}
              >
                {devices.length === 0
                  ? 'لم يتم تسجيل أي جهاز لهذا الاشتراك.'
                  : 'لا توجد نتائج تطابق البحث أو التصفية.'}
              </Typography>
            </Box>
          ) : (
            <TableContainer sx={{ overflowX: 'auto' }}>
              <Table sx={{ minWidth: 850 }}>
                <TableHead>
                  <TableRow sx={{ bgcolor: '#F8FAFC' }}>
                    <TableCell sx={{ fontWeight: 800 }}>
                      الجهاز
                    </TableCell>

                    <TableCell sx={{ fontWeight: 800 }}>
                      معرّف الجهاز
                    </TableCell>

                    <TableCell sx={{ fontWeight: 800 }}>
                      الحالة
                    </TableCell>

                    <TableCell sx={{ fontWeight: 800 }}>
                      تاريخ التفعيل
                    </TableCell>

                    <TableCell sx={{ fontWeight: 800 }}>
                      آخر ظهور
                    </TableCell>

                    <TableCell align="center" sx={{ fontWeight: 800 }}>
                      الإجراءات
                    </TableCell>
                  </TableRow>
                </TableHead>

                <TableBody>
                  {filteredDevices.map((device) => {
                    const status = getStatusInfo(device.status);

                    const isActive =
                      getDeviceStatus(device.status) === 'active';

                    return (
                      <TableRow key={device.id} hover>
                        <TableCell>
                          <Stack
                            direction="row"
                            spacing={1.5}
                            sx={{ alignItems: 'center' }}
                          >
                            <Avatar
                              sx={{
                                bgcolor: '#EFF6FF',
                                color: '#2563EB',
                                width: 38,
                                height: 38,
                              }}
                            >
                              <DevicesRounded fontSize="small" />
                            </Avatar>

                            <Typography sx={{ fontWeight: 600 }}>
                              {device.deviceName || 'جهاز غير معروف'}
                            </Typography>
                          </Stack>
                        </TableCell>

                        <TableCell>
                          <Typography
                            variant="body2"
                            sx={{
                              fontFamily: 'monospace',
                              direction: 'ltr',
                              textAlign: 'right',
                            }}
                          >
                            {device.deviceIdentifier}
                          </Typography>
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
                          <Stack
                            direction="row"
                            spacing={0.7}
                            sx={{ alignItems: 'center' }}
                          >
                            <CalendarMonthRounded
                              sx={{ fontSize: 17, color: '#64748B' }}
                            />

                            <Typography variant="body2">
                              {formatDate(device.activatedAt)}
                            </Typography>
                          </Stack>
                        </TableCell>

                        <TableCell>
                          <Stack
                            direction="row"
                            spacing={0.7}
                            sx={{ alignItems: 'center' }}
                          >
                            <AccessTimeRounded
                              sx={{ fontSize: 17, color: '#64748B' }}
                            />

                            <Typography variant="body2">
                              {formatDate(device.lastSeenAt)}
                            </Typography>
                          </Stack>
                        </TableCell>

                        <TableCell align="center">
                          {isActive ? (
                            <Button
                              size="small"
                              color="error"
                              variant="outlined"
                              startIcon={<BlockRounded />}
                              onClick={() => handleOpenRevoke(device)}
                              sx={{ borderRadius: 2 }}
                            >
                              إلغاء التفعيل
                            </Button>
                          ) : (
                            <Chip
                              label="ملغى"
                              color="default"
                              size="small"
                            />
                          )}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>
          )}

          {!loadingDevices && devices.length > 0 && (
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{ mt: 2 }}
            >
              عرض {filteredDevices.length} من أصل {devices.length} جهاز
            </Typography>
          )}
        </CardContent>
      </Card>

      {/* نافذة تأكيد إلغاء التفعيل */}
      <Dialog
        open={confirmOpen}
        onClose={handleCloseRevoke}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle sx={{ fontWeight: 800 }}>
          تأكيد إلغاء تفعيل الجهاز
        </DialogTitle>

        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Stack
              direction="row"
              spacing={1.5}
              sx={{ alignItems: 'center' }}
            >
              <Avatar
                sx={{
                  bgcolor: '#FEF2F2',
                  color: '#DC2626',
                }}
              >
                <WarningAmberRounded />
              </Avatar>

              <Typography sx={{ fontWeight: 700 }}>
                {selectedDevice?.deviceName}
              </Typography>
            </Stack>

            <Alert severity="warning">
              هل أنت متأكد من إلغاء تفعيل هذا الجهاز؟

              سيصبح سجل الجهاز بحالة ملغى، ولن يمكن إعادة تفعيله من خلال النظام.
            </Alert>
          </Stack>
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button
            onClick={handleCloseRevoke}
            disabled={revoking}
            color="inherit"
          >
            تراجع
          </Button>

          <Button
            onClick={handleRevoke}
            color="error"
            variant="contained"
            disabled={revoking}
          >
            {revoking ? (
              <CircularProgress size={20} color="inherit" />
            ) : (
              'تأكيد الإلغاء'
            )}
          </Button>
        </DialogActions>
      </Dialog>

    </Box>
  );
}

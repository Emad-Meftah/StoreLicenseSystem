
import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Divider,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  TextField,
} from '@mui/material';

import {
  ArrowBackRounded,
  AutorenewRounded,
  CalendarMonthRounded,
  CheckCircleRounded,
  DevicesRounded,
  KeyRounded,
  PersonRounded,
  PhoneRounded,
  SubscriptionsRounded,
  ContentCopyRounded,
  AddRounded,
} from '@mui/icons-material';

import api from '../services/api';

type EnumValue = number | string;

interface SubscriptionDevice {
  deviceId: string;
  deviceName: string;
  activatedAt: string;
  lastSeenAt: string | null;
  status: EnumValue;
}

interface ActivationCode {
  activationCodeId: string;
  activationCode: string | null;
  status: EnumValue;
  createdAt: string;
  expiresAt: string | null;
  redeemedAt: string | null;
}

interface SubscriptionDetailsData {
  subscriptionId: string;
  subscriberId: string;
  storeName: string;
  ownerName: string;
  phone: string | null;
  planId: string | null;
  planName: string | null;
  isLifetime: boolean;
  durationDays: number | null;
  maxDevices: number;
  status: EnumValue;
  startDate: string | null;
  endDate: string | null;
  createdAt: string;
  registeredDevicesCount: number;
  activeDevicesCount: number;
  devices: SubscriptionDevice[];
  activationCodes: ActivationCode[];
}

type ChipColor =
  | 'default'
  | 'success'
  | 'warning'
  | 'error'
  | 'info';

interface StatusInfo {
  label: string;
  color: ChipColor;
}

const subscriptionStatuses: Record<string, StatusInfo> = {
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

const deviceStatuses: Record<string, StatusInfo> = {
  '1': { label: 'نشط', color: 'success' },
  '2': { label: 'ملغي', color: 'error' },
  Active: { label: 'نشط', color: 'success' },
  Revoked: { label: 'ملغي', color: 'error' },
};

const activationCodeStatuses: Record<string, StatusInfo> = {
  '0': { label: 'جاهز للاستخدام', color: 'info' },
  '1': { label: 'مستخدم', color: 'success' },
  '2': { label: 'ملغي', color: 'error' },
  '3': { label: 'منتهي', color: 'default' },
  Generated: { label: 'جاهز للاستخدام', color: 'info' },
  Redeemed: { label: 'مستخدم', color: 'success' },
  Revoked: { label: 'ملغي', color: 'error' },
  Expired: { label: 'منتهي', color: 'default' },
};

function getStatus(
  value: EnumValue,
  statuses: Record<string, StatusInfo>,
): StatusInfo {
  return statuses[String(value)] ?? {
    label: 'غير معروف',
    color: 'default',
  };
}

function formatDate(value: string | null): string {
  if (!value) return '—';

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) return '—';

  return new Intl.DateTimeFormat('ar-YE', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(date);
}

function formatDateTime(value: string | null): string {
  if (!value) return '—';

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) return '—';

  return new Intl.DateTimeFormat('ar-YE', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

interface InfoItemProps {
  label: string;
  value: string;
  icon?: React.ReactNode;
}

function InfoItem({ label, value, icon }: InfoItemProps) {
  return (
    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
      {icon && (
        <Avatar
          variant="rounded"
          sx={{
            width: 38,
            height: 38,
            bgcolor: '#F1F5F9',
            color: '#475569',
          }}
        >
          {icon}
        </Avatar>
      )}

      <Box sx={{ minWidth: 0 }}>
        <Typography color="text.secondary" sx={{ fontSize: 12 }}>
          {label}
        </Typography>

        <Typography
          sx={{
            fontSize: 14,
            fontWeight: 600,
            overflowWrap: 'anywhere',
          }}
        >
          {value}
        </Typography>
      </Box>
    </Stack>
  );
}

export default function SubscriptionDetails() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [subscription, setSubscription] =
    useState<SubscriptionDetailsData | null>(null);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [generatingCode, setGeneratingCode] = useState(false);
  const [codeExpiresAt, setCodeExpiresAt] = useState('');
  const [generatedCode, setGeneratedCode] = useState('');
  const [generateError, setGenerateError] = useState('');
  const [generateSuccess, setGenerateSuccess] = useState('');

  const loadDetails = useCallback(async () => {
    if (!id) {
      setError('معرّف الاشتراك غير صالح.');
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError('');

      const response = await api.get<SubscriptionDetailsData>(
        `/api/subscriptions/${id}`,
      );

      setSubscription(response.data);
    } catch (err: unknown) {
      if (typeof err === 'object' && err !== null && 'response' in err) {
        const axiosError = err as {
          response?: {
            status?: number;
            data?: { message?: string };
          };
        };

        if (axiosError.response?.status === 401) {
          setError('انتهت جلسة تسجيل الدخول أو أن صلاحية الوصول غير متاحة.');
        } else if (axiosError.response?.status === 404) {
          setError('لم يتم العثور على الاشتراك المطلوب.');
        } else if (axiosError.response?.data?.message) {
          setError(axiosError.response.data.message);
        } else {
          setError('تعذر تحميل تفاصيل الاشتراك.');
        }
      } else {
        setError('تعذر الاتصال بالخادم. تحقق من تشغيل واجهة API.');
      }

      setSubscription(null);
    } finally {
      setLoading(false);
    }
  }, [id]);

  const handleGenerateActivationCode = async () => {
    if (!id) return;

    try {
      setGeneratingCode(true);
      setGenerateError('');
      setGenerateSuccess('');
      setGeneratedCode('');

      const response = await api.post<{
        subscriptionId: string;
        activationCode: string;
        createdAt: string;
        expiresAt: string | null;
      }>(
        `/api/subscriptions/${id}/activation-codes`,
        {
          expiresAt: codeExpiresAt
            ? `${codeExpiresAt}T23:59:59.000Z`
            : null,
        },
      );

      setGeneratedCode(response.data.activationCode);
      setGenerateSuccess('تم إنشاء رمز التفعيل بنجاح.');

      await loadDetails();
    } catch (err: unknown) {
      if (
        typeof err === 'object' &&
        err !== null &&
        'response' in err
      ) {
        const axiosError = err as {
          response?: {
            status?: number;
            data?: { message?: string };
          };
        };

        setGenerateError(
          axiosError.response?.data?.message ||
            'تعذر إنشاء رمز التفعيل.',
        );
      } else {
        setGenerateError('تعذر الاتصال بالخادم.');
      }
    } finally {
      setGeneratingCode(false);
    }
  };

  useEffect(() => {
    void loadDetails();
  }, [loadDetails]);

  if (loading) {
    return (
      <Stack
        spacing={2}
        sx={{
          alignItems: 'center',
          justifyContent: 'center',
          py: 12,
        }}
      >
        <CircularProgress />

        <Typography color="text.secondary" sx={{ fontSize: 13 }}>
          جارٍ تحميل تفاصيل الاشتراك...
        </Typography>
      </Stack>
    );
  }

  if (error || !subscription) {
    return (
      <Stack spacing={2}>
        <Button
          startIcon={<ArrowBackRounded />}
          onClick={() => navigate('/subscriptions')}
          sx={{ alignSelf: 'flex-start' }}
        >
          العودة إلى الاشتراكات
        </Button>

        <Alert
          severity="error"
          action={
            <Button
              color="inherit"
              size="small"
              onClick={() => void loadDetails()}
            >
              إعادة المحاولة
            </Button>
          }
        >
          {error || 'تعذر العثور على الاشتراك.'}
        </Alert>
      </Stack>
    );
  }

  const subscriptionStatus = getStatus(
    subscription.status,
    subscriptionStatuses,
  );

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
          <Button
            startIcon={<ArrowBackRounded />}
            onClick={() => navigate('/subscriptions')}
            sx={{ mb: 1 }}
          >
            العودة إلى الاشتراكات
          </Button>

          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            تفاصيل الاشتراك
          </Typography>

          <Typography color="text.secondary" sx={{ fontSize: 13, mt: 0.5 }}>
            معلومات الاشتراك والأجهزة ورموز التفعيل المرتبطة به.
          </Typography>
        </Box>

        <Button
          variant="outlined"
          startIcon={<AutorenewRounded />}
          onClick={() => void loadDetails()}
        >
          تحديث البيانات
        </Button>
      </Stack>

      {/* Subscription summary */}
      <Card
        elevation={0}
        sx={{
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 3,
        }}
      >
        <CardContent sx={{ p: 3 }}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={2}
            sx={{
              justifyContent: 'space-between',
              alignItems: { xs: 'flex-start', sm: 'center' },
            }}
          >
            <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
              <Avatar
                variant="rounded"
                sx={{
                  width: 54,
                  height: 54,
                  bgcolor: '#EFF6FF',
                  color: '#2563EB',
                }}
              >
                <SubscriptionsRounded sx={{ fontSize: 30 }} />
              </Avatar>

              <Box>
                <Typography variant="h6" sx={{ fontWeight: 700 }}>
                  {subscription.storeName}
                </Typography>

                <Typography color="text.secondary" sx={{ fontSize: 12 }}>
                  {subscription.planName || 'بدون خطة محددة'}
                </Typography>
              </Box>
            </Stack>

            <Chip
              label={subscriptionStatus.label}
              color={subscriptionStatus.color}
              variant="outlined"
            />
          </Stack>

          <Divider sx={{ my: 2.5 }} />

          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: {
                xs: '1fr',
                sm: 'repeat(2, 1fr)',
                lg: 'repeat(3, 1fr)',
              },
              gap: 3,
            }}
          >
            <InfoItem
              label="صاحب المتجر"
              value={subscription.ownerName}
              icon={<PersonRounded />}
            />

            <InfoItem
              label="رقم الهاتف"
              value={subscription.phone || 'غير متوفر'}
              icon={<PhoneRounded />}
            />

            <InfoItem
              label="اسم الخطة"
              value={subscription.planName || '—'}
              icon={<SubscriptionsRounded />}
            />

            <InfoItem
              label="عدد الأجهزة المسموح"
              value={String(subscription.maxDevices)}
              icon={<DevicesRounded />}
            />

            <InfoItem
              label="تاريخ بداية الاشتراك"
              value={formatDate(subscription.startDate)}
              icon={<CalendarMonthRounded />}
            />

            <InfoItem
              label="تاريخ الانتهاء"
              value={
                subscription.isLifetime
                  ? 'مدى الحياة'
                  : formatDate(subscription.endDate)
              }
              icon={<CalendarMonthRounded />}
            />

            <InfoItem
              label="مدة الاشتراك"
              value={
                subscription.isLifetime
                  ? 'مدى الحياة'
                  : subscription.durationDays != null
                    ? `${subscription.durationDays} يوم`
                    : '—'
              }
            />

            <InfoItem
              label="تاريخ إنشاء الاشتراك"
              value={formatDateTime(subscription.createdAt)}
            />

            <InfoItem
              label="معرّف الاشتراك"
              value={subscription.subscriptionId}
            />
          </Box>
        </CardContent>
      </Card>

      {/* Statistics */}
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: {
            xs: 'repeat(2, 1fr)',
            md: 'repeat(3, 1fr)',
          },
          gap: 2,
        }}
      >
        <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 3 }}>
          <CardContent>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
              <DevicesRounded color="action" />
              <Typography color="text.secondary" sx={{ fontSize: 12 }}>
                الأجهزة المسجلة
              </Typography>
            </Stack>
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              {subscription.registeredDevicesCount}
            </Typography>
          </CardContent>
        </Card>

        <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 3 }}>
          <CardContent>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
              <CheckCircleRounded color="success" />
              <Typography color="text.secondary" sx={{ fontSize: 12 }}>
                الأجهزة النشطة
              </Typography>
            </Stack>
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              {subscription.activeDevicesCount}
            </Typography>
          </CardContent>
        </Card>

        <Card elevation={0} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 3 }}>
          <CardContent>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
              <KeyRounded color="action" />
              <Typography color="text.secondary" sx={{ fontSize: 12 }}>
                رموز التفعيل
              </Typography>
            </Stack>
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              {subscription.activationCodes.length}
            </Typography>
          </CardContent>
        </Card>
      </Box>

      {/* Devices */}
      <Card
        elevation={0}
        sx={{
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 3,
        }}
      >
        <CardContent sx={{ p: 2.5 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
            <DevicesRounded color="action" />
            <Typography sx={{ fontWeight: 700, fontSize: 17 }}>
              الأجهزة المرتبطة
            </Typography>
          </Stack>

          <Typography color="text.secondary" sx={{ fontSize: 12, mb: 2 }}>
            الأجهزة التي سبق تسجيلها على هذا الاشتراك.
          </Typography>

          <TableContainer sx={{ overflowX: 'auto' }}>
            <Table sx={{ minWidth: 650 }}>
              <TableHead>
                <TableRow sx={{ bgcolor: '#F8FAFC' }}>
                  <TableCell sx={{ fontWeight: 700 }}>اسم الجهاز</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>تاريخ التفعيل</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>آخر ظهور</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>الحالة</TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {subscription.devices.map((device) => {
                  const status = getStatus(device.status, deviceStatuses);

                  return (
                    <TableRow key={device.deviceId} hover>
                      <TableCell>
                        <Typography sx={{ fontSize: 13, fontWeight: 600 }}>
                          {device.deviceName}
                        </Typography>
                      </TableCell>

                      <TableCell sx={{ fontSize: 12 }}>
                        {formatDateTime(device.activatedAt)}
                      </TableCell>

                      <TableCell sx={{ fontSize: 12 }}>
                        {formatDateTime(device.lastSeenAt)}
                      </TableCell>

                      <TableCell>
                        <Chip
                          label={status.label}
                          color={status.color}
                          size="small"
                          variant="outlined"
                        />
                      </TableCell>
                    </TableRow>
                  );
                })}

                {subscription.devices.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={4}>
                      <Typography
                        color="text.secondary"
                        sx={{ py: 4, fontSize: 13, textAlign: 'center' }}
                        >
                        لا توجد أجهزة مسجلة لهذا الاشتراك.
                        </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>

      {/* Activation codes */}
      <Card
        elevation={0}
        sx={{
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 3,
        }}
      >
        <CardContent sx={{ p: 2.5 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
            <KeyRounded color="action" />
            <Typography sx={{ fontWeight: 700, fontSize: 17 }}>
              رموز التفعيل
            </Typography>
          </Stack>

          <Typography color="text.secondary" sx={{ fontSize: 12, mb: 2 }}>
            سجل رموز التفعيل وحالات استخدامها.
          </Typography>

          <Stack spacing={2} sx={{ mb: 3 }}>
            <TextField
              label="تاريخ انتهاء الرمز (اختياري)"
              type="date"
              fullWidth
              size="small"
              value={codeExpiresAt}
              onChange={(event) => setCodeExpiresAt(event.target.value)}
              slotProps={{
                inputLabel: { shrink: true },
              }}
              helperText="اترك التاريخ فارغًا إذا كنت لا تريد تحديد تاريخ انتهاء."
            />

            <Button
              variant="contained"
              startIcon={<AddRounded />}
              onClick={() => void handleGenerateActivationCode()}
              disabled={generatingCode}
              sx={{ alignSelf: 'flex-start' }}
            >
              {generatingCode ? 'جارٍ إنشاء الرمز...' : 'توليد رمز تفعيل جديد'}
            </Button>

            {generateError && (
              <Alert severity="error">
                {generateError}
              </Alert>
            )}

            {generateSuccess && (
              <Alert severity="success">
                {generateSuccess}
              </Alert>
            )}

            {generatedCode && (
              <Card variant="outlined" sx={{ borderRadius: 2 }}>
                <CardContent>
                  <Typography sx={{ fontWeight: 700, mb: 1 }}>
                    رمز التفعيل الجديد
                  </Typography>

                  <Typography
                    sx={{
                      fontFamily: 'monospace',
                      fontWeight: 700,
                      fontSize: 16,
                      overflowWrap: 'anywhere',
                      mb: 2,
                    }}
                  >
                    {generatedCode}
                  </Typography>

                  <Button
                    size="small"
                    variant="outlined"
                    startIcon={<ContentCopyRounded />}
                    onClick={() => {
                      void navigator.clipboard.writeText(generatedCode);
                    }}
                  >
                    نسخ الرمز
                  </Button>
                </CardContent>
              </Card>
            )}
          </Stack>

          <TableContainer sx={{ overflowX: 'auto' }}>
            <Table sx={{ minWidth: 700 }}>
              <TableHead>
                <TableRow sx={{ bgcolor: '#F8FAFC' }}>
                  <TableCell sx={{ fontWeight: 700 }}>معرّف الرمز</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>الحالة</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>تاريخ الإنشاء</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>تاريخ الانتهاء</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>تاريخ الاستخدام</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>الإجراء</TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {subscription.activationCodes.map((code) => {
                  const status = getStatus(
                    code.status,
                    activationCodeStatuses,
                  );

                  return (
                    <TableRow key={code.activationCodeId} hover>
                      <TableCell>
                        <Typography
                          sx={{
                            fontSize: 12,
                            fontFamily: 'monospace',
                            fontWeight: 600,
                            overflowWrap: 'anywhere',
                          }}
                        >
                          {code.activationCode ?? 'غير متاح (رمز قديم)'}
                        </Typography>

                        <Typography
                          variant="caption"
                          color="text.secondary"
                          sx={{ fontSize: 10 }}
                        >
                          المعرف: {code.activationCodeId}
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

                      <TableCell sx={{ fontSize: 12 }}>
                        {formatDateTime(code.createdAt)}
                      </TableCell>

                      <TableCell sx={{ fontSize: 12 }}>
                        {formatDateTime(code.expiresAt)}
                      </TableCell>

                      <TableCell sx={{ fontSize: 12 }}>
                        {formatDateTime(code.redeemedAt)}
                      </TableCell>

                      <TableCell>
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<ContentCopyRounded />}
                          disabled={!code.activationCode}
                          onClick={() => {
                            if (code.activationCode) {
                              void navigator.clipboard.writeText(code.activationCode);
                            }
                          }}
                        >
                          نسخ الرمز
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })}

                {subscription.activationCodes.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={5}>
                      <Typography
                        color="text.secondary"
                        sx={{ py: 4, fontSize: 13, textAlign: 'center' }}
                        >
                        لا توجد رموز تفعيل لهذا الاشتراك.
                        </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </CardContent>
      </Card>
    </Stack>
  );
}

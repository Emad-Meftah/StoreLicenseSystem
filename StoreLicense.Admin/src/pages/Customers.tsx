
import { useEffect, useMemo, useState } from 'react';

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
  PeopleAltRounded,
  CheckCircleRounded,
  PauseCircleRounded,
  BlockRounded,
  EditRounded,
  PersonAddAlt1Rounded,
} from '@mui/icons-material';

import api from '../services/api';

type SubscriberStatus = number | string;
type StatusFilter = 'all' | '1' | '2' | '3';

interface Subscriber {
  id: string;
  storeName: string;
  ownerName: string;
  phone: string | null;
  notes: string | null;
  status: SubscriberStatus;
  createdAt: string;
  updatedAt: string;
}

interface SubscriberForm {
  storeName: string;
  ownerName: string;
  phone: string;
  notes: string;
}

const emptyForm: SubscriberForm = {
  storeName: '',
  ownerName: '',
  phone: '',
  notes: '',
};

const statusInfo: Record<
  string,
  {
    label: string;
    color: 'default' | 'success' | 'warning' | 'error';
  }
> = {
  '1': { label: 'نشط', color: 'success' },
  '2': { label: 'موقوف', color: 'warning' },
  '3': { label: 'محظور', color: 'error' },
  Active: { label: 'نشط', color: 'success' },
  Suspended: { label: 'موقوف', color: 'warning' },
  Blocked: { label: 'محظور', color: 'error' },
};

function getStatusInfo(status: SubscriberStatus) {
  return statusInfo[String(status)] ?? {
    label: 'غير معروف',
    color: 'default' as const,
  };
}

function formatDate(value: string): string {
  if (!value) return '—';

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) return '—';

  return new Intl.DateTimeFormat('ar-YE', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(date);
}

function getErrorMessage(error: unknown): string {
  if (typeof error === 'object' && error !== null && 'response' in error) {
    const axiosError = error as {
      response?: {
        status?: number;
        data?: { message?: string; title?: string };
      };
    };

    if (axiosError.response?.status === 401) {
      return 'انتهت جلسة تسجيل الدخول أو أن صلاحية الوصول غير متاحة.';
    }

    return (
      axiosError.response?.data?.message ||
      axiosError.response?.data?.title ||
      'حدث خطأ أثناء تنفيذ العملية.'
    );
  }

  return 'تعذر الاتصال بالخادم. تحقق من تشغيل واجهة API.';
}

export default function Customers() {
  const [subscribers, setSubscribers] = useState<Subscriber[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingSubscriber, setEditingSubscriber] =
    useState<Subscriber | null>(null);

  const [form, setForm] = useState<SubscriberForm>(emptyForm);
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);

  const [changingStatusId, setChangingStatusId] = useState<string | null>(
    null,
  );

  const loadSubscribers = async () => {
    try {
      setLoading(true);
      setError('');

      const response = await api.get<Subscriber[]>('/api/subscribers');

      setSubscribers(response.data);
    } catch (err: unknown) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadSubscribers();
  }, []);

  const statistics = useMemo(() => ({
    total: subscribers.length,
    active: subscribers.filter(
      (item) =>
        String(item.status) === '1' ||
        String(item.status) === 'Active',
    ).length,
    suspended: subscribers.filter(
      (item) =>
        String(item.status) === '2' ||
        String(item.status) === 'Suspended',
    ).length,
    blocked: subscribers.filter(
      (item) =>
        String(item.status) === '3' ||
        String(item.status) === 'Blocked',
    ).length,
  }), [subscribers]);

  const filteredSubscribers = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();

    return subscribers.filter((item) => {
      const matchesSearch =
        !normalizedSearch ||
        item.storeName.toLowerCase().includes(normalizedSearch) ||
        item.ownerName.toLowerCase().includes(normalizedSearch) ||
        (item.phone ?? '').toLowerCase().includes(normalizedSearch);

      const matchesStatus =
        statusFilter === 'all' ||
        String(item.status) === statusFilter ||
        String(item.status) ===
          ({ '1': 'Active', '2': 'Suspended', '3': 'Blocked' }[
            statusFilter
          ] ?? '');

      return matchesSearch && matchesStatus;
    });
  }, [subscribers, search, statusFilter]);

  const statCards = [
    {
      title: 'إجمالي العملاء',
      value: statistics.total,
      icon: <PeopleAltRounded />,
      color: '#2563EB',
      background: '#EFF6FF',
    },
    {
      title: 'العملاء النشطون',
      value: statistics.active,
      icon: <CheckCircleRounded />,
      color: '#059669',
      background: '#ECFDF5',
    },
    {
      title: 'العملاء الموقوفون',
      value: statistics.suspended,
      icon: <PauseCircleRounded />,
      color: '#D97706',
      background: '#FFFBEB',
    },
    {
      title: 'العملاء المحظورون',
      value: statistics.blocked,
      icon: <BlockRounded />,
      color: '#DC2626',
      background: '#FEF2F2',
    },
  ];

  const openCreateDialog = () => {
    setEditingSubscriber(null);
    setForm(emptyForm);
    setFormError('');
    setDialogOpen(true);
  };

  const openEditDialog = (subscriber: Subscriber) => {
    setEditingSubscriber(subscriber);

    setForm({
      storeName: subscriber.storeName,
      ownerName: subscriber.ownerName,
      phone: subscriber.phone ?? '',
      notes: subscriber.notes ?? '',
    });

    setFormError('');
    setDialogOpen(true);
  };

  const closeDialog = () => {
    if (saving) return;

    setDialogOpen(false);
    setEditingSubscriber(null);
    setForm(emptyForm);
    setFormError('');
  };

  const handleSave = async () => {
    const storeName = form.storeName.trim();
    const ownerName = form.ownerName.trim();

    if (!storeName || !ownerName) {
      setFormError('يرجى إدخال اسم المتجر واسم صاحب المتجر.');
      return;
    }

    if (storeName.length < 2 || storeName.length > 150) {
      setFormError('اسم المتجر يجب أن يكون بين حرفين و150 حرفاً.');
      return;
    }

    if (ownerName.length < 2 || ownerName.length > 150) {
      setFormError('اسم صاحب المتجر يجب أن يكون بين حرفين و150 حرفاً.');
      return;
    }

    const request = {
      storeName,
      ownerName,
      phone: form.phone.trim() || null,
      notes: form.notes.trim() || null,
    };

    try {
      setSaving(true);
      setFormError('');
      setError('');
      setSuccess('');

      if (editingSubscriber) {
        await api.put(
          `/api/subscribers/${editingSubscriber.id}`,
          request,
        );

        setSuccess('تم تحديث بيانات العميل بنجاح.');
      } else {
        await api.post('/api/subscribers', request);

        setSuccess('تمت إضافة العميل بنجاح.');
      }

      setDialogOpen(false);
      setEditingSubscriber(null);
      setForm(emptyForm);

      await loadSubscribers();
    } catch (err: unknown) {
      setFormError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const handleStatusChange = async (subscriber: Subscriber) => {
    const isActive =
      String(subscriber.status) === '1' ||
      String(subscriber.status) === 'Active';

    const action = isActive ? 'deactivate' : 'activate';

    const confirmationMessage = isActive
      ? `هل تريد إيقاف العميل "${subscriber.storeName}"؟`
      : `هل تريد تفعيل العميل "${subscriber.storeName}"؟`;

    if (!window.confirm(confirmationMessage)) return;

    try {
      setChangingStatusId(subscriber.id);
      setError('');
      setSuccess('');

      await api.patch(
        `/api/subscribers/${subscriber.id}/${action}`,
      );

      setSuccess(
        isActive
          ? 'تم إيقاف العميل بنجاح.'
          : 'تم تفعيل العميل بنجاح.',
      );

      await loadSubscribers();
    } catch (err: unknown) {
      setError(getErrorMessage(err));
    } finally {
      setChangingStatusId(null);
    }
  };

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
            العملاء
          </Typography>

          <Typography
            color="text.secondary"
            sx={{ fontSize: 14, mt: 0.5 }}
          >
            إدارة بيانات العملاء ومتابعة حالات حساباتهم.
          </Typography>
        </Box>

        <Stack direction="row" spacing={1}>
          <Button
            variant="outlined"
            startIcon={<AutorenewRounded />}
            onClick={() => void loadSubscribers()}
            disabled={loading}
          >
            تحديث
          </Button>

          <Button
            variant="contained"
            startIcon={<AddRounded />}
            onClick={openCreateDialog}
          >
            عميل جديد
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

      {/* Customers table */}
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
            قائمة العملاء
          </Typography>

          <Typography
            color="text.secondary"
            sx={{ fontSize: 12, mb: 2.5 }}
          >
            جميع العملاء المسجلين في النظام.
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
              placeholder="ابحث باسم المتجر أو المالك أو الهاتف..."
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
              <InputLabel id="subscriber-status-filter">
                حالة العميل
              </InputLabel>

              <Select
                labelId="subscriber-status-filter"
                value={statusFilter}
                label="حالة العميل"
                onChange={(event) =>
                  setStatusFilter(event.target.value as StatusFilter)
                }
              >
                <MenuItem value="all">جميع الحالات</MenuItem>
                <MenuItem value="1">نشط</MenuItem>
                <MenuItem value="2">موقوف</MenuItem>
                <MenuItem value="3">محظور</MenuItem>
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
                  onClick={() => void loadSubscribers()}
                >
                  إعادة المحاولة
                </Button>
              }
            >
              {error}
            </Alert>
          )}

          {success && (
            <Alert
              severity="success"
              sx={{ mb: 2 }}
              onClose={() => setSuccess('')}
            >
              {success}
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
                جارٍ تحميل العملاء...
              </Typography>
            </Stack>
          ) : (
            <TableContainer sx={{ overflowX: 'auto' }}>
              <Table sx={{ minWidth: 850 }}>
                <TableHead>
                  <TableRow sx={{ bgcolor: '#F8FAFC' }}>
                    <TableCell sx={{ fontWeight: 700 }}>المتجر</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>صاحب المتجر</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>رقم الهاتف</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>الحالة</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>تاريخ التسجيل</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>الإجراءات</TableCell>
                  </TableRow>
                </TableHead>

                <TableBody>
                  {filteredSubscribers.map((item) => {
                    const status = getStatusInfo(item.status);
                    const isActive =
                      String(item.status) === '1' ||
                      String(item.status) === 'Active';

                    return (
                      <TableRow
                        key={item.id}
                        hover
                        sx={{
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
                            {item.phone || '—'}
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
                          <Typography sx={{ fontSize: 12 }}>
                            {formatDate(item.createdAt)}
                          </Typography>
                        </TableCell>

                        <TableCell>
                          <Stack direction="row" spacing={0.5}>
                            <Button
                              size="small"
                              color="primary"
                              variant="outlined"
                              startIcon={<EditRounded />}
                              onClick={() => openEditDialog(item)}
                            >
                              تعديل
                            </Button>

                            <Button
                              size="small"
                              color={isActive ? 'warning' : 'success'}
                              variant="outlined"
                              disabled={changingStatusId === item.id}
                              onClick={() => void handleStatusChange(item)}
                            >
                              {changingStatusId === item.id
                                ? 'جارٍ...'
                                : isActive
                                  ? 'إيقاف'
                                  : 'تفعيل'}
                            </Button>
                          </Stack>
                        </TableCell>
                      </TableRow>
                    );
                  })}

                  {filteredSubscribers.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={6}>
                        <Stack
                          spacing={1}
                          sx={{ alignItems: 'center', py: 6 }}
                        >
                          <PeopleAltRounded
                            sx={{ fontSize: 42, color: '#CBD5E1' }}
                          />

                          <Typography sx={{ fontWeight: 600 }}>
                            لا يوجد عملاء
                          </Typography>

                          <Typography
                            color="text.secondary"
                            sx={{ fontSize: 12 }}
                          >
                            {subscribers.length === 0
                              ? 'لم يتم تسجيل أي عميل حتى الآن.'
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

          {!loading && subscribers.length > 0 && (
            <Typography
              color="text.secondary"
              sx={{ fontSize: 12, mt: 2 }}
            >
              عرض {filteredSubscribers.length} من أصل {subscribers.length} عميل.
            </Typography>
          )}
        </CardContent>
      </Card>

      {/* Create / Edit dialog */}
      <Dialog
        open={dialogOpen}
        onClose={closeDialog}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle sx={{ fontWeight: 700 }}>
          {editingSubscriber ? 'تعديل بيانات العميل' : 'إضافة عميل جديد'}
        </DialogTitle>

        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {formError && (
              <Alert severity="error">
                {formError}
              </Alert>
            )}

            <TextField
              label="اسم المتجر"
              required
              fullWidth
              value={form.storeName}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  storeName: event.target.value,
                }))
              }
              slotProps={{
                htmlInput: {
                    maxLength: 150,
                },
                }}
            />

            <TextField
              label="اسم صاحب المتجر"
              required
              fullWidth
              value={form.ownerName}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  ownerName: event.target.value,
                }))
              }
              slotProps={{
                htmlInput: {
                    maxLength: 150,
                },
                }}
            />

            <TextField
              label="رقم الهاتف"
              fullWidth
              value={form.phone}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  phone: event.target.value,
                }))
              }
            />

            <TextField
              label="ملاحظات"
              fullWidth
              multiline
              minRows={3}
              value={form.notes}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  notes: event.target.value,
                }))
              }
            />
          </Stack>
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button
            onClick={closeDialog}
            color="inherit"
            disabled={saving}
          >
            إلغاء
          </Button>

          <Button
            variant="contained"
            startIcon={
              editingSubscriber ? <EditRounded /> : <PersonAddAlt1Rounded />
            }
            onClick={() => void handleSave()}
            disabled={saving}
          >
            {saving
              ? 'جارٍ الحفظ...'
              : editingSubscriber
                ? 'حفظ التعديلات'
                : 'إضافة العميل'}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}

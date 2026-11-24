
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
  FormControlLabel,
  InputAdornment,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
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
  CardGiftcardRounded,
  CheckCircleRounded,
  BlockRounded,
  AllInclusiveRounded,
  EditRounded,
  ToggleOnRounded,
  ToggleOffRounded,
} from '@mui/icons-material';

import api from '../services/api';

interface SubscriptionPlan {
  id: string;
  name: string;
  durationDays: number | null;
  isLifetime: boolean;
  defaultMaxDevices: number;
  isActive: boolean;
  createdAt: string;
}

interface PlanForm {
  name: string;
  isLifetime: boolean;
  durationDays: string;
  defaultMaxDevices: string;
}

type StatusFilter = 'all' | 'active' | 'inactive';
type TypeFilter = 'all' | 'lifetime' | 'limited';

const emptyForm: PlanForm = {
  name: '',
  isLifetime: false,
  durationDays: '30',
  defaultMaxDevices: '1',
};

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

function getErrorMessage(
  error: unknown,
  fallback: string,
): string {
  if (
    typeof error === 'object' &&
    error !== null &&
    'response' in error
  ) {
    const axiosError = error as {
      response?: {
        status?: number;
        data?: { message?: string };
      };
    };

    if (axiosError.response?.status === 401) {
      return 'انتهت جلسة تسجيل الدخول أو أن صلاحية الوصول غير متاحة.';
    }

    if (axiosError.response?.data?.message) {
      return axiosError.response.data.message;
    }
  }

  return fallback;
}

export default function SubscriptionPlans() {
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [loading, setLoading] = useState(true);

  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] =
    useState<StatusFilter>('all');
  const [typeFilter, setTypeFilter] =
    useState<TypeFilter>('all');

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingPlan, setEditingPlan] =
    useState<SubscriptionPlan | null>(null);

  const [form, setForm] = useState<PlanForm>(emptyForm);
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const [changingStatusId, setChangingStatusId] =
    useState<string | null>(null);

  const loadPlans = async () => {
    try {
      setLoading(true);
      setError('');

      const response =
        await api.get<SubscriptionPlan[]>('/api/subscription-plans');

      setPlans(response.data);
    } catch (err: unknown) {
      setError(
        getErrorMessage(
          err,
          'تعذر تحميل خطط الاشتراك من الخادم.',
        ),
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadPlans();
  }, []);

  const statistics = useMemo(() => ({
    total: plans.length,
    active: plans.filter((plan) => plan.isActive).length,
    inactive: plans.filter((plan) => !plan.isActive).length,
    lifetime: plans.filter((plan) => plan.isLifetime).length,
  }), [plans]);

  const filteredPlans = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();

    return plans.filter((plan) => {
      const matchesSearch =
        !normalizedSearch ||
        plan.name.toLowerCase().includes(normalizedSearch);

      const matchesStatus =
        statusFilter === 'all' ||
        (statusFilter === 'active' && plan.isActive) ||
        (statusFilter === 'inactive' && !plan.isActive);

      const matchesType =
        typeFilter === 'all' ||
        (typeFilter === 'lifetime' && plan.isLifetime) ||
        (typeFilter === 'limited' && !plan.isLifetime);

      return matchesSearch && matchesStatus && matchesType;
    });
  }, [plans, search, statusFilter, typeFilter]);

  const statCards = [
    {
      title: 'إجمالي الخطط',
      value: statistics.total,
      icon: <CardGiftcardRounded />,
      color: '#2563EB',
      background: '#EFF6FF',
    },
    {
      title: 'الخطط المفعّلة',
      value: statistics.active,
      icon: <CheckCircleRounded />,
      color: '#059669',
      background: '#ECFDF5',
    },
    {
      title: 'الخطط المعطّلة',
      value: statistics.inactive,
      icon: <BlockRounded />,
      color: '#DC2626',
      background: '#FEF2F2',
    },
    {
      title: 'خطط مدى الحياة',
      value: statistics.lifetime,
      icon: <AllInclusiveRounded />,
      color: '#7C3AED',
      background: '#F5F3FF',
    },
  ];

  const openCreateDialog = () => {
    setEditingPlan(null);
    setForm(emptyForm);
    setFormError('');
    setError('');
    setSuccess('');
    setDialogOpen(true);
  };

  const openEditDialog = (plan: SubscriptionPlan) => {
    setEditingPlan(plan);

    setForm({
      name: plan.name,
      isLifetime: plan.isLifetime,
      durationDays: plan.durationDays?.toString() ?? '',
      defaultMaxDevices: plan.defaultMaxDevices.toString(),
    });

    setFormError('');
    setError('');
    setSuccess('');
    setDialogOpen(true);
  };

  const closeDialog = () => {
    if (saving) return;

    setDialogOpen(false);
    setEditingPlan(null);
    setForm(emptyForm);
    setFormError('');
  };

  const handleSave = async () => {
    setFormError('');

    const name = form.name.trim();

    if (name.length < 2 || name.length > 100) {
      setFormError('اسم الخطة يجب أن يكون بين حرفين و100 حرف.');
      return;
    }

    const maxDevices = Number(form.defaultMaxDevices);

    if (
      !Number.isInteger(maxDevices) ||
      maxDevices < 1 ||
      maxDevices > 1000
    ) {
      setFormError('عدد الأجهزة يجب أن يكون بين 1 و1000.');
      return;
    }

    let durationDays: number | null = null;

    if (!form.isLifetime) {
      durationDays = Number(form.durationDays);

      if (
        !Number.isInteger(durationDays) ||
        durationDays < 1 ||
        durationDays > 36500
      ) {
        setFormError(
          'مدة الاشتراك يجب أن تكون بين يوم واحد و36500 يوم.',
        );
        return;
      }
    }

    const payload = {
      name,
      isLifetime: form.isLifetime,
      durationDays,
      defaultMaxDevices: maxDevices,
    };

    try {
      setSaving(true);
      setFormError('');

      if (editingPlan) {
        await api.put(
          `/api/subscription-plans/${editingPlan.id}`,
          payload,
        );

        setSuccess('تم تعديل خطة الاشتراك بنجاح.');
      } else {
        await api.post('/api/subscription-plans', payload);

        setSuccess('تم إنشاء خطة الاشتراك بنجاح.');
      }

      setDialogOpen(false);
      setEditingPlan(null);
      setForm(emptyForm);

      await loadPlans();
    } catch (err: unknown) {
      setFormError(
        getErrorMessage(
          err,
          'تعذر حفظ الخطة. تحقق من الاتصال بالخادم.',
        ),
      );
    } finally {
      setSaving(false);
    }
  };

  const handleChangeStatus = async (plan: SubscriptionPlan) => {
    const action = plan.isActive ? 'تعطيل' : 'تفعيل';

    const confirmed = window.confirm(
      `هل أنت متأكد من ${action} خطة "${plan.name}"؟`,
    );

    if (!confirmed) return;

    try {
      setChangingStatusId(plan.id);
      setError('');
      setSuccess('');

      const endpoint = plan.isActive ? 'deactivate' : 'activate';

      await api.patch(
        `/api/subscription-plans/${plan.id}/${endpoint}`,
      );

      setSuccess(
        plan.isActive
          ? 'تم تعطيل الخطة بنجاح.'
          : 'تم تفعيل الخطة بنجاح.',
      );

      await loadPlans();
    } catch (err: unknown) {
      setError(
        getErrorMessage(
          err,
          'تعذر تغيير حالة الخطة.',
        ),
      );
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
            خطط الاشتراك
          </Typography>

          <Typography
            color="text.secondary"
            sx={{ fontSize: 14, mt: 0.5 }}
          >
            إدارة خطط الاشتراك ومددها وعدد الأجهزة المسموح بها.
          </Typography>
        </Box>

        <Stack direction="row" spacing={1}>
          <Button
            variant="outlined"
            startIcon={<AutorenewRounded />}
            onClick={() => void loadPlans()}
            disabled={loading}
          >
            تحديث
          </Button>

          <Button
            variant="contained"
            startIcon={<AddRounded />}
            onClick={openCreateDialog}
          >
            إضافة خطة
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

      {error && (
        <Alert
          severity="error"
          onClose={() => setError('')}
          action={
            <Button
              color="inherit"
              size="small"
              onClick={() => void loadPlans()}
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
          onClose={() => setSuccess('')}
        >
          {success}
        </Alert>
      )}

      {/* Plans table */}
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
            قائمة خطط الاشتراك
          </Typography>

          <Typography
            color="text.secondary"
            sx={{ fontSize: 12, mb: 2.5 }}
          >
            جميع الخطط المسجلة في النظام، مع إمكانية تعديلها وتغيير حالتها.
          </Typography>

          {/* Search and filters */}
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={1.5}
            sx={{ mb: 2.5 }}
          >
            <TextField
              fullWidth
              size="small"
              placeholder="ابحث باسم الخطة..."
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
              sx={{ minWidth: { xs: '100%', md: 160 } }}
            >
              <InputLabel id="plan-status-filter">
                حالة الخطة
              </InputLabel>

              <Select
                labelId="plan-status-filter"
                value={statusFilter}
                label="حالة الخطة"
                onChange={(event) =>
                  setStatusFilter(event.target.value as StatusFilter)
                }
              >
                <MenuItem value="all">جميع الحالات</MenuItem>
                <MenuItem value="active">مفعّلة</MenuItem>
                <MenuItem value="inactive">معطّلة</MenuItem>
              </Select>
            </FormControl>

            <FormControl
              size="small"
              sx={{ minWidth: { xs: '100%', md: 170 } }}
            >
              <InputLabel id="plan-type-filter">
                نوع الخطة
              </InputLabel>

              <Select
                labelId="plan-type-filter"
                value={typeFilter}
                label="نوع الخطة"
                onChange={(event) =>
                  setTypeFilter(event.target.value as TypeFilter)
                }
              >
                <MenuItem value="all">جميع الأنواع</MenuItem>
                <MenuItem value="limited">محددة المدة</MenuItem>
                <MenuItem value="lifetime">مدى الحياة</MenuItem>
              </Select>
            </FormControl>
          </Stack>

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
                جارٍ تحميل خطط الاشتراك...
              </Typography>
            </Stack>
          ) : (
            <TableContainer sx={{ overflowX: 'auto' }}>
              <Table sx={{ minWidth: 850 }}>
                <TableHead>
                  <TableRow sx={{ bgcolor: '#F8FAFC' }}>
                    <TableCell sx={{ fontWeight: 700 }}>اسم الخطة</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>نوع الاشتراك</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>المدة</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>عدد الأجهزة</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>الحالة</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>تاريخ الإنشاء</TableCell>
                    <TableCell sx={{ fontWeight: 700 }}>الإجراءات</TableCell>
                  </TableRow>
                </TableHead>

                <TableBody>
                  {filteredPlans.map((plan) => (
                    <TableRow
                      key={plan.id}
                      hover
                      sx={{
                        '&:last-child td': { borderBottom: 0 },
                      }}
                    >
                      <TableCell>
                        <Typography sx={{ fontWeight: 600, fontSize: 13 }}>
                          {plan.name}
                        </Typography>
                      </TableCell>

                      <TableCell>
                        <Chip
                          label={
                            plan.isLifetime
                              ? 'مدى الحياة'
                              : 'محددة المدة'
                          }
                          color={plan.isLifetime ? 'secondary' : 'info'}
                          size="small"
                          variant="outlined"
                        />
                      </TableCell>

                      <TableCell>
                        <Typography sx={{ fontSize: 13 }}>
                          {plan.isLifetime
                            ? 'غير محدودة'
                            : `${plan.durationDays ?? '—'} يوم`}
                        </Typography>
                      </TableCell>

                      <TableCell>
                        <Typography sx={{ fontSize: 13, fontWeight: 600 }}>
                          {plan.defaultMaxDevices}
                        </Typography>
                      </TableCell>

                      <TableCell>
                        <Chip
                          label={plan.isActive ? 'مفعّلة' : 'معطّلة'}
                          color={plan.isActive ? 'success' : 'default'}
                          size="small"
                          variant="outlined"
                        />
                      </TableCell>

                      <TableCell>
                        <Typography sx={{ fontSize: 12 }}>
                          {formatDate(plan.createdAt)}
                        </Typography>
                      </TableCell>

                      <TableCell>
                        <Stack direction="row" spacing={0.5}>
                          <Button
                            size="small"
                            variant="outlined"
                            color="primary"
                            startIcon={<EditRounded />}
                            onClick={() => openEditDialog(plan)}
                          >
                            تعديل
                          </Button>

                          <Button
                            size="small"
                            variant="outlined"
                            color={plan.isActive ? 'warning' : 'success'}
                            startIcon={
                              plan.isActive
                                ? <ToggleOffRounded />
                                : <ToggleOnRounded />
                            }
                            disabled={changingStatusId === plan.id}
                            onClick={() => void handleChangeStatus(plan)}
                          >
                            {changingStatusId === plan.id
                              ? 'جارٍ التغيير...'
                              : plan.isActive
                                ? 'تعطيل'
                                : 'تفعيل'}
                          </Button>
                        </Stack>
                      </TableCell>
                    </TableRow>
                  ))}

                  {filteredPlans.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={7}>
                        <Stack
                          spacing={1}
                          sx={{ alignItems: 'center', py: 6 }}
                        >
                          <CardGiftcardRounded
                            sx={{ fontSize: 42, color: '#CBD5E1' }}
                          />

                          <Typography sx={{ fontWeight: 600 }}>
                            لا توجد خطط اشتراك
                          </Typography>

                          <Typography
                            color="text.secondary"
                            sx={{ fontSize: 12 }}
                          >
                            {plans.length === 0
                              ? 'لم تتم إضافة أي خطة اشتراك حتى الآن.'
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

          {!loading && plans.length > 0 && (
            <Typography
              color="text.secondary"
              sx={{ fontSize: 12, mt: 2 }}
            >
              عرض {filteredPlans.length} من أصل {plans.length} خطة.
            </Typography>
          )}
        </CardContent>
      </Card>

      {/* Create and edit dialog */}
      <Dialog
        open={dialogOpen}
        onClose={closeDialog}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle sx={{ fontWeight: 700 }}>
          {editingPlan ? 'تعديل خطة الاشتراك' : 'إضافة خطة اشتراك جديدة'}
        </DialogTitle>

        <DialogContent>
          <Stack spacing={2.5} sx={{ pt: 1 }}>
            {formError && (
              <Alert severity="error">
                {formError}
              </Alert>
            )}

            <TextField
              fullWidth
              label="اسم الخطة"
              value={form.name}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  name: event.target.value,
                }))
              }
              required
              size="medium"
              placeholder="مثال: اشتراك شهري"
              slotProps={{
                htmlInput: {
                  maxLength: 100,
                },
              }}
            />

            <FormControlLabel
              control={
                <Switch
                  checked={form.isLifetime}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      isLifetime: event.target.checked,
                      durationDays: event.target.checked
                        ? ''
                        : current.durationDays || '30',
                    }))
                  }
                />
              }
              label="اشتراك مدى الحياة"
            />

            {!form.isLifetime && (
              <TextField
                fullWidth
                label="مدة الاشتراك بالأيام"
                type="number"
                value={form.durationDays}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    durationDays: event.target.value,
                  }))
                }
                required
                inputMode="numeric"
                helperText="يجب أن تكون المدة بين يوم واحد و36500 يوم."
                slotProps={{
                  htmlInput: {
                    min: 1,
                    max: 36500,
                  },
                }}
              />
            )}

            <TextField
              fullWidth
              label="الحد الأقصى للأجهزة"
              type="number"
              value={form.defaultMaxDevices}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  defaultMaxDevices: event.target.value,
                }))
              }
              required
              helperText="عدد الأجهزة المسموح بها من 1 إلى 1000."
              slotProps={{
                htmlInput: {
                  min: 1,
                  max: 1000,
                },
              }}
            />

            {form.isLifetime && (
              <Alert severity="info">
                هذه الخطة تسمح بالاشتراك مدى الحياة، ولا تحتاج إلى تحديد مدة.
              </Alert>
            )}
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
            onClick={() => void handleSave()}
            disabled={saving}
          >
            {saving
              ? 'جارٍ الحفظ...'
              : editingPlan
                ? 'حفظ التعديلات'
                : 'إنشاء الخطة'}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}

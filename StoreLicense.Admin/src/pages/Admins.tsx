
import { useState } from 'react';

import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material';

import {
  AdminPanelSettingsRounded,
  PersonAddAlt1Rounded,
  LockRounded,
  EmailRounded,
  PersonRounded,
} from '@mui/icons-material';

import api from '../services/api';

interface AdminForm {
  fullName: string;
  email: string;
  password: string;
}

const emptyForm: AdminForm = {
  fullName: '',
  email: '',
  password: '',
};

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
      'حدث خطأ أثناء إنشاء المسؤول.'
    );
  }

  return 'تعذر الاتصال بالخادم. تحقق من تشغيل واجهة API.';
}

export default function Admins() {
  const [form, setForm] = useState<AdminForm>(emptyForm);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const handleCreateAdmin = async () => {
    const fullName = form.fullName.trim();
    const email = form.email.trim();
    const password = form.password;

    if (!fullName || !email || !password) {
      setError('يرجى تعبئة جميع الحقول المطلوبة.');
      return;
    }

    if (fullName.length > 150) {
      setError('الاسم الكامل يجب ألا يتجاوز 150 حرفاً.');
      return;
    }

    if (email.length > 254) {
      setError('البريد الإلكتروني يجب ألا يتجاوز 254 حرفاً.');
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      setError('يرجى إدخال بريد إلكتروني صحيح.');
      return;
    }

    if (password.length < 12) {
      setError('كلمة المرور يجب ألا تقل عن 12 حرفاً.');
      return;
    }

    try {
      setLoading(true);
      setError('');
      setSuccess('');

      await api.post('/api/admin-auth/admins', {
        fullName,
        email,
        password,
      });

      setSuccess(`تم إنشاء حساب المسؤول "${fullName}" بنجاح.`);
      setForm(emptyForm);
    } catch (err: unknown) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          إدارة المسؤولين
        </Typography>

        <Typography
          color="text.secondary"
          sx={{ fontSize: 14, mt: 0.5 }}
        >
          إنشاء حسابات جديدة للمسؤولين الذين يمكنهم الدخول إلى لوحة الإدارة.
        </Typography>
      </Box>

      <Card
        elevation={0}
        sx={{
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 3,
          maxWidth: 750,
        }}
      >
        <CardContent sx={{ p: { xs: 2, sm: 3 } }}>
          <Stack spacing={2.5}>
            <Stack
            direction="row"
            spacing={1.5}
            sx={{ alignItems: 'center' }}
            >
              <Box
                sx={{
                  width: 48,
                  height: 48,
                  borderRadius: 2.5,
                  bgcolor: '#EFF6FF',
                  color: '#2563EB',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <AdminPanelSettingsRounded sx={{ fontSize: 28 }} />
              </Box>

              <Box>
                <Typography sx={{ fontWeight: 700, fontSize: 17 }}>
                  إضافة مسؤول جديد
                </Typography>

                <Typography
                  color="text.secondary"
                  sx={{ fontSize: 12, mt: 0.3 }}
                >
                  أدخل بيانات المسؤول لإنشاء حسابه.
                </Typography>
              </Box>
            </Stack>

            {error && (
              <Alert severity="error" onClose={() => setError('')}>
                {error}
              </Alert>
            )}

            {success && (
              <Alert severity="success" onClose={() => setSuccess('')}>
                {success}
              </Alert>
            )}

            <TextField
              label="الاسم الكامل"
              required
              fullWidth
              value={form.fullName}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  fullName: event.target.value,
                }))
              }
              slotProps={{
                htmlInput: {
                  maxLength: 150,
                },
                input: {
                  startAdornment: (
                    <PersonRounded
                      color="action"
                      sx={{ mr: 1 }}
                    />
                  ),
                },
              }}
            />

            <TextField
              label="البريد الإلكتروني"
              type="email"
              required
              fullWidth
              value={form.email}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  email: event.target.value,
                }))
              }
              slotProps={{
                htmlInput: {
                  maxLength: 254,
                },
                input: {
                  startAdornment: (
                    <EmailRounded
                      color="action"
                      sx={{ mr: 1 }}
                    />
                  ),
                },
              }}
            />

            <TextField
              label="كلمة المرور"
              type="password"
              required
              fullWidth
              value={form.password}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  password: event.target.value,
                }))
              }
              helperText="يجب ألا تقل كلمة المرور عن 12 حرفاً."
              slotProps={{
                htmlInput: {
                  minLength: 12,
                },
                input: {
                  startAdornment: (
                    <LockRounded
                      color="action"
                      sx={{ mr: 1 }}
                    />
                  ),
                },
              }}
            />

            <Button
              variant="contained"
              size="large"
              startIcon={<PersonAddAlt1Rounded />}
              onClick={() => void handleCreateAdmin()}
              disabled={loading}
              sx={{ alignSelf: 'flex-start', minWidth: 170 }}
            >
              {loading ? (
                <>
                  <CircularProgress size={18} color="inherit" sx={{ mr: 1 }} />
                  جارٍ الإنشاء...
                </>
              ) : (
                'إنشاء المسؤول'
              )}
            </Button>
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}

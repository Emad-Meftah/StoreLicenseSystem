
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

import {
  Alert,
  Avatar,
  Box,
  Button,
  Checkbox,
  Container,
  FormControlLabel,
  IconButton,
  InputAdornment,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';

import {
  AdminPanelSettingsRounded,
  VisibilityRounded,
  VisibilityOffRounded,
  LockOutlined,
  EmailOutlined,
} from '@mui/icons-material';

import axios from 'axios';
import api from '../services/api';

interface AdminLoginResponse {
  accessToken: string;
  tokenType: string;
  expiresInSeconds: number;
  adminId: number;
  email: string;
  fullName: string;
}

export default function Login() {
  const navigate = useNavigate();

  const [showPassword, setShowPassword] = useState(false);

  const [email, setEmail] = useState(
    localStorage.getItem('admin_remembered_email') || '',
  );

  const [password, setPassword] = useState('');

  const [rememberEmail, setRememberEmail] = useState(
    Boolean(localStorage.getItem('admin_remembered_email')),
  );

  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const handleLogin = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setErrorMessage('');
    setSuccessMessage('');
    setLoading(true);

    try {
      const response = await api.post<AdminLoginResponse>(
        '/api/admin-auth/login',
        {
          email: email.trim(),
          password,
        },
      );

      const admin = response.data;

      sessionStorage.setItem('admin_access_token', admin.accessToken);
      sessionStorage.setItem('admin_id', String(admin.adminId));
      sessionStorage.setItem('admin_email', admin.email);
      sessionStorage.setItem('admin_full_name', admin.fullName);

      if (rememberEmail) {
        localStorage.setItem('admin_remembered_email', email.trim());
      } else {
        localStorage.removeItem('admin_remembered_email');
      }

      setPassword('');

      navigate('/', { replace: true });
    } catch (error: unknown) {
      if (axios.isAxiosError(error)) {
        const serverMessage =
          error.response?.data?.message ||
          error.response?.data?.title;

        setErrorMessage(
          serverMessage ||
            (error.response?.status === 401
              ? 'البريد الإلكتروني أو كلمة المرور غير صحيحة.'
              : 'تعذر الاتصال بالخادم. تحقق من تشغيل API وحاول مرة أخرى.'),
        );
      } else {
        setErrorMessage('حدث خطأ غير متوقع أثناء تسجيل الدخول.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        px: 2,
        py: 4,
        background:
          'linear-gradient(135deg, #EFF6FF 0%, #F8FAFC 50%, #EEF2FF 100%)',
      }}
    >
      <Container maxWidth="sm">
        <Paper
          elevation={0}
          sx={{
            p: { xs: 3, sm: 5 },
            borderRadius: 4,
            border: '1px solid #E8ECF3',
            boxShadow: '0 20px 60px rgba(30, 64, 175, 0.08)',
          }}
        >
          <Stack spacing={1} sx={{ alignItems: 'center' }}>
            <Avatar
              variant="rounded"
              sx={{
                width: 68,
                height: 68,
                bgcolor: 'primary.main',
                borderRadius: 3,
              }}
            >
              <AdminPanelSettingsRounded
                sx={{ fontSize: 38, color: '#FFFFFF' }}
              />
            </Avatar>

            <Typography
              variant="h5"
              color="#172033"
              sx={{ fontWeight: 700, mt: 1 }}
            >
              StoreLicense
            </Typography>

            <Typography color="text.secondary" sx={{ fontSize: 13 }}>
              نظام إدارة الاشتراكات والتراخيص
            </Typography>
          </Stack>

          <Box sx={{ mt: 4, mb: 3 }}>
            <Typography variant="h5" sx={{ fontWeight: 700 }}>
              مرحباً بعودتك
            </Typography>

            <Typography
              color="text.secondary"
              sx={{ fontSize: 13, mt: 1 }}
            >
              سجّل الدخول للوصول إلى لوحة الإدارة.
            </Typography>
          </Box>

          <Box component="form" onSubmit={handleLogin}>
            <Stack spacing={2.5}>
              <Box>
                <Typography
                  sx={{ fontSize: 13, fontWeight: 600, mb: 1 }}
                >
                  البريد الإلكتروني
                </Typography>

                <TextField
                  fullWidth
                  placeholder="أدخل البريد الإلكتروني"
                  type="email"
                  autoComplete="username"
                  required
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  slotProps={{
                    input: {
                      startAdornment: (
                        <InputAdornment position="start">
                          <EmailOutlined color="action" />
                        </InputAdornment>
                      ),
                    },
                  }}
                />
              </Box>

              <Box>
                <Typography
                  sx={{ fontSize: 13, fontWeight: 600, mb: 1 }}
                >
                  كلمة المرور
                </Typography>

                <TextField
                  fullWidth
                  placeholder="أدخل كلمة المرور"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  required
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  slotProps={{
                    input: {
                      startAdornment: (
                        <InputAdornment position="start">
                          <LockOutlined color="action" />
                        </InputAdornment>
                      ),
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            onClick={() => setShowPassword(!showPassword)}
                            edge="end"
                            aria-label="إظهار أو إخفاء كلمة المرور"
                          >
                            {showPassword ? (
                              <VisibilityOffRounded />
                            ) : (
                              <VisibilityRounded />
                            )}
                          </IconButton>
                        </InputAdornment>
                      ),
                    },
                  }}
                />
              </Box>

              <FormControlLabel
                control={
                  <Checkbox
                    checked={rememberEmail}
                    onChange={(event) =>
                      setRememberEmail(event.target.checked)
                    }
                    size="small"
                  />
                }
                label={
                  <Typography sx={{ fontSize: 13 }}>
                    تذكر البريد الإلكتروني
                  </Typography>
                }
              />

              {errorMessage && (
                <Alert severity="error">{errorMessage}</Alert>
              )}

              {successMessage && (
                <Alert severity="success">{successMessage}</Alert>
              )}

              <Button
                type="submit"
                variant="contained"
                size="large"
                fullWidth
                disabled={loading}
                sx={{
                  py: 1.5,
                  borderRadius: 2.5,
                  fontSize: 15,
                  boxShadow: 'none',
                }}
              >
                {loading ? 'جارٍ تسجيل الدخول...' : 'تسجيل الدخول'}
              </Button>
            </Stack>
          </Box>

          <Typography
            color="text.secondary"
            sx={{
              textAlign: 'center',
              fontSize: 11,
              mt: 4,
            }}
          >
            © {new Date().getFullYear()} StoreLicense System
          </Typography>
        </Paper>
      </Container>
    </Box>
  );
}

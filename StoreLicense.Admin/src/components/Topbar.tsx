
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

import {
  AppBar,
  Toolbar,
  Box,
  IconButton,
  Typography,
  Avatar,
  Badge,
  Menu,
  MenuItem,
  Divider,
  useMediaQuery,
  useTheme,
} from '@mui/material';

import {
  MenuRounded,
  NotificationsNoneRounded,
  KeyboardArrowDownRounded,
  LogoutRounded,
  PersonOutlineRounded,
} from '@mui/icons-material';

interface TopbarProps {
  onMenuClick: () => void;
  title?: string;
}

export default function Topbar({
  onMenuClick,
  title = 'لوحة المعلومات',
}: TopbarProps) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const navigate = useNavigate();

  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);

  const fullName =
    sessionStorage.getItem('admin_full_name') || 'المدير';

  const email =
    sessionStorage.getItem('admin_email') || 'مسؤول النظام';

  const avatarLetter = fullName.trim().charAt(0) || 'م';

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setMenuAnchor(event.currentTarget);
  };

  const handleMenuClose = () => {
    setMenuAnchor(null);
  };

  const handleLogout = () => {
    sessionStorage.removeItem('admin_access_token');
    sessionStorage.removeItem('admin_id');
    sessionStorage.removeItem('admin_email');
    sessionStorage.removeItem('admin_full_name');

    handleMenuClose();

    navigate('/login', { replace: true });
  };

  return (
    <AppBar
      position="sticky"
      elevation={0}
      color="inherit"
      sx={{
        bgcolor: '#FFFFFF',
        borderBottom: '1px solid #E8ECF3',
      }}
    >
      <Toolbar
        sx={{
          minHeight: '72px !important',
          px: { xs: 2, md: 3 },
          gap: 2,
        }}
      >
        {isMobile && (
          <IconButton onClick={onMenuClick} edge="start">
            <MenuRounded />
          </IconButton>
        )}

        <Box sx={{ flex: 1 }}>
          <Typography sx={{ fontSize: 18, fontWeight: 700, color: '#172033' }}>
            {title}
          </Typography>

          {!isMobile && (
            <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
              مرحباً بك في نظام إدارة الاشتراكات والتراخيص
            </Typography>
          )}
        </Box>

        <IconButton>
          <Badge badgeContent={0} color="error">
            <NotificationsNoneRounded />
          </Badge>
        </IconButton>

        <Divider orientation="vertical" flexItem sx={{ mx: 1, my: 2 }} />

        <Box
          onClick={handleMenuOpen}
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1,
            cursor: 'pointer',
          }}
        >
          <Avatar
            sx={{
              width: 38,
              height: 38,
              bgcolor: '#DBEAFE',
              color: '#2563EB',
              fontWeight: 700,
            }}
          >
            {avatarLetter}
          </Avatar>

          {!isMobile && (
            <Box>
              <Typography sx={{ fontSize: 13, fontWeight: 700 }}>
                {fullName}
              </Typography>

              <Typography sx={{ fontSize: 11, color: 'text.secondary' }}>
                {email}
              </Typography>
            </Box>
          )}

          <KeyboardArrowDownRounded fontSize="small" />
        </Box>

        <Menu
          anchorEl={menuAnchor}
          open={Boolean(menuAnchor)}
          onClose={handleMenuClose}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
          transformOrigin={{ vertical: 'top', horizontal: 'left' }}
        >
          <MenuItem onClick={handleMenuClose}>
            <PersonOutlineRounded sx={{ ml: 1 }} fontSize="small" />
            الملف الشخصي
          </MenuItem>

          <Divider />

          <MenuItem
            onClick={handleLogout}
            sx={{ color: 'error.main' }}
          >
            <LogoutRounded sx={{ ml: 1 }} fontSize="small" />
            تسجيل الخروج
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  );
}

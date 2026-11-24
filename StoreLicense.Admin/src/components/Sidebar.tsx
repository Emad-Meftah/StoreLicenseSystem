
import {
  Box,
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Typography,
  Divider,
  useMediaQuery,
  useTheme,
} from '@mui/material';

import { useLocation, useNavigate } from 'react-router-dom';

import {
  DashboardRounded,
  PeopleAltRounded,
  SubscriptionsRounded,
  CardGiftcardRounded,
  DevicesRounded,
  AdminPanelSettingsRounded,
  HistoryRounded,
  SettingsRounded,
} from '@mui/icons-material';

interface SidebarProps {
  open: boolean;
  onClose: () => void;
}

const menuItems = [
  { title: 'لوحة المعلومات', icon: <DashboardRounded />, path: '/' },
  { title: 'العملاء', icon: <PeopleAltRounded />, path: '/customers' },
  { title: 'الاشتراكات', icon: <SubscriptionsRounded />, path: '/subscriptions' },
  { title: 'خطط الاشتراك', icon: <CardGiftcardRounded />, path: '/plans' },
  { title: 'الأجهزة', icon: <DevicesRounded />, path: '/devices' },
  { title: 'المديرون', icon: <AdminPanelSettingsRounded />, path: '/admins' },
  { title: 'سجل العمليات', icon: <HistoryRounded />, path: '/activity' },
  { title: 'الإعدادات', icon: <SettingsRounded />, path: '/settings' },
];

export default function Sidebar({ open, onClose }: SidebarProps) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const location = useLocation();
  const navigate = useNavigate();

  const drawerContent = (
    <Box
      sx={{
        width: 270,
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        bgcolor: '#FFFFFF',
      }}
    >
      <Box sx={{ px: 3, py: 3, display: 'flex', alignItems: 'center', gap: 1.5 }}>
        <Box
          sx={{
            width: 42,
            height: 42,
            borderRadius: 2.5,
            bgcolor: 'primary.main',
            color: '#FFFFFF',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontWeight: 700,
            fontSize: 21,
          }}
        >
          S
        </Box>

        <Box>
          <Typography sx={{ fontWeight: 700, fontSize: 18, color: '#172033' }}>
            StoreLicense
          </Typography>
          <Typography sx={{ fontSize: 11, color: 'text.secondary' }}>
            لوحة الإدارة
          </Typography>
        </Box>
      </Box>

      <Divider />

      <Box sx={{ px: 2, pt: 3, pb: 1 }}>
        <Typography sx={{ fontSize: 11, color: 'text.secondary', fontWeight: 700 }}>
          القائمة الرئيسية
        </Typography>
      </Box>

      <List sx={{ px: 1.5, flex: 1 }}>
        {menuItems.map((item) => {
          const isActive =
            location.pathname === item.path ||
            (item.path !== '/' &&
              location.pathname.startsWith(`${item.path}/`));

          return (
            <ListItemButton
              key={item.path}
              selected={isActive}
              onClick={() => {
                navigate(item.path);

                if (isMobile) {
                  onClose();
                }
              }}
              sx={{
                borderRadius: 2,
                mb: 0.5,
                py: 1.2,
                color: isActive ? 'primary.main' : '#64748B',
                '&.Mui-selected': {
                  bgcolor: '#EFF6FF',
                  color: 'primary.main',
                },
                '&.Mui-selected:hover': {
                  bgcolor: '#DBEAFE',
                },
                '&:hover': {
                  bgcolor: '#F8FAFC',
                },
              }}
            >
              <ListItemIcon
                sx={{
                  minWidth: 38,
                  color: 'inherit',
                }}
              >
                {item.icon}
              </ListItemIcon>

              <ListItemText
                primary={item.title}
                slotProps={{
                  primary: {
                    sx: {
                      fontSize: 14,
                      fontWeight: isActive ? 700 : 500,
                    },
                  },
                }}
              />
            </ListItemButton>
          );
        })}
      </List>

      <Box sx={{ p: 2 }}>
        <Divider sx={{ mb: 2 }} />

        <Typography sx={{ fontSize: 11, color: 'text.secondary', textAlign: 'center' }}>
          StoreLicense System
        </Typography>

        <Typography sx={{ fontSize: 10, color: 'text.disabled', textAlign: 'center' }}>
          الإصدار 1.0.0
        </Typography>
      </Box>
    </Box>
  );

  return (
    <Drawer
      variant={isMobile ? 'temporary' : 'permanent'}
      anchor="right"
      open={isMobile ? open : true}
      onClose={onClose}
      ModalProps={{ keepMounted: true }}
      sx={{
        width: isMobile ? 0 : 270,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width: 270,
          boxSizing: 'border-box',
          borderLeft: '1px solid #E8ECF3',
          borderRight: 'none',
          overflowX: 'hidden',
        },
      }}
    >
      {drawerContent}
    </Drawer>
  );
}

import { useState } from 'react';
import { Box } from '@mui/material';
import Sidebar from '../components/Sidebar';
import Topbar from '../components/Topbar';

interface AdminLayoutProps {
  children: React.ReactNode;
  title?: string;
}

export default function AdminLayout({
  children,
  title,
}: AdminLayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const handleOpenSidebar = () => {
    setSidebarOpen(true);
  };

  const handleCloseSidebar = () => {
    setSidebarOpen(false);
  };

  return (
    <Box
      sx={{
        display: 'flex',
        minHeight: '100vh',
        bgcolor: '#F5F7FB',
      }}
    >
      <Sidebar
        open={sidebarOpen}
        onClose={handleCloseSidebar}
      />

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minWidth: 0,
          minHeight: '100vh',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <Topbar
          title={title}
          onMenuClick={handleOpenSidebar}
        />

        <Box
          sx={{
            flexGrow: 1,
            p: { xs: 2, sm: 2.5, md: 3 },
          }}
        >
          {children}
        </Box>
      </Box>
    </Box>
  );
}

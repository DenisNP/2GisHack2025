import './App.css';
import React, { useState } from 'react';
import Mapgl from './Mapgl';
import { MapglContextProvider } from './MapglContext';
import { ZoneDrawer } from './components/ZoneDrawer';
import { ZoneType } from './types/Zone';
import { PoiManager } from './components/PoiManager/PoiManager';
import { 
    Box, 
    Typography, 
    Stack,
    CssBaseline,
    IconButton,
    Tooltip,
    Paper,
    Divider,
    ThemeProvider
} from '@mui/material';
import { customTheme } from './theme/customTheme';
import {
    Block as BlockIcon,
    CheckCircle as CheckCircleIcon,
    DirectionsWalk as DirectionsWalkIcon,
    Close as CloseIcon,
    Place as PlaceIcon,
    CropFree as CropFreeIcon,
    Settings as SettingsIcon
} from '@mui/icons-material';
import { useZones } from './hooks/useZones';
import { Tools } from './components/Tools/Tools';
import { NotificationProvider } from './components/NotificationProvider';

// Компонент-обертка для управления видимостью панели
const PanelWrapper: React.FC<{ 
    panelId: string; 
    children: React.ReactNode; 
    openPanel: string | null;
}> = React.memo(({ panelId, children, openPanel }) => {
    const isVisible = openPanel === panelId;
    
    return (
        <Box 
            sx={{ 
                display: isVisible ? 'block' : 'none'
            }}
        >
            {children}
        </Box>
    );
});

function App() {
    const {
        availableZones, 
        restrictedZones, 
        urbanZones,
        baseZones,
        hasBaseZone,
        setAvailableZones, 
        setRestrictedZones, 
        setUrbanZones,
        setBaseZones,
    } = useZones();

    // Состояние для открытой панели
    const [openPanel, setOpenPanel] = useState<string | null>(null);

    const sidebarWidth = 100; // Ширина узкой боковой панели
    const drawerWidth = 320; // Ширина выезжающей панели

    const menuItems = [
        { 
            id: ZoneType.None, 
            label: 'Рабочая область', 
            icon: CropFreeIcon,
            color: '#8E8E93' 
        },
        { 
            id: ZoneType.Restricted, 
            label: 'Запрещенные зоны', 
            icon: BlockIcon,
            color: '#cc3b3b' 
        },
        { 
            id: ZoneType.Available, 
            label: 'Разрешенные зоны', 
            icon: CheckCircleIcon,
            color: '#00b450' 
        },
        { 
            id: ZoneType.Urban, 
            label: 'Общая зона', 
            icon: DirectionsWalkIcon,
            color: '#FFA500' 
        },
        { 
            id: 'poi', 
            label: 'Точки интереса (POI)',
            icon: PlaceIcon,
            color: '#007acc' 
        },
        { 
            id: 'tools', 
            label: 'Инструменты',
            icon: SettingsIcon,
            color: '#9C27B0' 
        }
    ];

    const handleMenuClick = (itemId: string) => {
        setOpenPanel(openPanel === itemId ? null : itemId);
    };

    return (
        <ThemeProvider theme={customTheme}>
            <NotificationProvider>
                <MapglContextProvider>
                    <CssBaseline />
                    <Box sx={{ display: 'flex', height: '100vh', position: 'relative' }}>
                        {/* Узкая вертикальная панель слева */}
                        <Paper 
                            elevation={3}
                            sx={{
                                width: sidebarWidth,
                                flexShrink: 0,
                                zIndex: 1300,
                                borderRadius: 0,
                                position: 'relative',
                            }}
                        >
                            <Stack spacing={1} sx={{ p: 1, pt: 2 }}>
                                {/* Логотип и название */}
                                <Box sx={{ 
                                    display: 'flex', 
                                    flexDirection: 'column', 
                                    alignItems: 'center',
                                    mb: 2,
                                    gap: 1
                                }}>
                                    <img 
                                        src="/icon.png" 
                                        alt="Pathscape Logo" 
                                        style={{ 
                                            width: '60px', 
                                            height: '60px',
                                            objectFit: 'contain'
                                        }} 
                                    />
                                    <Typography 
                                        variant="h6" 
                                        sx={{ 
                                            fontWeight: 600,
                                            fontSize: '16px',
                                            letterSpacing: '0.5px',
                                            background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                                            WebkitBackgroundClip: 'text',
                                            WebkitTextFillColor: 'transparent',
                                            backgroundClip: 'text',
                                            textAlign: 'center'
                                        }}
                                    >
                                        Pathscape
                                    </Typography>
                                </Box>
                                <Divider sx={{ mb: 1 }} />
                                {menuItems.map((item) => (
                                    <Tooltip key={item.id} title={item.label} placement="right">
                                        <IconButton
                                            onClick={() => handleMenuClick(item.id)}
                                            disabled={item.id !== ZoneType.None && item.id !== "tools" && !hasBaseZone}
                                            sx={{
                                                width: 80,
                                                height: 60,
                                                borderRadius: 2,
                                                backgroundColor: openPanel === item.id ? item.color : 'transparent',
                                                color: openPanel === item.id ? 'white' : 'text.primary',
                                                fontSize: '24px',
                                                '&:hover': {
                                                    backgroundColor: openPanel === item.id ? item.color : 'action.hover',
                                                },
                                                flexDirection: 'column',
                                                gap: 0.5
                                            }}
                                        >
                                            <item.icon />
                                            <Typography 
                                                variant="caption" 
                                                sx={{ 
                                                    fontSize: '9px', 
                                                    lineHeight: 1, 
                                                    textAlign: 'center',
                                                    maxWidth: '76px',
                                                    overflow: 'hidden',
                                                    textOverflow: 'ellipsis',
                                                    whiteSpace: 'nowrap'
                                                }}
                                            >
                                                {item.label.length > 12 ? item.label.split(' ')[0] : item.label}
                                            </Typography>
                                        </IconButton>
                                    </Tooltip>
                                ))}
                            </Stack>
                        </Paper>

                        {/* Основная область с картой */}
                        <Box 
                            component="main" 
                            sx={{ 
                                flexGrow: 1, 
                                height: '100vh',
                                position: 'relative',
                            }}
                        >
                            <Mapgl />
                        </Box>

                        {/* Выезжающая панель поверх карты */}
                        <Box
                            sx={{
                                position: 'absolute',
                                left: sidebarWidth,
                                top: 0,
                                height: '100%',
                                width: openPanel ? drawerWidth : 0,
                                transition: 'width 0.3s ease',
                                overflow: 'hidden',
                                backgroundColor: 'background.paper',
                                boxShadow: openPanel ? '2px 0 8px rgba(0, 0, 0, 0.15)' : 'none',
                                zIndex: 1200,
                            }}
                        >
                            <Box sx={{ width: drawerWidth, height: '100%', p: 2, overflow: 'auto', display: openPanel ? 'block' : 'none' }}>
                                <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
                                    <Typography variant="h6">
                                        {menuItems.find(item => item.id === openPanel)?.label}
                                    </Typography>
                                    <IconButton onClick={() => setOpenPanel(null)} size="small">
                                        <CloseIcon />
                                    </IconButton>
                                </Stack>
                                <Divider sx={{ mb: 2 }} />
                                {/* Отображается панель только активного компонента */}
                                <PanelWrapper panelId={ZoneType.None} openPanel={openPanel}>
                                    <ZoneDrawer 
                                        type={ZoneType.None} 
                                        zones={baseZones} 
                                        onZonesChanged={setBaseZones}
                                        isActiveZone={ZoneType.None === openPanel}
                                    />
                                </PanelWrapper>
                                <PanelWrapper panelId={ZoneType.Restricted} openPanel={openPanel}>
                                    <ZoneDrawer 
                                        type={ZoneType.Restricted} 
                                        zones={restrictedZones} 
                                        onZonesChanged={setRestrictedZones}
                                        isActiveZone={ZoneType.Restricted === openPanel}
                                    />
                                </PanelWrapper>
                                <PanelWrapper panelId={ZoneType.Available} openPanel={openPanel}>
                                    <ZoneDrawer 
                                        type={ZoneType.Available} 
                                        zones={availableZones} 
                                        onZonesChanged={setAvailableZones}
                                        isActiveZone={ZoneType.Available === openPanel}
                                    />
                                </PanelWrapper>
                                <PanelWrapper panelId={ZoneType.Urban} openPanel={openPanel}>
                                    <ZoneDrawer 
                                        type={ZoneType.Urban} 
                                        zones={urbanZones} 
                                        onZonesChanged={setUrbanZones}
                                        isActiveZone={ZoneType.Urban === openPanel}
                                    />
                                </PanelWrapper>
                                <PanelWrapper panelId="poi" openPanel={openPanel}>
                                    <PoiManager />
                                </PanelWrapper>
                                <PanelWrapper panelId="tools" openPanel={openPanel}>
                                    <Stack spacing={2}>
                                    <Tools />
                                    </Stack>
                                </PanelWrapper>
                            </Box>
                        </Box>
                    </Box>
                </MapglContextProvider>
            </NotificationProvider>
        </ThemeProvider>
    );
}

export default App;

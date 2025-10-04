import './App.css';
import React, { useState } from 'react';
import Mapgl from './Mapgl';
import { MapglContextProvider } from './MapglContext';
import { UrbanDrawer, SidewalkData } from './components/UrbanDrawer';
import { ZoneDrawer, ZoneData } from './components/ZoneDrawer';
import { ZoneType } from './types/Zone';
import { 
    Box, 
    Drawer, 
    Typography, 
    Stack,
    CssBaseline,
    IconButton,
    Tooltip,
    Paper,
    Divider
} from '@mui/material';
import {
    Block as BlockIcon,
    CheckCircle as CheckCircleIcon,
    DirectionsWalk as DirectionsWalkIcon,
    Close as CloseIcon,
    LocationCity as LocationCityIcon
} from '@mui/icons-material';

function App() {
    // controlled sidewalks list
    const [sidewalks, setSidewalks] = useState<SidewalkData[]>([]);
    
    // controlled zones lists for different zone types
    const [availableZones, setAvailableZones] = useState<ZoneData[]>([]);
    const [restrictedZones, setRestrictedZones] = useState<ZoneData[]>([]);

    // Состояние для открытой панели
    const [openPanel, setOpenPanel] = useState<string | null>(null);

    const sidebarWidth = 80; // Ширина узкой боковой панели
    const drawerWidth = 400; // Ширина выезжающей панели

    const menuItems = [
        { 
            id: 'restricted', 
            label: 'Запрещенные зоны', 
            icon: BlockIcon,
            color: '#cc3b3b' 
        },
        { 
            id: 'available', 
            label: 'Разрешенные зоны', 
            icon: CheckCircleIcon,
            color: '#00b450' 
        },
        { 
            id: 'sidewalks', 
            label: 'Тротуары', 
            icon: DirectionsWalkIcon,
            color: '#FFA500' 
        }
    ];

    const handleMenuClick = (itemId: string) => {
        setOpenPanel(openPanel === itemId ? null : itemId);
    };

    const renderPanelContent = () => {
        switch (openPanel) {
            case 'restricted':
                return (
                    <Stack spacing={2}>
                        <Typography variant="h6">Запрещенные зоны</Typography>
                        <ZoneDrawer 
                            type={ZoneType.Restricted} 
                            zones={restrictedZones} 
                            onZonesChanged={setRestrictedZones} 
                        />
                    </Stack>
                );
            case 'available':
                return (
                    <Stack spacing={2}>
                        <Typography variant="h6">Разрешенные зоны</Typography>
                        <ZoneDrawer 
                            type={ZoneType.Available} 
                            zones={availableZones} 
                            onZonesChanged={setAvailableZones} 
                        />
                    </Stack>
                );
            case 'sidewalks':
                return (
                    <Stack spacing={2}>
                        <Typography variant="h6">Управление тротуарами</Typography>
                        <UrbanDrawer 
                            width={2} 
                            color='#FFD700' 
                            label='Тротуары' 
                            sidewalks={sidewalks} 
                            onSidewalksChanged={setSidewalks} 
                        />
                    </Stack>
                );
            default:
                return null;
        }
    };

    return (
        <MapglContextProvider>
            <CssBaseline />
            <Box sx={{ display: 'flex', height: '100vh' }}>
                {/* Узкая вертикальная панель слева */}
                <Paper 
                    elevation={3}
                    sx={{
                        width: sidebarWidth,
                        flexShrink: 0,
                        zIndex: 1200,
                        borderRadius: 0,
                    }}
                >
                    <Stack spacing={1} sx={{ p: 1, pt: 2 }}>
                        {menuItems.map((item) => (
                            <Tooltip key={item.id} title={item.label} placement="right">
                                <IconButton
                                    onClick={() => handleMenuClick(item.id)}
                                    sx={{
                                        width: 60,
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
                                    <Typography variant="caption" sx={{ fontSize: '10px', lineHeight: 1 }}>
                                        {item.label.split(' ')[0]}
                                    </Typography>
                                </IconButton>
                            </Tooltip>
                        ))}
                    </Stack>
                </Paper>

                {/* Выезжающая панель */}
                <Drawer
                    variant="persistent"
                    anchor="left"
                    open={openPanel !== null}
                    sx={{
                        width: openPanel ? drawerWidth : 0,
                        flexShrink: 0,
                        '& .MuiDrawer-paper': {
                            width: drawerWidth,
                            boxSizing: 'border-box',
                            left: sidebarWidth,
                            borderRadius: 0,
                        },
                    }}
                >
                    <Box sx={{ p: 2, height: '100%', overflow: 'auto' }}>
                        <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
                            <Typography variant="h6">
                                {menuItems.find(item => item.id === openPanel)?.label}
                            </Typography>
                            <IconButton onClick={() => setOpenPanel(null)} size="small">
                                <CloseIcon />
                            </IconButton>
                        </Stack>
                        <Divider sx={{ mb: 2 }} />
                        {renderPanelContent()}
                    </Box>
                </Drawer>

                {/* Основная область с картой */}
                <Box 
                    component="main" 
                    sx={{ 
                        flexGrow: 1, 
                        height: '100vh',
                        position: 'relative',
                        marginLeft: openPanel ? 0 : `-${sidebarWidth}px`,
                        transition: 'margin-left 0.3s ease',
                    }}
                >
                    <Mapgl />
                    
                    {/* Все ZoneDrawer компоненты всегда смонтированы, но скрыты */}
                    <Box sx={{ display: 'none' }}>
                        <ZoneDrawer 
                            type={ZoneType.Restricted} 
                            zones={restrictedZones} 
                            onZonesChanged={setRestrictedZones} 
                        />
                        <ZoneDrawer 
                            type={ZoneType.Available} 
                            zones={availableZones} 
                            onZonesChanged={setAvailableZones} 
                        />
                        <UrbanDrawer 
                            width={2} 
                            color='#FFD700' 
                            label='Тротуары' 
                            sidewalks={sidewalks} 
                            onSidewalksChanged={setSidewalks} 
                        />
                    </Box>
                </Box>
            </Box>
        </MapglContextProvider>
    );
}

export default App;

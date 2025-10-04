import './App.css';
import React, { useState } from 'react';
import Mapgl from './Mapgl';
import { MapglContextProvider } from './MapglContext';
import { UrbanDrawer } from './components/UrbanDrawer';
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
    Button
} from '@mui/material';
import {
    Block as BlockIcon,
    CheckCircle as CheckCircleIcon,
    DirectionsWalk as DirectionsWalkIcon,
    Close as CloseIcon,
    Place as PlaceIcon
} from '@mui/icons-material';
import { useZones } from './hooks/useZones';
import { useUnit } from 'effector-react';
import { events } from './stores/globalState';
import { useSaveRestoreZones } from './hooks/useSaveRestoreZones';

function App() {
    const getJson = useUnit(events.getJson)
    const {
        availableZones, 
        restrictedZones, 
        urbanZones,
        sidewalks, 
        setAvailableZones, 
        setRestrictedZones, 
        setUrbanZones,
        setSidewalks} = useZones();
    
    const {saveZones, restoreZones} = useSaveRestoreZones()

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
            id: 'urban', 
            label: 'Общая зона', 
            icon: DirectionsWalkIcon,
            color: '#FFA500' 
        },
        { 
            id: 'sidewalks', 
            label: 'Тротуары', 
            icon: DirectionsWalkIcon,
            color: '#FFA500' 
        },
        { 
            id: 'poi', 
            label: 'Точки интереса (POI)',
            icon: PlaceIcon,
            color: '#007acc' 
        }
    ];

    const handleMenuClick = (itemId: string) => {
        setOpenPanel(openPanel === itemId ? null : itemId);
    };

    // Компонент-обертка для управления видимостью панели
    const PanelWrapper: React.FC<{ 
        panelId: string; 
        children: React.ReactNode; 
    }> = ({ panelId, children }) => {
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
    };

    return (
        <MapglContextProvider>
            <CssBaseline />
            <Box sx={{ display: 'flex', height: '100vh' }}>
                {/* Контейнер для левой части (узкая панель + выезжающая панель) */}
                <Box sx={{ display: 'flex', flexShrink: 0 }}>
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

                    {/* Выезжающая панель рядом с узкой панелью */}
                    <Box
                        sx={{
                            width: openPanel ? drawerWidth : 0,
                            transition: 'width 0.3s ease',
                            overflow: 'hidden',
                            backgroundColor: 'background.paper',
                            borderRight: openPanel ? '1px solid rgba(0, 0, 0, 0.12)' : 'none',
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
                            <PanelWrapper panelId="restricted">
                                <ZoneDrawer 
                                    type={ZoneType.Restricted} 
                                    zones={restrictedZones} 
                                    onZonesChanged={setRestrictedZones}
                                />
                            </PanelWrapper>
                            <PanelWrapper panelId="available">
                                <ZoneDrawer 
                                    type={ZoneType.Available} 
                                    zones={availableZones} 
                                    onZonesChanged={setAvailableZones}
                                />
                            </PanelWrapper>
                            <PanelWrapper panelId="urban">
                                <ZoneDrawer 
                                    type={ZoneType.Urban} 
                                    zones={urbanZones} 
                                    onZonesChanged={setUrbanZones}
                                />
                            </PanelWrapper>
                            <PanelWrapper panelId="sidewalks">
                                <UrbanDrawer 
                                    width={3} 
                                    color='#FFD700' 
                                    label='Тротуары' 
                                    sidewalks={sidewalks} 
                                    onSidewalksChanged={setSidewalks}
                                />
                            </PanelWrapper>
                            <PanelWrapper panelId="poi">
                                <PoiManager />
                            </PanelWrapper>
                        </Box>
                    </Box>
                </Box>

                {/* Основная область с картой */}
                <Box 
                    component="main" 
                    sx={{ 
                        flexGrow: 1, 
                        height: '100vh',
                        position: 'relative',
                    }}
                >
                    <Button onClick={getJson}>GET JSON</Button>
                    <Button onClick={saveZones}>SAVE ZONES</Button>
                    <Button onClick={restoreZones}>RESTORE ZONES</Button>
                    <Mapgl />
                </Box>
            </Box>
        </MapglContextProvider>
    );
}

export default App;

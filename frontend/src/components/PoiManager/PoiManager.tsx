import React, { useEffect, useRef, useState } from 'react';
import { useMapglContext } from '../../MapglContext';
import { useUnit } from 'effector-react';
import { stores, events } from './models';
import { stores as gisStores, events as gisEvents} from "./models/doubleGisStore"
import { Poi, PoiType } from '../../types/Poi';
import { 
    Groups as GroupsIcon, 
    Person as PersonIcon, 
    PersonOutline as PersonOutlineIcon,
    TouchApp as TouchAppIcon,
    DeleteSweep as DeleteSweepIcon,
    CloudDownload as CloudDownloadIcon,
    Info as InfoIcon,
    Warning as WarningIcon
} from '@mui/icons-material';
import { Typography, Button, Stack, Alert, Divider, CircularProgress } from '@mui/material';
import './PoiManager.css';

// Вспомогательная функция для получения anchor (точка привязки к карте)
// Размеры должны соответствовать CSS
const getAnchor = (type: PoiType): [number, number] => {
    switch (type) {
        case PoiType.High: return [13, 13];    // центр кружочка
        case PoiType.Medium: return [11, 11];
        case PoiType.Low: return [8, 8];
    }
};

export function PoiManager() {
    const { mapglInstance, mapgl } = useMapglContext();
    const [store, isLoadingGisData, loadGisData] = useUnit([stores.$store, gisStores.$isLoadingData, gisEvents.loadData]);
    
    // Режим добавления POI
    const [addingMode, setAddingMode] = useState<PoiType | null>(null);
    
    // Режим удаления POI
    const [isDeletionMode, setIsDeletionMode] = useState(false);
    
    // Храним ссылки на маркеры POI
    const markersRef = useRef<Array<{ id: number; marker: any }>>([]);

    // Синхронизация маркеров с данными из стора
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;
        
        // Удаляем все существующие маркеры
        markersRef.current.forEach((entry) => {
            try {
                entry.marker && entry.marker.destroy();
            } catch (e) {
                console.error('Ошибка при удалении маркера:', e);
            }
        });
        markersRef.current = [];

        // Создаем новые маркеры для каждого POI
        store.poi.forEach((poi: Poi) => {
            try {
                const markerClass = `poi-marker poi-marker-${poi.type.toLowerCase()}`;

                // Создаем HTML элемент для маркера
                const html = document.createElement('div');
                html.className = markerClass;

                // Обработчик удаления POI при клике (только в режиме удаления)
                if (isDeletionMode) {
                    const clickHandler = (e: MouseEvent) => {
                        e.stopPropagation(); // Предотвращаем всплытие события к карте
                        events.removePoiById(poi.id);
                    };
                    html.addEventListener('click', clickHandler);
                }

                // Обновляем стиль курсора в зависимости от режима
                html.style.cursor = isDeletionMode ? 'pointer' : 'default';

                // Создаем маркер на карте
                const marker = new (mapgl as any).HtmlMarker(mapglInstance, {
                    coordinates: [poi.geoPoint.lng, poi.geoPoint.lat],
                    html,
                    anchor: getAnchor(poi.type),
                    interactive: isDeletionMode,
                    zIndex: 1000,
                });

                markersRef.current.push({ id: poi.id, marker });
            } catch (e) {
                console.error('Ошибка при создании маркера POI:', e);
            }
        });
    }, [store, mapglInstance, mapgl, isDeletionMode]);

    // Обработка кликов по карте для добавления POI
    useEffect(() => {
        if (!mapglInstance || !mapgl || !addingMode) return;
        
        const onMapClick = (e: any) => {
            const lngLat: number[] = e.lngLat;
            if (!Array.isArray(lngLat) || lngLat.length < 2) return;
            
            // Добавляем новый POI
            events.addPoi({
                geoPoint: { lng: lngLat[0], lat: lngLat[1] },
                type: addingMode,
            });
        };
        
        mapglInstance.on('click', onMapClick);
        
        return () => {
            try {
                mapglInstance.off('click', onMapClick);
            } catch (e) {
                console.error('Ошибка при отключении обработчика клика:', e);
            }
        };
    }, [mapglInstance, mapgl, addingMode]);
    
    // Очистка при размонтировании компонента
    useEffect(() => {
        return () => {
            markersRef.current.forEach((entry) => {
                try {
                    entry.marker && entry.marker.destroy();
                } catch (e) {
                    console.error('Ошибка при очистке маркера:', e);
                }
            });
            markersRef.current = [];
        };
    }, []);
    
    // Обработчик клика по кнопке добавления
    const toggleAddingMode = (type: PoiType) => {
        if (addingMode === type) {
            // Отжимаем кнопку
            setAddingMode(null);
        } else {
            // Включаем режим добавления и отключаем режим удаления
            setAddingMode(type);
            setIsDeletionMode(false);
        }
    };
    
    // Обработчик клика по кнопке удаления
    const toggleDeletionMode = () => {
        setIsDeletionMode(!isDeletionMode);
        // Отключаем режим добавления при включении удаления
        if (!isDeletionMode) {
            setAddingMode(null);
        }
    };
    
    return (
        <Stack spacing={1} className="poi-manager-panel">
            <Typography variant="groupHeader">
                Добавить
            </Typography>

            <Button
                onClick={() => toggleAddingMode(PoiType.High)}
                variant={addingMode === PoiType.High ? "contained" : "outlined"}
                startIcon={<GroupsIcon />}
                className={`poi-button-high ${addingMode === PoiType.High ? 'active' : ''}`}
            >
                Популярные точки интереса
            </Button>
            <Button
                onClick={() => toggleAddingMode(PoiType.Medium)}
                variant={addingMode === PoiType.Medium ? "contained" : "outlined"}
                startIcon={<PersonIcon />}
                className={`poi-button-medium ${addingMode === PoiType.Medium ? 'active' : ''}`}
            >
                Обычные точки интереса
            </Button>
            <Button
                onClick={() => toggleAddingMode(PoiType.Low)}
                variant={addingMode === PoiType.Low ? "contained" : "outlined"}
                startIcon={<PersonOutlineIcon />}
                className={`poi-button-low ${addingMode === PoiType.Low ? 'active' : ''}`}
            >
                Второстепенные точки интереса
            </Button>

            
            <Divider />
            <Typography variant="groupHeader">
                Загрузка из API
            </Typography>
            <Button
                onClick={loadGisData}
                variant="success"
                startIcon={
                    isLoadingGisData ? (
                        <CircularProgress size={20} color="inherit" />
                    ) : (
                        <CloudDownloadIcon />
                    )
                }
                disabled={isLoadingGisData}
            >
                {isLoadingGisData ? 'Загрузка...' : 'Загрузить POI из 2GIS'}
            </Button>

            <Divider />
            <Typography variant="groupHeader">
                Удалить
            </Typography>
            <Button
                onClick={toggleDeletionMode}
                variant={isDeletionMode ? "contained" : "outlined"}
                startIcon={<TouchAppIcon />}
                color={isDeletionMode ? "error" : "neutral"}
            >
                Удаление по одной
            </Button>
            <Button
                onClick={() => events.removeAllPoi()}
                variant="outlined"
                startIcon={<DeleteSweepIcon />}
                disabled={store.poi.length === 0}
                color="error"
            >
                Удалить все POI
            </Button>
            
            {addingMode && (
                <Alert severity="info" icon={<InfoIcon />}>
                    Кликните на карту для добавления точки
                </Alert>
            )}
            {isDeletionMode && (
                <Alert severity="warning" icon={<WarningIcon />}>
                    Кликните на маркер для удаления
                </Alert>
            )}
        </Stack>
    );
}
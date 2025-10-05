import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useMapglContext } from '../../../../MapglContext';
import { GeoPoint } from "../../../../types/GeoPoint";
import { Button, Stack, Alert, Typography, Box, TextField } from "@mui/material";
import AddIcon from '@mui/icons-material/Add';
import CancelOutlinedIcon from '@mui/icons-material/CancelOutlined';
import CheckIcon from '@mui/icons-material/Check';
import InfoIcon from '@mui/icons-material/Info';
import {events as zonesEvents} from "../../../../stores/zonesStore"
import { useUnit } from 'effector-react';
import { ZoneType } from '../../../../types/Zone';
import { ZONE_TYPES_COLOR } from '../../ZoneDrawer.constants';
import { UrbanDrawerProps } from './UrbanDrawer.types';

// Утилиты для работы с цветом
function clamp(v: number, lo = 0, hi = 255) { return Math.max(lo, Math.min(hi, Math.round(v))); }
function rgbToHex(r: number, g: number, b: number) {
    return '#' + [r, g, b].map((n) => clamp(n).toString(16).padStart(2, '0')).join('');
}

const BASE_COLOR_RGB = { r: 255, g: 215, b: 0 }; // желтый цвет по умолчанию

function rgbaStringFromRgb(rgb: { r: number; g: number; b: number }, alpha = 1) {
    return `rgba(${clamp(rgb.r)},${clamp(rgb.g)},${clamp(rgb.b)},${alpha})`;
}

function darkerVariant(rgb: { r: number; g: number; b: number }, factor = 0.7) {
    return { r: Math.round(rgb.r * factor), g: Math.round(rgb.g * factor), b: Math.round(rgb.b * factor) };
}

function colorsFromRgb(rgb: { r: number; g: number; b: number }) {
    const normalFill = rgbaStringFromRgb(rgb, 0.6);
    const normalStroke = rgbToHex(rgb.r, rgb.g, rgb.b);
    const highlightRgb = darkerVariant(rgb, 0.8);
    const highlightFill = rgbaStringFromRgb(highlightRgb, 0.8);
    const highlightStroke = rgbToHex(highlightRgb.r, highlightRgb.g, highlightRgb.b);
    const tempColor = rgbaStringFromRgb(rgb, 0.8);
    return { normalFill, normalStroke, highlightFill, highlightStroke, tempColor };
}

function parseHexColor(hex: string) {
    if (!hex) return BASE_COLOR_RGB;
    const m = hex.replace('#', '');
    if (m.length === 3) {
        const r = parseInt(m[0] + m[0], 16);
        const g = parseInt(m[1] + m[1], 16);
        const b = parseInt(m[2] + m[2], 16);
        return { r, g, b };
    }
    if (m.length === 6) {
        const r = parseInt(m.slice(0, 2), 16);
        const g = parseInt(m.slice(2, 4), 16);
        const b = parseInt(m.slice(4, 6), 16);
        return { r, g, b };
    }
    return BASE_COLOR_RGB;
}

// Улучшенная функция для создания полигона тротуара с правильной шириной
function createSidewalkPolygon(line: GeoPoint[], widthMeters: number): GeoPoint[] {
    if (line.length < 2) return [];
    
    const leftSide: GeoPoint[] = [];
    const rightSide: GeoPoint[] = [];
    
    for (let i = 0; i < line.length; i++) {
        const current = line[i];
        let perpVector: { lat: number; lng: number };
        
        if (i === 0) {
            // Первая точка - используем направление к следующей точке
            const next = line[i + 1];
            perpVector = getPerpendicularVector(current, next, widthMeters);
        } else if (i === line.length - 1) {
            // Последняя точка - используем направление от предыдущей точки
            const prev = line[i - 1];
            perpVector = getPerpendicularVector(prev, current, widthMeters);
        } else {
            // Средние точки - используем биссектрису угла
            const prev = line[i - 1];
            const next = line[i + 1];
            perpVector = getBisectorVector(prev, current, next, widthMeters);
        }
        
        // Добавляем точки с обеих сторон
        leftSide.push({
            lat: current.lat + perpVector.lat,
            lng: current.lng + perpVector.lng
        });
        
        rightSide.push({
            lat: current.lat - perpVector.lat,
            lng: current.lng - perpVector.lng
        });
    }
    
    // Соединяем левую и правую стороны в замкнутый полигон
    return [...leftSide, ...rightSide.reverse()];
}

// Функция для получения перпендикулярного вектора между двумя точками
function getPerpendicularVector(point1: GeoPoint, point2: GeoPoint, widthMeters: number): { lat: number; lng: number } {
    // Направление от point1 к point2
    const direction = {
        lat: point2.lat - point1.lat,
        lng: point2.lng - point1.lng
    };
    
    // Длина направления
    const length = Math.sqrt(direction.lat * direction.lat + direction.lng * direction.lng);
    if (length === 0) return { lat: 0, lng: 0 };
    
    // Нормализованное направление
    const normalized = {
        lat: direction.lat / length,
        lng: direction.lng / length
    };
    
    // Перпендикулярный вектор (поворот на 90 градусов)
    const perpendicular = {
        lat: -normalized.lng,
        lng: normalized.lat
    };
    
    // Конвертируем ширину в градусы (приблизительно для данной широты)
    const metersToDegreesLat = widthMeters / 111000; // примерно 111 км на градус широты
    const metersToDegreesLng = widthMeters / (111000 * Math.cos(point1.lat * Math.PI / 180)); // корректировка для долготы
    
    return {
        lat: perpendicular.lat * metersToDegreesLat / 2,
        lng: perpendicular.lng * metersToDegreesLng / 2
    };
}

// Функция для получения биссектрисы угла (для равномерной ширины на поворотах)
function getBisectorVector(prev: GeoPoint, current: GeoPoint, next: GeoPoint, widthMeters: number): { lat: number; lng: number } {
    // Направления к соседним точкам
    const dir1 = {
        lat: current.lat - prev.lat,
        lng: current.lng - prev.lng
    };
    const dir2 = {
        lat: next.lat - current.lat,
        lng: next.lng - current.lng
    };
    
    // Нормализуем направления
    const len1 = Math.sqrt(dir1.lat * dir1.lat + dir1.lng * dir1.lng);
    const len2 = Math.sqrt(dir2.lat * dir2.lat + dir2.lng * dir2.lng);
    
    if (len1 === 0 || len2 === 0) {
        return getPerpendicularVector(prev, next, widthMeters);
    }
    
    const norm1 = { lat: dir1.lat / len1, lng: dir1.lng / len1 };
    const norm2 = { lat: dir2.lat / len2, lng: dir2.lng / len2 };
    
    // Биссектриса - усредненное направление
    let bisector = {
        lat: (norm1.lat + norm2.lat) / 2,
        lng: (norm1.lng + norm2.lng) / 2
    };
    
    const bisectorLen = Math.sqrt(bisector.lat * bisector.lat + bisector.lng * bisector.lng);
    
    if (bisectorLen === 0) {
        // Если точки на одной линии, используем обычный перпендикуляр
        return getPerpendicularVector(prev, next, widthMeters);
    }
    
    // Нормализуем биссектрису
    bisector = {
        lat: bisector.lat / bisectorLen,
        lng: bisector.lng / bisectorLen
    };
    
    // Перпендикуляр к биссектрисе
    const perpendicular = {
        lat: -bisector.lng,
        lng: bisector.lat
    };
    
    // Вычисляем коэффициент для сохранения ширины на поворотах
    const angle = Math.acos(Math.max(-1, Math.min(1, norm1.lat * norm2.lat + norm1.lng * norm2.lng)));
    const widthMultiplier = 1 / Math.sin(Math.PI / 2 - angle / 2);
    
    // Ограничиваем множитель, чтобы избежать слишком длинных выступов на острых углах
    const limitedMultiplier = Math.min(widthMultiplier, 3);
    
    // Конвертируем ширину в градусы
    const metersToDegreesLat = (widthMeters * limitedMultiplier) / 111000;
    const metersToDegreesLng = (widthMeters * limitedMultiplier) / (111000 * Math.cos(current.lat * Math.PI / 180));
    
    return {
        lat: perpendicular.lat * metersToDegreesLat / 2,
        lng: perpendicular.lng * metersToDegreesLng / 2
    };
}

const color = ZONE_TYPES_COLOR.get(ZoneType.Urban)!;

export const UrbanDrawer: React.FC<UrbanDrawerProps> = ({ isActiveZone, onDrawingStart, onDrawingCancel, shouldCancelDrawing }) => {
    const { mapglInstance, mapgl } = useMapglContext();
    const [isDrawing, setIsDrawing] = useState(false);
    const [currentPoints, setCurrentPoints] = useState<GeoPoint[]>([]);
    const [width, setWidth] = useState(2);

    const colorRgb = parseHexColor(color);
    const colorDerived = colorsFromRgb(colorRgb);
    
    // Ссылка на временную линию
    const tempLineRef = useRef<any | null>(null);
    // Ссылка на маркер первой точки
    const firstPointMarkerRef = useRef<any | null>(null);
    const addZone = useUnit(zonesEvents.addZone);

    const addUrbanZone = useCallback((coords: GeoPoint[]) => {
        addZone({type: ZoneType.Urban, coords});
    }, [addZone]);

    // Обработка кликов по карте для рисования линии
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;
        
        const onClick = (e: any) => {
            if (!isDrawing) return;
            
            const lngLat: number[] = e.lngLat;
            if (!Array.isArray(lngLat) || lngLat.length < 2) return;
            
            const point: GeoPoint = { lng: lngLat[0], lat: lngLat[1] };
            setCurrentPoints((prev) => [...prev, point]);
        };
        
        mapglInstance.on('click', onClick);
        return () => {
            try { mapglInstance.off('click', onClick); } catch (e) {}
        };
    }, [mapglInstance, mapgl, isDrawing]);
    
    // Отображение временной линии и маркера первой точки
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;
        
        // Удаляем предыдущие элементы
        if (tempLineRef.current) {
            try { tempLineRef.current.destroy(); } catch (e) {}
            tempLineRef.current = null;
        }
        if (firstPointMarkerRef.current) {
            try { firstPointMarkerRef.current.destroy(); } catch (e) {}
            firstPointMarkerRef.current = null;
        }
        
        if (currentPoints.length === 0) return;
        
        // Создаем маркер первой точки
        try {
            const first = currentPoints[0];
            const html = document.createElement('div');
            html.style.width = '18px';
            html.style.height = '18px';
            html.style.borderRadius = '9px';
            html.style.background = colorDerived.normalStroke;
            html.style.display = 'flex';
            html.style.alignItems = 'center';
            html.style.justifyContent = 'center';
            html.style.color = 'white';
            html.style.fontSize = '11px';
            html.style.fontWeight = '600';
            html.style.cursor = 'pointer';
            html.style.border = '2px solid white';
            html.style.boxShadow = '0 2px 4px rgba(0,0,0,0.2)';
            
            firstPointMarkerRef.current = new (mapgl as any).HtmlMarker(mapglInstance, {
                coordinates: [first.lng, first.lat],
                html,
                anchor: [9, 9],
                interactive: false,
                zIndex: 1000,
            });
        } catch (err) {
            firstPointMarkerRef.current = null;
        }
        
        // Создаем временную линию если есть минимум 2 точки
        if (currentPoints.length >= 2) {
            try {
                tempLineRef.current = new (mapgl as any).Polyline(mapglInstance, {
                    coordinates: currentPoints.map(p => [p.lng, p.lat]),
                    width: 3,
                    color: colorDerived.tempColor,
                });
            } catch (err) {
                tempLineRef.current = null;
            }
        }
    }, [currentPoints, mapglInstance, mapgl, colorDerived]);
    
    // Очистка при размонтировании
    useEffect(() => () => {
        if (tempLineRef.current) {
            try { tempLineRef.current.destroy(); } catch (e) {}
            tempLineRef.current = null;
        }
        if (firstPointMarkerRef.current) {
            try { firstPointMarkerRef.current.destroy(); } catch (e) {}
            firstPointMarkerRef.current = null;
        }
    }, []);
    
    function startDrawing() {
        setCurrentPoints([]);
        setIsDrawing(true);
        onDrawingStart?.(); // Оповещаем о начале рисования
    }
    
    const cancelDrawing = useCallback(() => {
        setIsDrawing(false);
        setCurrentPoints([]);
        
        // Очищаем временные элементы
        if (tempLineRef.current) {
            try { tempLineRef.current.destroy(); } catch (e) {}
            tempLineRef.current = null;
        }
        if (firstPointMarkerRef.current) {
            try { firstPointMarkerRef.current.destroy(); } catch (e) {}
            firstPointMarkerRef.current = null;
        }
        
        onDrawingCancel?.(); // Оповещаем об отмене рисования
    }, [onDrawingCancel]);
    
    function finishDrawing() {
        if (currentPoints.length < 2) {
            alert('Нужно минимум 2 точки для создания тротуара');
            return;
        }
        
        // Создаем полигон тротуара и добавляем как урбан-зону
        const polygon = createSidewalkPolygon(currentPoints, width);
        addUrbanZone(polygon);
        
        // Очищаем временные элементы
        if (tempLineRef.current) {
            try { tempLineRef.current.destroy(); } catch (e) {}
            tempLineRef.current = null;
        }
        if (firstPointMarkerRef.current) {
            try { firstPointMarkerRef.current.destroy(); } catch (e) {}
            firstPointMarkerRef.current = null;
        }
        
        // Завершаем рисование
        setIsDrawing(false);
        setCurrentPoints([]);
    }

    useEffect(()=>{
        if(!isActiveZone)
        {
            cancelDrawing();
        }
    }, [isActiveZone, cancelDrawing])
    
    // Эффект для принудительной отмены рисования
    useEffect(() => {
        if (shouldCancelDrawing && isDrawing) {
            cancelDrawing();
        }
    }, [shouldCancelDrawing, isDrawing, cancelDrawing]);
    
    return (
        <Stack spacing={1}>
            <Typography variant="groupHeader">
                    Тротуар
            </Typography>
            {/* Настройка ширины */}
            <Box sx={{ 
                p: 1, 
                // backgroundColor: 'action.hover', 
                borderRadius: 1,
                display: 'flex',
                alignItems: 'center',
                gap: 1,
                justifyContent: 'space-between'
            }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography variant="body2">
                        Ширина тротуара:
                    </Typography>
                </Box>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Button 
                        size="small" 
                        variant="outlined"
                        onClick={() => setWidth(Math.max(0.5, width - 0.5))}
                        disabled={width <= 0.5 || isDrawing}
                        sx={{ 
                            minWidth: 32, 
                            height: 24,
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center'
                        }}
                    >
                        −
                    </Button>
                    <TextField
                        size="small"
                        type="number"
                        value={width}
                        onChange={(e) => {
                            const value = parseFloat(e.target.value);
                            if (!isNaN(value) && value >= 0.5 && value <= 10) {
                                setWidth(value);
                            }
                        }}
                        disabled={isDrawing}
                        inputProps={{
                            min: 0.5,
                            max: 10,
                            step: 0.5,
                            style: { textAlign: 'center' }
                        }}
                        sx={{ 
                            width: 60,
                            '& .MuiInputBase-root': {
                                height: 24
                            },
                            '& input[type=number]': {
                                '-moz-appearance': 'textfield'
                            },
                            '& input[type=number]::-webkit-outer-spin-button': {
                                '-webkit-appearance': 'none',
                                margin: 0
                            },
                            '& input[type=number]::-webkit-inner-spin-button': {
                                '-webkit-appearance': 'none',
                                margin: 0
                            }
                        }}
                    />
                    <Typography variant="body2" sx={{ fontSize: '12px' }}>
                        м
                    </Typography>
                    <Button 
                        size="small" 
                        variant="outlined"
                        onClick={() => setWidth(Math.min(10, width + 0.5))}
                        disabled={width >= 10 || isDrawing}
                        sx={{ 
                            minWidth: 32, 
                            height: 24,
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center'
                        }}
                    >
                        +
                    </Button>
                </Box>
            </Box>
            
            {/* Кнопки рисования */}
            <Button 
                variant="contained" 
                startIcon={<AddIcon />} 
                onClick={startDrawing} 
                disabled={isDrawing}>
                Добавить тротуар
            </Button>
            
            {isDrawing && (
                <>
                    <Button 
                        variant="contained" 
                        startIcon={<CheckIcon />} 
                        onClick={finishDrawing} 
                        disabled={currentPoints.length < 2}
                        color="success">
                        Готово
                    </Button>
                    <Button 
                        variant="outlined" 
                        startIcon={<CancelOutlinedIcon />} 
                        onClick={cancelDrawing}>
                        Отмена
                    </Button>
                </>
            )}
            
            {/* Подсказки */}
            {isDrawing && (
                <Alert severity="info" icon={<InfoIcon />}>
                    Кликайте по карте для добавления точек тротуара. 
                    Кликните "Готово" для завершения.
                </Alert>
            )}
        </Stack>
    );
};

// Явный экспорт для отладки
export default UrbanDrawer;
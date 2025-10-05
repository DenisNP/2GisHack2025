import { useCallback, useEffect, useRef, useState } from "react";
import { useMapglContext } from "../../MapglContext";
import { ZoneDrawerProps, ZoneData, ZonePolygon } from "./ZoneDrawer.types";
import { GeoPoint } from "../../types/GeoPoint";
import { ZONE_TYPES_COLOR } from "./ZoneDrawer.constants";
import { useZoneId} from "../../hooks/useZoneId";
import { ZoneType } from "../../types/Zone";
import { Button, Divider, Stack, Alert, Typography } from "@mui/material";
import AddIcon from '@mui/icons-material/Add';
import CancelOutlinedIcon from '@mui/icons-material/CancelOutlined';
import DeleteSweepOutlinedIcon from '@mui/icons-material/DeleteSweepOutlined';
import TouchAppOutlinedIcon from '@mui/icons-material/TouchAppOutlined';
import InfoIcon from '@mui/icons-material/Info';
import WarningIcon from '@mui/icons-material/Warning';
import { UrbanDrawer } from "./components/UrbanDrawer";
import { useUnit } from "effector-react";
import { stores as zonesStores, events as zonesEvents } from "../../stores/zonesStore";

// color helpers
function clamp(v: number, lo = 0, hi = 255) { return Math.max(lo, Math.min(hi, Math.round(v))); }
function rgbToHex(r: number, g: number, b: number) {
    return '#' + [r, g, b].map((n) => clamp(n).toString(16).padStart(2, '0')).join('');
}

const BASE_COLOR_RGB = { r: 0, g: 122, b: 204 }; // base blue fallback

function rgbaStringFromRgb(rgb: { r: number; g: number; b: number }, alpha = 1) {
    return `rgba(${clamp(rgb.r)},${clamp(rgb.g)},${clamp(rgb.b)},${alpha})`;
}

function darkerVariant(rgb: { r: number; g: number; b: number }, factor = 0.5) {
    return { r: Math.round(rgb.r * factor), g: Math.round(rgb.g * factor), b: Math.round(rgb.b * factor) };
}

function colorsFromRgb(rgb: { r: number; g: number; b: number }) {
    const normalFill = rgbaStringFromRgb(rgb, 0.25);
    const normalStroke = rgbToHex(rgb.r, rgb.g, rgb.b);
    const highlightRgb = darkerVariant(rgb, 0.45);
    const highlightFill = rgbaStringFromRgb(highlightRgb, 0.5);
    const highlightStroke = rgbToHex(highlightRgb.r, highlightRgb.g, highlightRgb.b);
    const tempColor = rgbaStringFromRgb(rgb, 0.65);
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

function closeRing(coords: GeoPoint[]) {
    if (coords.length === 0) return coords;
    const first = coords[0];
    const last = coords[coords.length - 1];
    if (first.lat === last.lat && first.lng === last.lng) return coords;
    return [...coords, first];
}


export const ZoneDrawer:React.FC<ZoneDrawerProps> = ({type, zones, isActiveZone, onZonesChanged}) =>{

    const { mapglInstance, mapgl } = useMapglContext();
    // Используем состояние рисования из Effector store
    const isDrawing = useUnit(zonesStores.getDrawingState(type));
    const [isDeletionMode, setIsDeletionMode] = useState(false);
    const [isSidewalkDrawing, setIsSidewalkDrawing] = useState(false); // состояние рисования тротуаров
    const [currentPoints, setCurrentPoints] = useState<GeoPoint[]>([]);
    const polygonsRef = useRef<Array<ZonePolygon>>([]);
    const getNewZoneId = useZoneId();
    const currentPointsRef = useRef<GeoPoint[]>([]);
    const currentZonesRef = useRef<ZoneData[]>([]);

    // Колбэки для взаимодействия с UrbanDrawer
    const handleSidewalkDrawingStart = useCallback(() => {
        setIsSidewalkDrawing(true);
        // Отменяем рисование зон при начале рисования тротуаров
        if (isDrawing) {
            zonesEvents.setDrawingForZone({ zoneType: type, isDrawing: false });
            setCurrentPoints([]);
            if (tempLineRef.current) { try { tempLineRef.current.destroy(); } catch (e) {} tempLineRef.current = null; }
            if (firstPointMarkerRef.current) {
                try { if (firstPointHtmlRef.current && firstPointClickHandlerRef.current) { firstPointHtmlRef.current.removeEventListener('click', firstPointClickHandlerRef.current); } } catch (e) {}
                try { firstPointMarkerRef.current.destroy(); } catch (e) {}
                firstPointMarkerRef.current = null; firstPointHtmlRef.current = null; firstPointClickHandlerRef.current = null;
            }
        }
    }, [isDrawing, type]);    const handleSidewalkDrawingCancel = useCallback(() => {
        setIsSidewalkDrawing(false);
    }, []);

    const colorRgb = parseHexColor(ZONE_TYPES_COLOR.get(type)!);
    const colorDerived = colorsFromRgb(colorRgb);

    // Обновляем ref при изменении currentPoints
    useEffect(() => {
        currentPointsRef.current = currentPoints;
    }, [currentPoints]);

    // Обновляем ref при изменении zones
    useEffect(() => {
        currentZonesRef.current = zones;
    }, [zones]);

    const tempLineRef = useRef<any | null>(null);
    const firstPointMarkerRef = useRef<any | null>(null);
    const firstPointHtmlRef = useRef<HTMLElement | null>(null);
    const firstPointClickHandlerRef = useRef<((e: MouseEvent) => void) | null>(null);

    const finishPolygon = useCallback(() => {
            const currentPointsValue = currentPointsRef.current;
            if (currentPointsValue.length >= 3) {
                const coords = closeRing(currentPointsValue);
                const id = getNewZoneId();
                const newEntry: ZoneData = { id, coords };
                const newList = [...currentZonesRef.current, newEntry];
                onZonesChanged(newList);
            }
            
            // Очищаем текущие точки и временные элементы
            if (tempLineRef.current) { try { tempLineRef.current.destroy(); } catch (e) {} tempLineRef.current = null; }
            if (firstPointMarkerRef.current) {
                try {
                    if (firstPointHtmlRef.current && firstPointClickHandlerRef.current) {
                        firstPointHtmlRef.current.removeEventListener('click', firstPointClickHandlerRef.current);
                    }
                } catch (e) {}
                try { firstPointMarkerRef.current.destroy(); } catch (e) {}
                firstPointMarkerRef.current = null;
                firstPointHtmlRef.current = null;
                firstPointClickHandlerRef.current = null;
            }
            
            // Для типа None выключаем режим рисования после завершения полигона
            if (type === ZoneType.None) {
                zonesEvents.setDrawingForZone({ zoneType: type, isDrawing: false });
            }
            // Для других типов остаемся в режиме рисования для непрерывного рисования
            
            setCurrentPoints([]); // Очищаем точки для начала нового полигона
        }, [onZonesChanged, getNewZoneId, type]);

    const createFirstPointMarker = useCallback((firstPoint: GeoPoint) => {
            if (!mapglInstance || !mapgl) return;
            
            // Очищаем предыдущий маркер если есть
            if (firstPointMarkerRef.current) {
                try {
                    if (firstPointHtmlRef.current && firstPointClickHandlerRef.current) {
                        firstPointHtmlRef.current.removeEventListener('click', firstPointClickHandlerRef.current);
                    }
                } catch (e) {}
                try { firstPointMarkerRef.current.destroy(); } catch (e) {}
                firstPointMarkerRef.current = null;
                firstPointHtmlRef.current = null;
                firstPointClickHandlerRef.current = null;
            }
            
            try {
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
                
                // Обработчик клика по маркеру - просто завершаем полигон
                const handler = (ev: MouseEvent) => {
                    try { ev.stopPropagation(); ev.preventDefault(); } catch (e) {}
                    finishPolygon();
                };
                
                firstPointHtmlRef.current = html;
                firstPointClickHandlerRef.current = handler;
                html.addEventListener('click', handler);
                
                firstPointMarkerRef.current = new mapgl.HtmlMarker(mapglInstance, {
                    coordinates: [firstPoint.lng, firstPoint.lat],
                    html,
                    anchor: [9, 9],
                    interactive: true,
                    zIndex: 1000,
                });
            } catch (err) {
                firstPointMarkerRef.current = null;
                firstPointHtmlRef.current = null;
                firstPointClickHandlerRef.current = null;
            }
        }, [mapglInstance, mapgl, colorDerived, finishPolygon]);

    const isClickNearFirstPoint = useCallback((clickPoint: GeoPoint): boolean => {
            const currentPointsValue = currentPointsRef.current;
            if (currentPointsValue.length === 0) return false;
            
            try {
                const firstPoint = currentPointsValue[0];
                const clickPx = mapglInstance!.project([clickPoint.lng, clickPoint.lat]);
                const firstPx = mapglInstance!.project([firstPoint.lng, firstPoint.lat]);
                
                const dx = clickPx[0] - firstPx[0];
                const dy = clickPx[1] - firstPx[1];
                const distance = Math.sqrt(dx * dx + dy * dy);
                
                return distance < 12; // 12 пикселей
            } catch (err) {
                return false;
            }
        }, [mapglInstance]);

    // Добавляем новую точку в полигон
        const addPointToPolygon = useCallback((point: GeoPoint) => {
            setCurrentPoints((prev) => {
                const newPoints = [...prev, point];
                
                // Если это первая точка, создаем маркер
                if (newPoints.length === 1) {
                    createFirstPointMarker(point);
                }
                
                return newPoints;
            });
        }, [createFirstPointMarker]);

    const deletePolygonById = useCallback((id: number) => {
        // destroy instance if present
        // const pos = polygonsRef.current.findIndex((p) => p.id === id);
        // if (pos !== -1) {
        //     const pInst = polygonsRef.current[pos];
        //     try { pInst.instance && pInst.instance.destroy(); } catch (e) {}
        //     polygonsRef.current.splice(pos, 1);
        // }
        // update logical list
        const newList = currentZonesRef.current.filter((p) => p.id !== id);
        onZonesChanged(newList);
        console.log("DELETE")
    }, [onZonesChanged]);

    // keep map polygon instances in sync with zones
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;
        // destroy existing
        polygonsRef.current.forEach((p) => { try { p.instance && p.instance.destroy(); } catch (e) {} });
        polygonsRef.current = [];
        // recreate from zones
        zones.forEach((z) => {
            try {
                // Convert GeoPoint[] to [lng, lat][] format for MapGL
                const coords = z.coords.map(point => [point.lng, point.lat]);
                const id = z.id;
                const inst = new (mapgl as any).Polygon(mapglInstance, {
                    coordinates: [coords], // MapGL expects array of rings: [[[lng,lat],...]]
                    color: colorDerived.normalFill,
                    strokeColor: colorDerived.normalStroke,
                    interactive: isDeletionMode,
                });
                
                // Добавляем обработчик клика для удаления
                try {
                    inst.on && inst.on('click', (e: any) => {
                            deletePolygonById(id);
                    });
                } catch (e) {}
                
                polygonsRef.current.push({ id, instance: inst, coords: z.coords, rgb: colorRgb });
            } catch (e) {}
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [zones, mapglInstance, mapgl, type, isDeletionMode]);

    const handleMapClick = useCallback((e: any) => {
        // Проверяем, что мы в режиме рисования
        if (!isDrawing) return;
        
        // Получаем координаты клика
        const lngLat: number[] = e.lngLat;
        if (!Array.isArray(lngLat) || lngLat.length < 2) return;
        const clickPoint: GeoPoint = { lng: lngLat[0], lat: lngLat[1] };

        // Проверяем, находится ли клик рядом с первой точкой
        if (isClickNearFirstPoint(clickPoint)) {
            finishPolygon(); // Замыкаем полигон
            return;
        }

        // Добавляем новую точку
        addPointToPolygon(clickPoint);
    }, [isClickNearFirstPoint, isDrawing, finishPolygon, addPointToPolygon]);

    // Обработчик кликов по карте
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;
        
        mapglInstance.on('click', handleMapClick);
        return () => {
            try { mapglInstance.off('click', handleMapClick); } catch (e) {}
        };
    }, [mapglInstance, mapgl, handleMapClick]);

    // Эффект для отображения временной линии - оптимизированный
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;
        
        // Удаляем предыдущую линию
        if (tempLineRef.current) {
            try { tempLineRef.current.destroy(); } catch (e) {}
            tempLineRef.current = null;
        }
        
        // Создаем новую временную линию только если есть минимум 2 точки
        if (currentPoints.length >= 2) {
            try {
                const tempColor = colorDerived.tempColor;
                // Преобразуем GeoPoint в [lng, lat] формат для MapGL
                const coordinates = currentPoints.map(point => [point.lng, point.lat]);
                tempLineRef.current = new (mapgl as any).Polyline(mapglInstance, {
                    coordinates,
                    width: 3,
                    color: tempColor,
                });
            } catch (err) {
                tempLineRef.current = null;
            }
        }
    }, [currentPoints, colorDerived, mapglInstance, mapgl]);

    useEffect(() => () => {
        polygonsRef.current.forEach((p) => { try { p.instance && p.instance.destroy(); } catch (e) {} });
        polygonsRef.current = [];
        if (tempLineRef.current) { try { tempLineRef.current.destroy(); } catch (e) {} tempLineRef.current = null; }
        if (firstPointMarkerRef.current) {
            try {
                if (firstPointHtmlRef.current && firstPointClickHandlerRef.current) {
                    try { firstPointHtmlRef.current.removeEventListener('click', firstPointClickHandlerRef.current); } catch (e) {}
                }
            } catch (e) {}
            try { firstPointMarkerRef.current.destroy(); } catch (e) {}
            firstPointMarkerRef.current = null;
        }
    }, []);

    function startDrawing() { 
        setCurrentPoints([]); 
        zonesEvents.setDrawingForZone({ zoneType: type, isDrawing: true }); 
        setIsDeletionMode(false); // отключаем режим удаления при начале рисования
        setIsSidewalkDrawing(false); // отменяем рисование тротуаров при начале рисования зон
    }
    
    const cancelDrawing = useCallback(() =>{
        zonesEvents.setDrawingForZone({ zoneType: type, isDrawing: false }); setCurrentPoints([]);
        if (tempLineRef.current) { try { tempLineRef.current.destroy(); } catch (e) {} tempLineRef.current = null; }
        if (firstPointMarkerRef.current) {
            try { if (firstPointHtmlRef.current && firstPointClickHandlerRef.current) { firstPointHtmlRef.current.removeEventListener('click', firstPointClickHandlerRef.current); } } catch (e) {}
            try { firstPointMarkerRef.current.destroy(); } catch (e) {}
            firstPointMarkerRef.current = null; firstPointHtmlRef.current = null; firstPointClickHandlerRef.current = null;
        }
    }, [type, setCurrentPoints])

    function toggleDeletionMode() {
        setIsDeletionMode((prevIsDeletionMode) => {
            // отключаем режим рисования при включении удаления
            if (!prevIsDeletionMode) {
                zonesEvents.setDrawingForZone({ zoneType: type, isDrawing: false });
                setCurrentPoints([]);
            }
            return !prevIsDeletionMode
        });
    }

    // finishPolygon removed: finishing is handled by clicking the first point marker or proximity while drawing

    function clearAll() {
        // destroy instances
        polygonsRef.current.forEach((p) => { try { p.instance && p.instance.destroy(); } catch (e) {} });
        polygonsRef.current = [];
        // clear logical list
        onZonesChanged([]);
        zonesEvents.setDrawingForZone({ zoneType: type, isDrawing: false })
    }

    const hasZones = zones.length > 0;
    const isNoneType = type === ZoneType.None;
    const maxZonesReached = isNoneType && hasZones; // для None максимум 1 зона

    useEffect(()=>{
        if(isDeletionMode && (!hasZones || isNoneType)) // для None отключаем удаление
        {
            setIsDeletionMode(false);
        }
    },[isDeletionMode,hasZones,isNoneType])

    useEffect(()=>{
        if(!isActiveZone)
        {
            cancelDrawing()
            setIsDeletionMode(false);
        }
    },[isActiveZone, cancelDrawing])

    const isDrawingStarted = isDrawing && currentPoints.length > 0;
    const disableDelete = isDrawingStarted || !hasZones || isNoneType; // для None всегда отключено
    const disableAdd = !isActiveZone || isDrawing || maxZonesReached; // для None отключаем при наличии зоны

    return <Stack spacing={1}>
        <Button 
                variant="contained" 
                startIcon={<AddIcon />} 
                onClick={startDrawing} 
                disabled={disableAdd}>
            {isNoneType ? 'Добавить зону' : 'Добавить'}
        </Button>
        <Button 
                variant="outlined" 
                startIcon={<CancelOutlinedIcon />} 
                onClick={cancelDrawing} 
                disabled={!isDrawing}>
            Отменить
        </Button>
        {type === ZoneType.Urban && <><Divider /><UrbanDrawer 
            isActiveZone={isActiveZone}
            onDrawingStart={handleSidewalkDrawingStart}
            onDrawingCancel={handleSidewalkDrawingCancel}
            shouldCancelDrawing={isDrawing && !isSidewalkDrawing}
        /></>}
        {!isNoneType && (
            <>
                <Divider />
                <Typography variant="groupHeader">
                    Удаление зон
                </Typography>
                <Button 
                        variant={ isDeletionMode ? "contained" : "outlined"} 
                        startIcon={<TouchAppOutlinedIcon />} 
                        onClick={toggleDeletionMode} 
                        disabled={disableDelete}
                        color={isDeletionMode ? "error" : "neutral"}>
                    Удаление по одной
                </Button>
                <Button 
                        variant={"outlined"} 
                        startIcon={<DeleteSweepOutlinedIcon />} 
                        onClick={clearAll}
                        disabled={disableDelete}
                        color="error">
                    Удалить все зоны
                </Button>
            </>
        )}
        {isNoneType && hasZones && (
            <>
                <Divider />
                <Button 
                        variant={"outlined"} 
                        startIcon={<DeleteSweepOutlinedIcon />} 
                        onClick={clearAll}
                        color="error">
                    Удалить зону
                </Button>
            </>
        )}
        
        {/* Подписи через MUI Alert */}
        {maxZonesReached && (
            <Alert severity="warning" icon={<WarningIcon />}>
                Для данного типа можно создать только одну зону. Удалите существующую, чтобы создать новую.
            </Alert>
        )}
        {isDrawing && (
            <Alert severity="info" icon={<InfoIcon />}>
                Кликните на карту для добавления точек. Кликните на первую точку для завершения.
            </Alert>
        )}
        {isDeletionMode && (
            <Alert severity="warning" icon={<WarningIcon />}>
                Кликните на полигон для удаления
            </Alert>
        )}
    </Stack>
}
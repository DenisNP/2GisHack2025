import React, { useEffect, useRef, useState } from 'react';
import { useMapglContext } from '../../MapglContext';
import { UrbanDrawerProps, SidewalkData } from "./UrbanDrawer.types";
import { GeoPoint } from "../../types/GeoPoint";

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

// Функции для конвертации между GeoPoint и MapGL координатами
function geoPointToMapGL(point: GeoPoint): [number, number] {
    return [point.lng, point.lat];
}

function geoPointArrayToMapGL(points: GeoPoint[]): [number, number][] {
    return points.map(geoPointToMapGL);
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

export const UrbanDrawer: React.FC<UrbanDrawerProps> = ({
    width = 2,
    color = '#FFD700',
    label = 'Тротуары',
    sidewalks,
    onSidewalksChanged,
    position
}) => {
    const { mapglInstance, mapgl } = useMapglContext();
    const [isDrawing, setIsDrawing] = useState(false);
    const [currentPoints, setCurrentPoints] = useState<GeoPoint[]>([]);
    const sidewalksRef = useRef<Array<{ id: string; polygonInstance: any; lineInstance: any; data: SidewalkData }>>([]);
    const nextSidewalkIndexRef = useRef<number>(1);
    const [selectedSidewalkId, setSelectedSidewalkId] = useState<string | null>(null);
    
    // uncontrolled storage когда prop sidewalks не предоставлен
    const [localSidewalks, setLocalSidewalks] = useState<SidewalkData[]>([]);
    const effectiveSidewalks = sidewalks ?? localSidewalks;
    const colorRgb = parseHexColor(color);
    const colorDerived = colorsFromRgb(colorRgb);
    
    const tempLineRef = useRef<any | null>(null);
    const firstPointMarkerRef = useRef<any | null>(null);
    const firstPointHtmlRef = useRef<HTMLElement | null>(null);
    const firstPointClickHandlerRef = useRef<((e: MouseEvent) => void) | null>(null);
    
    // Пересоздание тротуара с подсветкой или без
    const recreateSidewalk = React.useCallback((sEntry: { id: string; polygonInstance: any; lineInstance: any; data: SidewalkData }, highlight: boolean) => {
        try { sEntry.polygonInstance && sEntry.polygonInstance.destroy(); } catch (e) {}
        try { sEntry.lineInstance && sEntry.lineInstance.destroy(); } catch (e) {}
        
        const { normalFill, normalStroke, highlightFill, highlightStroke } = colorDerived;
        const fillColor = highlight ? highlightFill : normalFill;
        const strokeColor = highlight ? highlightStroke : normalStroke;
        
        // Создаем полигон тротуара
        const newPolygonInst = new (mapgl as any).Polygon(mapglInstance, {
            coordinates: [geoPointArrayToMapGL(sEntry.data.polygon[0])],
            color: fillColor,
            strokeColor,
            interactive: true,
        });
        
        // Создаем линию центра
        const newLineInst = new (mapgl as any).Polyline(mapglInstance, {
            coordinates: geoPointArrayToMapGL(sEntry.data.centerLine),
            width: 2,
            color: strokeColor,
        });
        
        try {
            newPolygonInst.on && newPolygonInst.on('click', () => {
                setSelectedSidewalkId((prev) => (prev === sEntry.id ? null : sEntry.id));
            });
        } catch (e) {}
        
        sEntry.polygonInstance = newPolygonInst;
        sEntry.lineInstance = newLineInst;
    }, [mapglInstance, mapgl, colorDerived]);
    
    // Обработка изменения выделения
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;
        
        const entries = sidewalksRef.current;
        entries.forEach((s) => {
            if (s.id !== selectedSidewalkId) {
                try {
                    recreateSidewalk(s, false);
                } catch (e) {}
            }
        });
        
        if (selectedSidewalkId) {
            const sel = entries.find((s) => s.id === selectedSidewalkId);
            if (sel) try { recreateSidewalk(sel, true); } catch (e) {}
        }
    }, [selectedSidewalkId, recreateSidewalk, mapglInstance, mapgl]);
    
    // Синхронизация с effectiveSidewalks
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;
        
        // Удаляем существующие
        sidewalksRef.current.forEach((s) => {
            try { s.polygonInstance && s.polygonInstance.destroy(); } catch (e) {}
            try { s.lineInstance && s.lineInstance.destroy(); } catch (e) {}
        });
        sidewalksRef.current = [];
        
        // Создаем заново из effectiveSidewalks
        effectiveSidewalks.forEach((s) => {
            try {
                const id = s.id;
                const data = s;
                
                const polygonInst = new (mapgl as any).Polygon(mapglInstance, {
                    coordinates: [geoPointArrayToMapGL(s.polygon[0])],
                    color: colorDerived.normalFill,
                    strokeColor: colorDerived.normalStroke,
                    interactive: true,
                });
                
                const lineInst = new (mapgl as any).Polyline(mapglInstance, {
                    coordinates: geoPointArrayToMapGL(s.centerLine),
                    width: 2,
                    color: colorDerived.normalStroke,
                });
                
                try {
                    polygonInst.on && polygonInst.on('click', () => {
                        setSelectedSidewalkId((prev) => (prev === id ? null : id));
                    });
                } catch (e) {}
                
                sidewalksRef.current.push({ id, polygonInstance: polygonInst, lineInstance: lineInst, data });
            } catch (e) {}
        });
    }, [effectiveSidewalks, mapglInstance, mapgl, colorDerived]);
    
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
    
    // Отображение временной линии во время рисования
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;
        
        // Удаляем предыдущую временную линию и маркер
        if (tempLineRef.current) {
            try { tempLineRef.current.destroy(); } catch (e) {}
            tempLineRef.current = null;
        }
        
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
            
            const handler = (ev: MouseEvent) => {
                try { ev.stopPropagation(); ev.preventDefault(); } catch (e) {}
                if (currentPoints.length >= 2) {
                    // Прямой вызов логики завершения без зависимости от функции
                    const polygon = createSidewalkPolygon(currentPoints, width);
                    const id = String(nextSidewalkIndexRef.current++);
                    
                    const newSidewalk: SidewalkData = {
                        id,
                        centerLine: [...currentPoints],
                        polygon: [polygon],
                        width
                    };
                    
                    const newList = [...effectiveSidewalks, newSidewalk];
                    if (onSidewalksChanged) onSidewalksChanged(newList);
                    if (!sidewalks) setLocalSidewalks(newList);
                    
                    // Завершаем рисование
                    setIsDrawing(false);
                    setCurrentPoints([]);
                    
                    if (tempLineRef.current) {
                        try { tempLineRef.current.destroy(); } catch (e) {}
                        tempLineRef.current = null;
                    }
                    
                    try { html.removeEventListener('click', handler); } catch (e) {}
                    try { firstPointMarkerRef.current && firstPointMarkerRef.current.destroy(); } catch (e) {}
                    firstPointMarkerRef.current = null;
                    firstPointHtmlRef.current = null;
                    firstPointClickHandlerRef.current = null;
                }
            };
            
            firstPointHtmlRef.current = html;
            firstPointClickHandlerRef.current = handler;
            html.addEventListener('click', handler);
            
            firstPointMarkerRef.current = new (mapgl as any).HtmlMarker(mapglInstance, {
                coordinates: geoPointToMapGL(first),
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
        
        // Создаем временную линию
        if (currentPoints.length >= 2) {
            try {
                tempLineRef.current = new (mapgl as any).Polyline(mapglInstance, {
                    coordinates: geoPointArrayToMapGL(currentPoints),
                    width: 3,
                    color: colorDerived.tempColor,
                });
            } catch (err) {
                tempLineRef.current = null;
            }
        }
    }, [currentPoints, mapglInstance, mapgl, colorDerived, effectiveSidewalks, onSidewalksChanged, sidewalks, width]);
    
    // Очистка при размонтировании
    useEffect(() => () => {
        sidewalksRef.current.forEach((s) => {
            try { s.polygonInstance && s.polygonInstance.destroy(); } catch (e) {}
            try { s.lineInstance && s.lineInstance.destroy(); } catch (e) {}
        });
        sidewalksRef.current = [];
        
        if (tempLineRef.current) {
            try { tempLineRef.current.destroy(); } catch (e) {}
            tempLineRef.current = null;
        }
        
        if (firstPointMarkerRef.current) {
            try {
                if (firstPointHtmlRef.current && firstPointClickHandlerRef.current) {
                    firstPointHtmlRef.current.removeEventListener('click', firstPointClickHandlerRef.current);
                }
            } catch (e) {}
            try { firstPointMarkerRef.current.destroy(); } catch (e) {}
            firstPointMarkerRef.current = null;
        }
    }, []);
    
    function startDrawing() {
        setCurrentPoints([]);
        setIsDrawing(true);
    }
    
    function cancelDrawing() {
        setIsDrawing(false);
        setCurrentPoints([]);
        
        if (tempLineRef.current) {
            try { tempLineRef.current.destroy(); } catch (e) {}
            tempLineRef.current = null;
        }
        
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
    }
    
    function finishDrawing() {
        if (currentPoints.length < 2) {
            alert('Нужно минимум 2 точки для создания тротуара');
            return;
        }
        
        const polygon = createSidewalkPolygon(currentPoints, width);
        const id = String(nextSidewalkIndexRef.current++);
        
        const newSidewalk: SidewalkData = {
            id,
            centerLine: [...currentPoints],
            polygon: [polygon], // оборачиваем в массив для внешнего контура
            width
        };
        
        const newList = [...effectiveSidewalks, newSidewalk];
        if (onSidewalksChanged) onSidewalksChanged(newList);
        if (!sidewalks) setLocalSidewalks(newList);
        
        // Завершаем рисование
        setIsDrawing(false);
        setCurrentPoints([]);
        
        if (tempLineRef.current) {
            try { tempLineRef.current.destroy(); } catch (e) {}
            tempLineRef.current = null;
        }
        
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
    }
    
    function clearAll() {
        // Удаляем все экземпляры
        sidewalksRef.current.forEach((s) => {
            try { s.polygonInstance && s.polygonInstance.destroy(); } catch (e) {}
            try { s.lineInstance && s.lineInstance.destroy(); } catch (e) {}
        });
        sidewalksRef.current = [];
        
        // Очищаем логический список
        const newList: SidewalkData[] = [];
        if (onSidewalksChanged) onSidewalksChanged(newList);
        if (!sidewalks) setLocalSidewalks(newList);
        
        nextSidewalkIndexRef.current = 1;
        setSelectedSidewalkId(null);
    }
    
    function deleteSidewalkById(id: string) {
        // Удаляем экземпляр если есть
        const pos = sidewalksRef.current.findIndex((s) => s.id === id);
        if (pos !== -1) {
            const sInst = sidewalksRef.current[pos];
            try { sInst.polygonInstance && sInst.polygonInstance.destroy(); } catch (e) {}
            try { sInst.lineInstance && sInst.lineInstance.destroy(); } catch (e) {}
            sidewalksRef.current.splice(pos, 1);
        }
        
        // Обновляем логический список
        const newList = effectiveSidewalks.filter((s) => s.id !== id);
        if (onSidewalksChanged) onSidewalksChanged(newList);
        if (!sidewalks) setLocalSidewalks(newList);
        
        if (selectedSidewalkId === id) setSelectedSidewalkId(null);
        if (newList.length === 0) nextSidewalkIndexRef.current = 1;
    }
    
    function deleteSelected() {
        if (!selectedSidewalkId) return;
        deleteSidewalkById(selectedSidewalkId);
    }
    
    const boxStyle: React.CSSProperties = position ? {
        position: 'absolute',
        left: position.left ?? 12,
        top: position.top ?? 220,
        zIndex: 1000,
        background: 'white',
        padding: 8,
        borderRadius: 6,
        minWidth: 200
    } : {
        background: 'white',
        padding: 8,
        borderRadius: 6,
        border: '1px solid #e0e0e0',
        minWidth: 200
    };
    
    return (
        <div style={boxStyle}>
            <div style={{ marginBottom: 8, fontWeight: 600, display: 'flex', alignItems: 'center', gap: 8 }}>
                <span
                    aria-hidden
                    style={{
                        width: 12,
                        height: 12,
                        borderRadius: 2,
                        background: colorDerived.normalStroke,
                        display: 'inline-block',
                        boxShadow: '0 0 0 2px rgba(0,0,0,0.04) inset',
                    }}
                />
                <span>Панель: {label}</span>
            </div>
            
                <div style={{ display: 'flex', gap: 8, flexDirection: 'column' }}>
                <div style={{ fontSize: 12, color: '#666', marginBottom: 4 }}>
                    Ширина тротуара: {width}м
                </div>
                
                {isDrawing && (
                    <div style={{ fontSize: 11, color: '#0066cc', background: '#f0f8ff', padding: 6, borderRadius: 4, marginBottom: 4 }}>
                        📍 Кликайте по карте для добавления точек тротуара.
                        <br />💡 Кликните по первой точке (⚫) или нажмите "Готово" для завершения.
                    </div>
                )}
                
                <button onClick={startDrawing} disabled={isDrawing} title='Начать рисование тротуара'>
                    Добавить тротуар
                </button>
                
                {isDrawing && (
                    <>
                        <button onClick={finishDrawing} disabled={currentPoints.length < 2} title='Завершить рисование'>
                            Готово
                        </button>
                        <button onClick={cancelDrawing} title='Отменить рисование'>
                            Отмена
                        </button>
                    </>
                )}
                
                {!isDrawing && (
                    <button onClick={clearAll} title='Удалить все тротуары'>
                        Очистить всё
                    </button>
                )}
                
                <div style={{ marginTop: 6, fontSize: 12 }}>
                    {isDrawing ? `Точек: ${currentPoints.length}` : ''}
                </div>
                
                <div style={{ marginTop: 8, borderTop: '1px solid #eee', paddingTop: 8 }}>
                    <div style={{ fontSize: 12, marginBottom: 6, fontWeight: 600 }}>Тротуары</div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 6, maxHeight: 160, overflow: 'auto' }}>
                        {effectiveSidewalks.length === 0 ? (
                            <div style={{ fontSize: 12, color: '#666' }}>Тротуаров нет</div>
                        ) : (
                            effectiveSidewalks.map((s) => (
                                <div key={s.id} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                    <button
                                        onClick={() => setSelectedSidewalkId(s.id)}
                                        style={{
                                            padding: '2px 6px',
                                            background: selectedSidewalkId === s.id ? colorDerived.normalStroke : '#f0f0f0',
                                            color: selectedSidewalkId === s.id ? 'white' : 'black',
                                            border: 'none',
                                            borderRadius: 4,
                                            fontSize: 11
                                        }}
                                    >
                                        {selectedSidewalkId === s.id ? 'Выбран' : 'Выбрать'}
                                    </button>
                                    <div style={{ fontSize: 12, flex: 1 }}>#{s.id} ({s.width}м)</div>
                                    <button
                                        onClick={() => deleteSidewalkById(s.id)}
                                        style={{ padding: '2px 6px', fontSize: 11 }}
                                    >
                                        Удалить
                                    </button>
                                </div>
                            ))
                        )}
                    </div>
                    
                    {effectiveSidewalks.length > 0 && (
                        <div style={{ marginTop: 8 }}>
                            <button onClick={deleteSelected} disabled={!selectedSidewalkId}>
                                Удалить выбранный
                            </button>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

// Явный экспорт для отладки
export default UrbanDrawer;
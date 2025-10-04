import { useCallback, useEffect, useRef, useState } from "react";
import { useMapglContext } from "../../MapglContext";
import { ZoneDrawerProps, ZoneData, ZonePolygon } from "./ZoneDrawer.types";
import { GeoPoint } from "../../types/GeoPoint";
import { ZONE_TYPES_COLOR } from "./ZoneDrawer.constants";
import { useZoneId} from "../../hooks/useZoneId";

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


export const ZoneDrawer:React.FC<ZoneDrawerProps> = ({type, zones, onZonesChanged}) =>{

    const { mapglInstance, mapgl } = useMapglContext();
    const [isDrawing, setIsDrawing] = useState(false);
    const [currentPoints, setCurrentPoints] = useState<GeoPoint[]>([]);
    const polygonsRef = useRef<Array<ZonePolygon>>([]);
    const getNewZoneId = useZoneId();
    const [selectedPolygonId, setSelectedPolygonId] = useState<number | null>(null);
    const currentPointsRef = useRef<GeoPoint[]>([]);
    const currentZonesRef = useRef<ZoneData[]>([]);


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

    const recreatePolygon = useCallback((pEntry: ZonePolygon, highlight: boolean) => {
        if(!mapglInstance)
        {
            return;
        }
        // destroy old instance and create a new one with highlight or normal style
        try { pEntry.instance && pEntry.instance.destroy(); } catch (e) {}
        const rgb = pEntry.rgb || colorRgb || BASE_COLOR_RGB;
        const { normalFill, normalStroke, highlightFill, highlightStroke } = colorsFromRgb(rgb);
        const fillColor = highlight ? highlightFill : normalFill;
        const strokeColor = highlight ? highlightStroke : normalStroke;

        // Convert GeoPoint[] to [lng, lat][] format for MapGL
        const coords = pEntry.coords.map(point => [point.lng, point.lat]);
            
        const newInst = new (mapgl as any).Polygon(mapglInstance, {
            coordinates: [coords], // MapGL expects array of rings: [[[lng,lat],...]]
            color: fillColor,
            strokeColor,
            interactive: false, // делаем некликабельными
        });
        try { newInst.userData = newInst.userData || {}; newInst.userData._coords = coords; } catch (e) {}
        pEntry.instance = newInst;
    }, [mapglInstance, mapgl, colorRgb]);

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
            
            // Очищаем всё
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
            setIsDrawing(false);
            setCurrentPoints([]);
        }, [onZonesChanged, getNewZoneId]);

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

    // react to selection changes: unhighlight previous, highlight current
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;
        // find all entries
        const entries = polygonsRef.current;
        // unhighlight others
        entries.forEach((p) => {
            if (p.id !== selectedPolygonId) {
                try {
                    // recreate with normal style
                    recreatePolygon(p, false);
                } catch (e) {}
            }
        });
        // highlight selected
        if (selectedPolygonId) {
            const sel = entries.find((p) => p.id === selectedPolygonId);
            if (sel) try { recreatePolygon(sel, true); } catch (e) {}
        }
    }, [selectedPolygonId, recreatePolygon, mapglInstance, mapgl]);

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
                    interactive: false, // делаем некликабельными
                });
                polygonsRef.current.push({ id, instance: inst, coords: z.coords, rgb: colorRgb });
            } catch (e) {}
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [zones, mapglInstance, mapgl, type]);

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

    function startDrawing() { setCurrentPoints([]); setIsDrawing(true); }
    function cancelDrawing() {
        setIsDrawing(false); setCurrentPoints([]);
        if (tempLineRef.current) { try { tempLineRef.current.destroy(); } catch (e) {} tempLineRef.current = null; }
        if (firstPointMarkerRef.current) {
            try { if (firstPointHtmlRef.current && firstPointClickHandlerRef.current) { firstPointHtmlRef.current.removeEventListener('click', firstPointClickHandlerRef.current); } } catch (e) {}
            try { firstPointMarkerRef.current.destroy(); } catch (e) {}
            firstPointMarkerRef.current = null; firstPointHtmlRef.current = null; firstPointClickHandlerRef.current = null;
        }
    }

    // finishPolygon removed: finishing is handled by clicking the first point marker or proximity while drawing

    function clearAll() {
        // destroy instances
        polygonsRef.current.forEach((p) => { try { p.instance && p.instance.destroy(); } catch (e) {} });
        polygonsRef.current = [];
        // clear logical list
        onZonesChanged([]);
    }

    function deletePolygonById(id: number) {
        // destroy instance if present
        const pos = polygonsRef.current.findIndex((p) => p.id === id);
        if (pos !== -1) {
            const pInst = polygonsRef.current[pos];
            try { pInst.instance && pInst.instance.destroy(); } catch (e) {}
            polygonsRef.current.splice(pos, 1);
        }
        // update logical list
        const newList = zones.filter((p) => p.id !== id);
        onZonesChanged(newList);
        if (selectedPolygonId === id) setSelectedPolygonId(null);
    }

    function deleteSelected() {
        if (!selectedPolygonId) return;
        deletePolygonById(selectedPolygonId);
    }

    const boxStyle: React.CSSProperties = {
        background: 'white',
        padding: 8,
        borderRadius: 6,
        border: '1px solid #e0e0e0',
    };

    return (
        <div style={boxStyle}>
            <div style={{ marginBottom: 8, fontWeight: 600, display: 'flex', alignItems: 'center', gap: 8 }}>
                <span
                    aria-hidden
                    style={{
                        width: 12,
                        height: 12,
                        borderRadius: 12,
                        background: colorDerived.normalStroke,
                        display: 'inline-block',
                        boxShadow: '0 0 0 2px rgba(0,0,0,0.04) inset',
                    }}
                />
                <span>Панель: {type}</span>
            </div>
            <div style={{ display: 'flex', gap: 8, flexDirection: 'column' }}>
                <button onClick={startDrawing} disabled={isDrawing} title='Начать добавление'>Добавить</button>
                <button onClick={cancelDrawing} disabled={!isDrawing} title='Отменить добавление'>Отмена</button>
                <button onClick={clearAll} title='Удалить все полигоны'>Очистить всё</button>
                <div style={{ marginTop: 6, fontSize: 12 }}>{isDrawing ? `Точек: ${currentPoints.length}` : ''}</div>
                <div style={{ marginTop: 8, borderTop: '1px solid #eee', paddingTop: 8 }}>
                    <div style={{ fontSize: 12, marginBottom: 6, fontWeight: 600 }}>Полигоны</div>
                            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, maxHeight: 160, overflow: 'auto' }}>
                        {polygonsRef.current.length === 0 ? (
                            <div style={{ fontSize: 12, color: '#666' }}>Полигонов нет</div>
                        ) : (
                            zones.map((p) => {
                                const pRgb = (polygonsRef.current.find(pp => pp.id === p.id) || { rgb: colorRgb }).rgb || colorRgb;
                                const pColor = colorsFromRgb(pRgb).normalStroke;
                                return (
                                <div key={p.id} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                            <button onClick={() => setSelectedPolygonId(p.id)} style={{ padding: '2px 6px', background: selectedPolygonId === p.id ? pColor : '#f0f0f0', color: selectedPolygonId === p.id ? 'white' : 'black', border: 'none', borderRadius: 4 }}>{selectedPolygonId === p.id ? 'Выбран' : 'Выбрать'}</button>
                                    <div style={{ fontSize: 12, flex: 1 }}>#{p.id}</div>
                                    <button onClick={() => deletePolygonById(p.id)} style={{ padding: '2px 6px' }}>Удалить</button>
                                </div>
                                );
                            })
                        )}
                    </div>
                    <div style={{ marginTop: 8, display: 'flex', gap: 8 }}>
                        <button onClick={deleteSelected} disabled={!selectedPolygonId}>Удалить выбранный</button>
                    </div>
                </div>
            </div>
        </div>
    );
}
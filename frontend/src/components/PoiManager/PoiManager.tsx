import React, { useEffect, useRef, useState } from 'react';
import { useMapglContext } from '../../MapglContext';
import { useUnit } from 'effector-react';
import { stores, events } from './models';
import { Poi, PoiType } from '../../types/Poi';
import './PoiManager.css';

// Цвета для разных типов POI
const POI_COLORS = {
    [PoiType.High]: '#FF0000',    // красный
    [PoiType.Medium]: '#FFA500',  // оранжевый
    [PoiType.Low]: '#FFFF00',     // желтый
};

// Размеры маркеров в зависимости от типа (для anchor)
const POI_SIZES = {
    [PoiType.High]: 20,
    [PoiType.Medium]: 16,
    [PoiType.Low]: 12,
};

// CSS классы для разных типов маркеров
const POI_MARKER_CLASSES = {
    [PoiType.High]: 'poi-marker poi-marker-high',
    [PoiType.Medium]: 'poi-marker poi-marker-medium',
    [PoiType.Low]: 'poi-marker poi-marker-low',
};

export function PoiManager() {
    const { mapglInstance, mapgl } = useMapglContext();
    const store = useUnit(stores.$store);
    
    // Режим добавления POI
    const [addingMode, setAddingMode] = useState<PoiType | null>(null);
    
    // Храним ссылки на маркеры POI
    const markersRef = useRef<Array<{ id: number; marker: any }>>([]);
    console.log(store);

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
                const size = POI_SIZES[poi.type] || POI_SIZES[PoiType.Low];
                const markerClass = POI_MARKER_CLASSES[poi.type] || POI_MARKER_CLASSES[PoiType.Low];
                
                // Создаем HTML элемент для маркера
                const html = document.createElement('div');
                html.className = markerClass;
                
                // Обработчик удаления POI при клике
                html.addEventListener('click', (e) => {
                    e.stopPropagation(); // Предотвращаем всплытие события к карте
                    events.removePoiById(poi.id);
                });
                
                // Создаем маркер на карте
                const marker = new (mapgl as any).HtmlMarker(mapglInstance, {
                    coordinates: [poi.geoPoint.lng, poi.geoPoint.lat],
                    html,
                    anchor: [size / 2, size / 2],
                    interactive: true,
                    zIndex: 1000,
                });
                
                markersRef.current.push({ id: poi.id, marker });
            } catch (e) {
                console.error('Ошибка при создании маркера POI:', e);
            }
        });
    }, [store.poi, mapglInstance, mapgl]);

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
            
            // Отключаем режим добавления после клика
            setAddingMode(null);
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
    
    // Обработчик клика по кнопке
    const toggleAddingMode = (type: PoiType) => {
        if (addingMode === type) {
            // Отжимаем кнопку
            setAddingMode(null);
        } else {
            // Включаем режим добавления
            setAddingMode(type);
        }
    };
    
    const boxStyle: React.CSSProperties = {
        position: 'absolute',
        right: 12,
        top: 12,
        zIndex: 1000,
        background: 'white',
        padding: 8,
        borderRadius: 6,
        display: 'flex',
        flexDirection: 'column',
        gap: 8,
    };
    
    const buttonStyle = (type: PoiType): React.CSSProperties => ({
        padding: '8px 16px',
        border: '2px solid',
        borderColor: addingMode === type ? POI_COLORS[type] : '#ccc',
        borderRadius: 4,
        background: addingMode === type ? POI_COLORS[type] : 'white',
        color: addingMode === type ? 'white' : '#333',
        cursor: 'pointer',
        fontWeight: addingMode === type ? 'bold' : 'normal',
    });
    
    return (
        <div style={boxStyle}>
            <div style={{ fontWeight: 600, marginBottom: 4 }}>Добавить POI</div>
            <button
                onClick={() => toggleAddingMode(PoiType.High)}
                style={buttonStyle(PoiType.High)}
            >
                Высокий приоритет
            </button>
            <button
                onClick={() => toggleAddingMode(PoiType.Medium)}
                style={buttonStyle(PoiType.Medium)}
            >
                Средний приоритет
            </button>
            <button
                onClick={() => toggleAddingMode(PoiType.Low)}
                style={buttonStyle(PoiType.Low)}
            >
                Низкий приоритет
            </button>
            {addingMode && (
                <div style={{ fontSize: 12, color: '#666', marginTop: 4 }}>
                    💡 Кликните на карту для добавления точки
                </div>
            )}
        </div>
    );
}
import React, { useEffect, useRef, useState } from 'react';
import { useMapglContext } from '../../MapglContext';
import { useUnit } from 'effector-react';
import { stores, events } from './models';
import { Poi, PoiType } from '../../types/Poi';
import { setMap } from '../../stores/mapStore';
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

interface PoiManagerProps {
    showPanel?: boolean;
}

export function PoiManager({ showPanel = false }: PoiManagerProps) {
    const { mapglInstance, mapgl } = useMapglContext();
    const store = useUnit(stores.$store);
    
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
                const markerClass = `poi-marker poi-marker-${poi.type}`;
                
                // Создаем HTML элемент для маркера
                const html = document.createElement('div');
                html.className = markerClass;
                
                // Обработчик удаления POI при клике (только в режиме удаления)
                const clickHandler = (e: MouseEvent) => {
                    e.stopPropagation(); // Предотвращаем всплытие события к карте
                    if (isDeletionMode) {
                        events.removePoiById(poi.id);
                    }
                };
                html.addEventListener('click', clickHandler);
                
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
    }, [store.poi, mapglInstance, mapgl, isDeletionMode]);

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
    
    // Если панель не нужно показывать, возвращаем null (маркеры всё равно отрисуются через useEffect)
    if (!showPanel) {
        return null;
    }
    
    return (
        <div className="poi-manager-panel">
            <div className="poi-manager-title">Управление POI</div>
            
            <div style={{ marginBottom: 8, fontWeight: 600, fontSize: 12 }}>Добавить:</div>
            <button
                onClick={() => toggleAddingMode(PoiType.High)}
                className={`poi-button poi-button-high ${addingMode === PoiType.High ? 'active' : ''}`}
            >
                Высокий приоритет
            </button>
            <button
                onClick={() => toggleAddingMode(PoiType.Medium)}
                className={`poi-button poi-button-medium ${addingMode === PoiType.Medium ? 'active' : ''}`}
            >
                Средний приоритет
            </button>
            <button
                onClick={() => toggleAddingMode(PoiType.Low)}
                className={`poi-button poi-button-low ${addingMode === PoiType.Low ? 'active' : ''}`}
            >
                Низкий приоритет
            </button>
            
            <div style={{ marginTop: 12, marginBottom: 8, fontWeight: 600, fontSize: 12 }}>Удалить:</div>
            <button
                onClick={toggleDeletionMode}
                className={`poi-button poi-button-delete ${isDeletionMode ? 'active' : ''}`}
            >
                {isDeletionMode ? '✓ Режим удаления' : 'Режим удаления'}
            </button>
            
            {addingMode && (
                <div className="poi-hint">
                    💡 Кликните на карту для добавления точки
                </div>
            )}
            {isDeletionMode && (
                <div className="poi-hint" style={{ color: '#d32f2f' }}>
                    🗑️ Кликните на маркер для удаления
                </div>
            )}
        </div>
    );
}
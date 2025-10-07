import { Map as MapGl } from '@2gis/mapgl/types';
import { GeoPoint } from '../types/GeoPoint';
import { stores as zonesStores } from '../stores/zonesStore';

/**
 * Центрирует карту по первой базовой зоне
 * @param map - экземпляр карты MapGL
 */
export const centerMapOnBaseZone = (map: MapGl): void => {
    try {
        // Получаем базовые зоны
        const baseZones = zonesStores.$baseZones.getState();

        if (baseZones.length === 0) {
            console.warn('Нет базовых зон для центрирования карты');
            return;
        }

        // Берем первую базовую зону
        const baseZone = baseZones[0];
        
        if (!baseZone.coords || baseZone.coords.length === 0) {
            console.warn('Базовая зона не содержит координат');
            return;
        }

        // Вычисляем границы зоны для вписывания в экран
        const bounds = calculateZoneBounds(baseZone.coords);
        
        if (bounds) {
            // Вписываем всю зону в экран с плавной анимацией
            map.fitBounds({
                southWest: bounds.southWest,
                northEast: bounds.northEast
            }, {
                animation: {
                    duration: 1500 // 1.5 секунды плавной анимации
                },
                padding: { 
                    top: 60, 
                    right: 60, 
                    bottom: 60, 
                    left: 60 
                } // Красивые отступы от краев экрана
            });
        }
    } catch (error) {
        console.error('Ошибка при центрировании карты по базовой зоне:', error);
    }
};

/**
 * Вычисляет границы зоны для fitBounds
 */
const calculateZoneBounds = (coords: GeoPoint[]): { southWest: [number, number]; northEast: [number, number] } | null => {
    if (coords.length === 0) return null;

    // Находим крайние точки
    let minLat = Infinity, maxLat = -Infinity;
    let minLng = Infinity, maxLng = -Infinity;

    coords.forEach(point => {
        if (point.lat < minLat) minLat = point.lat;
        if (point.lat > maxLat) maxLat = point.lat;
        if (point.lng < minLng) minLng = point.lng;
        if (point.lng > maxLng) maxLng = point.lng;
    });

    // Возвращаем в формате для fitBounds
    return {
        southWest: [minLng, minLat],
        northEast: [maxLng, maxLat]
    };
};
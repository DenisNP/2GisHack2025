import { GeoPoint } from '../types/GeoPoint';
import { Point } from '../types/Point';
import { MAIN_ZONE_TYPE } from '../types/Zone';
import { stores as zonesStores } from '../stores/zonesStore';
import { stores as mapStores } from '../stores/mapStore';
import { unprojectPoint } from './pointsProjection';

/**
 * Получить ограничивающий прямоугольник для главной зоны
 * @returns Объект с двумя GeoPoint: topLeft (левая верхняя) и bottomRight (правая нижняя)
 */
export const getBoundingBox = (): { topLeft: GeoPoint; bottomRight: GeoPoint } | null => {
    // Получаем все зоны и карту
    const allZones = zonesStores.$allZones.getState();
    const mapStore = mapStores.$mapStore.getState();

    // Проверяем, что карта инициализирована
    if (!mapStore.map) {
        console.error('Карта не инициализирована');
        return null;
    }

    // Находим главную зону (она одна)
    const mainZone = allZones.find(zone => zone.type === MAIN_ZONE_TYPE);

    if (!mainZone) {
        console.warn('Нет главной зоны');
        return null;
    }

    // Получаем все точки главной зоны
    const allPoints: Point[] = mainZone.region;

    if (allPoints.length === 0) {
        console.warn('Нет точек в главной зоне');
        return null;
    }

    // Находим крайние координаты в экранных координатах (метрах)
    let minX = Infinity;
    let maxX = -Infinity;
    let minY = Infinity;
    let maxY = -Infinity;

    allPoints.forEach(point => {
        if (point.x < minX) minX = point.x;
        if (point.x > maxX) maxX = point.x;
        if (point.y < minY) minY = point.y;
        if (point.y > maxY) maxY = point.y;
    });

    // Создаем точки: левая верхняя и правая нижняя
    const topLeft: Point = { x: minX, y: minY };
    const bottomRight: Point = { x: maxX, y: maxY };

    // Конвертируем в GeoPoint через unprojectPoint (теперь с правильным учётом origin)
    const topLeftGeo = unprojectPoint(topLeft, mapStore.origin, mapStore.map!);
    const bottomRightGeo = unprojectPoint(bottomRight, mapStore.origin, mapStore.map!);

    return {
        topLeft: topLeftGeo,
        bottomRight: bottomRightGeo
    };
};

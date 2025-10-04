import { GeoPoint } from '../types/GeoPoint';
import { Point } from '../types/Point';
import { ZoneType } from '../types/Zone';
import { stores as globalStateStores } from '../stores/globalState';
import { stores as mapStores } from '../stores/mapStore';
import { unprojectPoint } from './pointsProjection';

/**
 * Получить ограничивающий прямоугольник для всех Urban зон
 * @returns Объект с двумя GeoPoint: topLeft (левая верхняя) и bottomRight (правая нижняя)
 */
export const getBoundingBox = (): { topLeft: GeoPoint; bottomRight: GeoPoint } | null => {
    // Получаем глобальное состояние и карту
    const globalState = globalStateStores.$globalState.getState();
    const mapStore = mapStores.$mapStore.getState();

    // Проверяем, что карта инициализирована
    if (!mapStore.map) {
        console.error('Карта не инициализирована');
        return null;
    }

    // Фильтруем только Urban зоны
    const urbanZones = globalState.zones.filter(zone => zone.type === ZoneType.Urban);

    if (urbanZones.length === 0) {
        console.warn('Нет Urban зон');
        return null;
    }

    // Собираем все точки из всех Urban зон
    const allPoints: Point[] = urbanZones.flatMap(zone => zone.region);

    if (allPoints.length === 0) {
        console.warn('Нет точек в Urban зонах');
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

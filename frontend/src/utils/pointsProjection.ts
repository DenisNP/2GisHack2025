import { GeoPoint } from '../types/GeoPoint';
import { Map as MapGl } from '@2gis/mapgl/types';
import { geoDistance } from './getDistance';
import { Point } from '../types/Point';

/**
 * Превратить точку на карте в точку в виртуальном пространстве в метрах для обработки бэком
 * @param point точка на карте
 * @param origin начало координат на карте, от которого всё считается
 * @param map экземпляр карты в текущем масштабе
 */
export const projectPoint = (point: GeoPoint, origin: GeoPoint, map: MapGl): Point => {
    // Получаем позиции точек на экране в пикселях
    const geomPoint = map.project([point.lng, point.lat]);
    const geomOrigin = map.project([origin.lng, origin.lat]);
    // Коэффициент преобразования в метры
    const ratio = getPixelsToMetersRatio(map);
    // Найдём расстояния по горизонтали и вертикали от начала координат в пикселях
    const xDist = geomPoint[0] - geomOrigin[0];
    const yDist = geomPoint[1] - geomOrigin[1];

    // Превращаем в метры и возвращаем
    return { x: xDist / ratio.x, y: yDist / ratio.y };
}

/**
 * Превратить точку виртуального пространства, с которым работает бэк, в точку на карте
 * @param point точка виртуального пространства
 * @param origin начало координат на карте, от которого всё считается
 * @param map экземпляр карты в текущем масштабе
 */
export const unprojectPoint = (point: Point, origin: GeoPoint, map: MapGl): GeoPoint => {
    // Коэффициент преобразования в метры
    const ratio = getPixelsToMetersRatio(map);
    // Превращаем метры в пиксели относительно origin
    const xPixels = point.x * ratio.x;
    const yPixels = point.y * ratio.y;
    
    // Получаем позицию origin в пикселях
    const geomOrigin = map.project([origin.lng, origin.lat]);
    
    // Добавляем смещение к origin
    const absoluteX = geomOrigin[0] + xPixels;
    const absoluteY = geomOrigin[1] + yPixels;
    
    // Спроецируем на карту
    const projected = map.unproject([absoluteX, absoluteY]);
    return { lng: projected[0], lat: projected[1] };
}

/**
 * Получить количество логических пикселей в метре реального пространства при текущем масштабировании
 * Отдельно по горизонтали и вертикали, поэтому возвращается Point
 * @param map экземпляр карты
 */
const getPixelsToMetersRatio = (map: MapGl): Point => {
    // Искусственно спроецируем точки со смещением в 100 пикселей на карту
    const rawTopLeft = map.unproject([100, 100]);
    const rawTopRight = map.unproject([200, 100]);
    const rawBottomLeft = map.unproject([100, 200]);

    const geoTopLeft = { lng: rawTopLeft[0], lat: rawTopLeft[1] };
    const geoTopRight = { lng: rawTopRight[0], lat: rawTopRight[1] };
    const geoBottomLeft = { lng: rawBottomLeft[0], lat: rawBottomLeft[1] };

    // Найдём горизонтальное расстояние и вертикальное
    const horizontalDist = geoDistance(geoTopLeft, geoTopRight);
    const verticalDist = geoDistance(geoTopLeft, geoBottomLeft);

    // Вернём соотношения
    return { x: 100 / horizontalDist, y: 100 / verticalDist };
}
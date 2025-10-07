import { Poi } from "../types/Poi";
import { Point } from "../types/Point";
import { Zone, ZoneType } from "../types/Zone";

/**
 * Проверяет, находится ли точка внутри полигона (алгоритм ray casting)
 */
function pointInPolygon(point: Point, polygon: Point[]): boolean {
    let inside = false;
    const x = point.x;
    const y = point.y;
    
    for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
        const xi = polygon[i].x;
        const yi = polygon[i].y;
        const xj = polygon[j].x;
        const yj = polygon[j].y;
        
        if (((yi > y) !== (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi)) {
            inside = !inside;
        }
    }
    
    return inside;
}

/**
 * Проверяет, находится ли точка внутри любого из полигонов
 */
function pointInAnyPolygon(point: Point, polygons: Point[][]): boolean {
    return polygons.some(polygon => pointInPolygon(point, polygon));
}

/**
 * Пытается сместить точку по кругу, чтобы она вышла из полигонов
 */
function tryMovePointOutOfPolygons(
    point: Point, 
    polygons: Point[][], 
    stepMeters: number = 0.5,
    maxRadius: number = 3
): Point | null {
    // Если точка уже не в полигоне, возвращаем её
    if (!pointInAnyPolygon(point, polygons)) {
        return point;
    }
    
    // Пробуем смещать точку по кругу с увеличивающимся радиусом
    for (let radius = stepMeters; radius <= maxRadius; radius += stepMeters) {
        // Проверяем 8 направлений (каждые 15 градусов)
        for (let angle = 0; angle < 360; angle += 15) {
            const radians = (angle * Math.PI) / 180;
            const newPoint: Point = {
                x: point.x + radius * Math.cos(radians),
                y: point.y + radius * Math.sin(radians)
            };
            
            // Если новая точка не в полигоне, возвращаем её
            if (!pointInAnyPolygon(newPoint, polygons)) {
                return newPoint;
            }
        }
    }
    
    // Если не удалось найти свободное место, возвращаем null
    return null;
}

/**
 * Обрабатывает массив точек, смещая те, что находятся в полигонах
 */
function processPointsAgainstPolygons(
    points: Poi[], 
    polygons: Point[][], 
    stepMeters: number,
    maxRadius: number
): Poi[] {
    return points.reduce<Poi[]>((result, poi) => {
        const adjustedPoint = tryMovePointOutOfPolygons(poi.point, polygons, stepMeters, maxRadius);

        // Если удалось найти свободное место, добавляем точку
        if (adjustedPoint !== null) {
            result.push({
                ...poi,
                point: adjustedPoint
            });
        }
        // Если не удалось - пропускаем точку (она слишком глубоко в полигоне)
        
        return result;
    }, []);
}


/**
 * Функция для получения POI с учетом смещения от полигонов
 * Вызывается только когда нужно, не реактивная
 */
export function getAdjustedPoi(poi: Poi[], allZones: Zone[]): Poi[] {
    const restrictedZones = allZones.filter(z => z.type === ZoneType.Restricted);
    
    // Извлекаем полигоны из зон (массив Point[][])
    const polygons = restrictedZones.map(zone => zone.region);
    
    // Обрабатываем все POI, смещая те, что находятся в полигонах
    const adjustedPoi = processPointsAgainstPolygons(
        poi, 
        polygons, 
        0.5,  // шаг в метрах
        3  // максимальный радиус поиска
    );
    
    return adjustedPoi;
}
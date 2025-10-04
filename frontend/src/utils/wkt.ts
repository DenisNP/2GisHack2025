import { GeoPoint } from '../types/GeoPoint';

/**
 * Конвертирует WKT POINT в объект GeoPoint
 * @param wktPoint - строка в формате "POINT (lng lat)"
 * @returns объект GeoPoint с координатами lng и lat
 * @example parseWktPoint("POINT (37.6173 55.7558)") // { lng: 37.6173, lat: 55.7558 }
 */
export function parseWktPoint(wktPoint: string): GeoPoint {
    // Удаляем пробелы и приводим к верхнему регистру для унификации
    const normalized = wktPoint.trim();
    
    // Проверяем, что строка начинается с POINT
    if (!normalized.toUpperCase().startsWith('POINT')) {
        throw new Error('Неверный формат WKT POINT');
    }
    
    // Извлекаем координаты из скобок
    const match = normalized.match(/POINT\s*\(\s*([-\d.]+)\s+([-\d.]+)\s*\)/i);
    
    if (!match) {
        throw new Error('Не удалось распарсить координаты из WKT POINT');
    }
    
    const lng = parseFloat(match[1]);
    const lat = parseFloat(match[2]);
    
    if (isNaN(lng) || isNaN(lat)) {
        throw new Error('Неверные координаты в WKT POINT');
    }
    
    return { lng, lat };
}

/**
 * Конвертирует WKT POLYGON в массив объектов GeoPoint
 * @param wktPolygon - строка в формате "POLYGON ((lng1 lat1, lng2 lat2, ...))"
 * @returns массив объектов GeoPoint с координатами вершин полигона
 * @example parseWktPolygon("POLYGON ((37.6 55.7, 37.7 55.8, 37.8 55.7, 37.6 55.7))")
 */
export function parseWktPolygon(wktPolygon: string): GeoPoint[] {
    // Удаляем пробелы и приводим к верхнему регистру для унификации
    const normalized = wktPolygon.trim();
    
    // Проверяем, что строка начинается с POLYGON
    if (!normalized.toUpperCase().startsWith('POLYGON')) {
        throw new Error('Неверный формат WKT POLYGON');
    }
    
    // Извлекаем координаты из двойных скобок
    const match = normalized.match(/POLYGON\s*\(\s*\((.*?)\)\s*\)/i);
    
    if (!match) {
        throw new Error('Не удалось распарсить координаты из WKT POLYGON');
    }
    
    // Разбиваем координаты по запятым
    const coordinatePairs = match[1].split(',').map(pair => pair.trim());
    
    const points: GeoPoint[] = [];
    
    for (const pair of coordinatePairs) {
        // Очищаем от скобок и парсим каждую пару координат
        const cleanPair = pair.replace(/[()]/g, '').trim();
        
        if (!cleanPair) {
            continue;
        }
        
        const coords = cleanPair.split(/\s+/);
        
        if (coords.length !== 2) {
            console.warn(`Пропускаем неверную пару координат: ${cleanPair}`);
            continue;
        }
        
        const lng = parseFloat(coords[0]);
        const lat = parseFloat(coords[1]);
        
        if (isNaN(lng) || isNaN(lat)) {
            console.warn(`Пропускаем неверные координаты: ${cleanPair}`);
            continue;
        }
        
        points.push({ lng, lat });
    }
    
    if (points.length < 3) {
        throw new Error('Полигон должен содержать минимум 3 точки');
    }
    
    return points;
}

/**
 * Конвертирует WKT MULTIPOLYGON в массив массивов объектов GeoPoint (каждый полигон отдельно)
 * @param wktMultiPolygon - строка в формате "MULTIPOLYGON(((lng1 lat1, ...)), ((lng1 lat1, ...)))"
 * @returns массив массивов объектов GeoPoint, где каждый внутренний массив - это отдельный полигон
 * @example parseWktMultiPolygon("MULTIPOLYGON(((37.6 55.7, 37.7 55.8, 37.6 55.7)), ((37.8 55.9, 37.9 56.0, 37.8 55.9)))")
 */
export function parseWktMultiPolygon(wktMultiPolygon: string): GeoPoint[][] {
    // Удаляем пробелы и приводим к верхнему регистру для унификации
    const normalized = wktMultiPolygon.trim();
    
    // Проверяем, что строка начинается с MULTIPOLYGON
    if (!normalized.toUpperCase().startsWith('MULTIPOLYGON')) {
        throw new Error('Неверный формат WKT MULTIPOLYGON');
    }
    
    // Извлекаем всё содержимое после MULTIPOLYGON
    const match = normalized.match(/MULTIPOLYGON\s*\((.*)\)\s*$/is);
    
    if (!match) {
        throw new Error('Не удалось распарсить MULTIPOLYGON');
    }
    
    const content = match[1];
    const polygons: GeoPoint[][] = [];
    
    // Ищем все полигоны вида ((lng lat, lng lat, ...))
    const polygonRegex = /\(\((.*?)\)\)/g;
    let polygonMatch;
    
    while ((polygonMatch = polygonRegex.exec(content)) !== null) {
        const coordinatesStr = polygonMatch[1];
        const polygonPoints: GeoPoint[] = [];
        
        // Разбиваем координаты по запятым
        const coordinatePairs = coordinatesStr.split(',').map(pair => pair.trim());
        
        for (const pair of coordinatePairs) {
            // Очищаем от скобок и парсим каждую пару координат
            const cleanPair = pair.replace(/[()]/g, '').trim();
            
            if (!cleanPair) {
                continue;
            }
            
            const coords = cleanPair.split(/\s+/);
            
            if (coords.length !== 2) {
                console.warn(`Пропускаем неверную пару координат в MULTIPOLYGON: ${cleanPair}`);
                continue;
            }
            
            const lng = parseFloat(coords[0]);
            const lat = parseFloat(coords[1]);
            
            if (isNaN(lng) || isNaN(lat)) {
                console.warn(`Пропускаем неверные координаты в MULTIPOLYGON: ${cleanPair}`);
                continue;
            }
            
            polygonPoints.push({ lng, lat });
        }
        
        if (polygonPoints.length >= 3) {
            polygons.push(polygonPoints);
        } else if (polygonPoints.length > 0) {
            console.warn(`Пропускаем полигон с недостаточным количеством точек: ${polygonPoints.length}`);
        }
    }
    
    if (polygons.length === 0) {
        throw new Error('Не удалось извлечь полигоны из MULTIPOLYGON');
    }
    
    return polygons;
}


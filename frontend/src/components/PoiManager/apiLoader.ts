import { fetchJsonFromApi } from '../../utils/api';
import { ApiItem } from '../../types/ApiResponse';
import { parseWktPolygon, parseWktPoint, parseWktMultiPolygon } from '../../utils/wkt';
import { GeoPoint } from '../../types/GeoPoint';
import { typeByRubric } from './PoiManager.constants';
import { events } from './models';
import { PoiType } from '../../types/Poi';
import { geoPointInMainRegion } from '../../utils/pointInRegion';
import { events as zoneEvents } from '../../stores/zonesStore';
import { ZoneType } from '../../types/Zone';

/**
 * Проверяет, попадает ли item в главную зону по его координатам point
 */
function isItemInMainRegion(item: ApiItem): boolean {
    const geoPoint: GeoPoint = {
        lng: item.point.lon,
        lat: item.point.lat
    };
    
    return geoPointInMainRegion(geoPoint);
}

/**
 * Обработчик для типа branch
 */
async function processBranch(item: ApiItem): Promise<void> {
    // Получаем parent_id из рубрик
    const parentId = item.rubrics?.find(r => r.parent_id)?.parent_id;
    
    // Определяем тип POI (по умолчанию Low)
    const poiType: PoiType = parentId ? typeByRubric(parentId) : PoiType.Low;
    
    // Получаем координаты из point (API возвращает lon, lat)
    const geoPoint: GeoPoint = {
        lng: item.point.lon,
        lat: item.point.lat
    };
    
    // Добавляем POI через store
    events.addPoi({
        geoPoint,
        type: poiType
    });
}

/**
 * Обработчик для типа building
 */
async function processBuilding(item: ApiItem): Promise<void> {
    // Проверяем наличие geometry.hover
    const hoverWkt = item.geometry?.hover;
    
    if (!hoverWkt || hoverWkt.trim() === '') {
        return;
    }
    
    try {
        // Преобразуем WKT в массив GeoPoint (поддерживаем POLYGON и MULTIPOLYGON)
        if (hoverWkt.toUpperCase().startsWith('MULTIPOLYGON')) {
            const polygons = parseWktMultiPolygon(hoverWkt);
            
            // Добавляем каждый полигон как Restricted зону
            polygons.forEach((polygon) => {
                zoneEvents.addZone({
                    type: ZoneType.Restricted,
                    coords: polygon
                });
            });
        } else if (hoverWkt.toUpperCase().startsWith('POLYGON')) {
            const geoPoints = parseWktPolygon(hoverWkt);
            
            // Добавляем полигон как Restricted зону
            zoneEvents.addZone({
                type: ZoneType.Restricted,
                coords: geoPoints
            });
        } else {
            console.warn(`Неизвестный формат geometry.hover: ${hoverWkt.substring(0, 50)}...`);
            return;
        }
    } catch (error) {
        console.error('Ошибка при парсинге geometry.hover для building:', error);
    }
    
    // Обрабатываем входы, если они есть
    const entrances = item.links?.database_entrances;
    if (entrances && entrances.length > 0) {
        for (const entrance of entrances) {
            const points = entrance.geometry?.points;
            if (points && points.length > 0) {
                try {
                    // Берём первый элемент массива points и парсим его из WKT
                    const entranceGeoPoint = parseWktPoint(points[0]);
                    
                    // Проверяем, попадает ли вход в главную зону
                    if (!geoPointInMainRegion(entranceGeoPoint)) {
                        continue; // Пропускаем входы вне главной зоны
                    }
                    
                    // Добавляем POI с типом Low
                    events.addPoi({
                        geoPoint: entranceGeoPoint,
                        type: PoiType.Low
                    });
                } catch (error) {
                    console.error('Ошибка при парсинге входа building:', error);
                }
            }
        }
    }
}

/**
 * Обработчик для типов station_platform, station.metro, station_entrance
 */
async function processStation(item: ApiItem): Promise<void> {
    // Получаем координаты из point
    const geoPoint: GeoPoint = {
        lng: item.point.lon,
        lat: item.point.lat
    };
    
    // Добавляем POI с типом High
    events.addPoi({
        geoPoint,
        type: PoiType.High
    });
}

/**
 * Обрабатывает один элемент в зависимости от его типа
 */
async function processItem(item: ApiItem): Promise<void> {
    // Проверяем, попадает ли item в главную зону
    if (!isItemInMainRegion(item)) {
        return; // Пропускаем items вне главной зоны
    }
    
    switch (item.type) {
        case 'branch':
            await processBranch(item);
            break;
        case 'building':
            await processBuilding(item);
            break;
        case 'station_platform':
        case 'station.metro':
        case 'station_entrance':
            await processStation(item);
            break;
        default:
            console.warn(`Неизвестный тип элемента: ${item.type}`);
    }
}

/**
 * Запускает цепочку загрузок из API постранично
 * Начинает с первой страницы и продолжает пока не получит null
 */
export async function loadAllPages(): Promise<void> {
    console.log('Начало загрузки POI из 2GIS API...');
    let currentPage = 1;
    let totalItems = 0;
    
    while (true) {
        // Получаем данные со страницы
        const response = await fetchJsonFromApi(currentPage);
        
        // Если получили null, останавливаем цикл
        if (!response) {
            break;
        }
        
        // Обрабатываем все элементы на странице
        const items = response.result?.items || [];
        totalItems += items.length;
        
        for (const item of items) {
            await processItem(item);
        }
        
        // Переходим к следующей странице
        currentPage++;
    }
    
    console.log(`Загрузка завершена. Обработано страниц: ${currentPage - 1}, элементов: ${totalItems}`);
}


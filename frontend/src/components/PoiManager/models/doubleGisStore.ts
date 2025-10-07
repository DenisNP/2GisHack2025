import { createEffect, createEvent, sample } from "effector";
import { ApiItem } from "../../../types/ApiResponse";
import { PoiType } from "../../../types/Poi";
import { GeoPoint } from "../../../types/GeoPoint";
import { events as poiEvents } from './';
import { events as zoneEvents } from '../../../stores/zonesStore';
import { geoDistance } from "../../../utils/getDistance";
import { geoPointInMainRegion } from "../../../utils/pointInRegion";
import { typeByRubric } from "../PoiManager.constants";
import { parseWktMultiPolygon, parseWktPoint, parseWktPolygon } from "../../../utils/wkt";
import { ZoneType } from "../../../types/Zone";
import { fetchJsonFromApi } from "../../../utils/api";
import { showNotification } from "../../../utils/showNotification";

// Константы для API
const API_TYPE = ['branch', 'building', 'station.metro,station_entrance,station_platform'];


const MAX_SIZE_ZONE_IN_METERS = 4

/**
 * Проверяет, является ли полигон маленьким (вписывается в прямоугольник с заданным размером в метрах)
 */
function isSmallPolygon(polygon: GeoPoint[], maxSizeMeters: number): boolean {
    if (polygon.length === 0) return true;
    
    // Находим bounding box полигона
    let minLng = polygon[0].lng;
    let maxLng = polygon[0].lng;
    let minLat = polygon[0].lat;
    let maxLat = polygon[0].lat;
    
    for (const point of polygon) {
        minLng = Math.min(minLng, point.lng);
        maxLng = Math.max(maxLng, point.lng);
        minLat = Math.min(minLat, point.lat);
        maxLat = Math.max(maxLat, point.lat);
    }
    
    // Вычисляем размеры bounding box в метрах
    const topLeft: GeoPoint = { lng: minLng, lat: maxLat };
    const topRight: GeoPoint = { lng: maxLng, lat: maxLat };
    const bottomLeft: GeoPoint = { lng: minLng, lat: minLat };
    
    const widthMeters = geoDistance(topLeft, topRight);
    const heightMeters = geoDistance(topLeft, bottomLeft);
    
    // Проверяем, что оба размера не превышают maxSizeMeters
    return widthMeters <= maxSizeMeters && heightMeters <= maxSizeMeters;
}

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
    poiEvents.addPoi({
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
            
            // Добавляем каждый полигон как Restricted зону, если он не маленький
            polygons.forEach((polygon) => {
                if (!isSmallPolygon(polygon, MAX_SIZE_ZONE_IN_METERS)) {
                    zoneEvents.addZone({
                        type: ZoneType.Restricted,
                        coords: polygon
                    });
                } else {
                    console.log(`Пропускаем маленький полигон (меньше ${MAX_SIZE_ZONE_IN_METERS}x${MAX_SIZE_ZONE_IN_METERS} метра)`);
                }
            });
        } else if (hoverWkt.toUpperCase().startsWith('POLYGON')) {
            const geoPoints = parseWktPolygon(hoverWkt);
            
            // Добавляем полигон как Restricted зону, если он не маленький
            if (!isSmallPolygon(geoPoints, MAX_SIZE_ZONE_IN_METERS)) {
                zoneEvents.addZone({
                    type: ZoneType.Restricted,
                    coords: geoPoints
                });
            } else {
                console.log(`Пропускаем маленький полигон (меньше ${MAX_SIZE_ZONE_IN_METERS}x${MAX_SIZE_ZONE_IN_METERS} метра)`);
            }
        } else {
            console.warn(`Неизвестный формат geometry.hover: ${hoverWkt.substring(0, 50)}...`);
            return;
        }
    } catch (error) {
        console.error('Ошибка при парсинге geometry.hover для building:', error);
    }
    
    // Обрабатываем входы, если они есть
    const entrances = item.links?.database_entrances || item.links?.entrances;
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
                    poiEvents.addPoi({
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
    poiEvents.addPoi({
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

const loadDataInternal = async () => {
    console.log('Начало загрузки POI из 2GIS API...');
    let totalItems = 0;
    let totalPages = 0;
    
    // Внешний цикл по типам объектов
    for (const type of API_TYPE) {
        console.log(`Начинаем загрузку данных для типа: ${type}`);
        let currentPage = 1;
        let typeItems = 0;
        let typePages = 0;
        
        // Внутренний цикл по страницам для текущего типа
        while (true) {
            try {
                // Получаем данные со страницы для текущего типа
                const response = await fetchJsonFromApi(type, currentPage);
                
                // Если получили null, останавливаем цикл для этого типа
                if (!response) {
                    break;
                }
                
                // Обрабатываем все элементы на странице
                const items = response.result?.items || [];
                typeItems += items.length;
                typePages++;
                
                for (const item of items) {
                    await processItem(item);
                }
                
                // Проверяем, есть ли еще страницы для этого типа
                const totalItemsForType = response.result?.total || 0;
                if (typeItems >= totalItemsForType || items.length === 0) {
                    break;
                }
                
                // Переходим к следующей странице
                currentPage++;
                
            } catch (error) {
                console.error(`Ошибка при загрузке страницы ${currentPage} для типа ${type}:`, error);
                break;
            }
        }
        
        console.log(`Завершена загрузка для типа ${type}: страниц ${typePages}, элементов ${typeItems}`);
        totalItems += typeItems;
        totalPages += typePages;
        
        // Небольшая пауза между запросами разных типов для снижения нагрузки на API
        if (type !== API_TYPE[API_TYPE.length - 1]) {
            await new Promise(resolve => setTimeout(resolve, 100));
        }
    }
    
    console.log(`Загрузка завершена. Всего обработано страниц: ${totalPages}, элементов: ${totalItems}`);
}

const loadData = createEvent();

const loadDataFx = createEffect(async() => {
    let isSuccess = true
    try
    {
        await loadDataInternal()
    }
    catch
    {
        isSuccess = false
    }

    return isSuccess;
})

loadDataFx.doneData.watch((isSuccess) => {
    if(!isSuccess)
    {
        showNotification({
            variant: "error",
            message: "Не удалось получить данные из справочника 2ГИС"
        })
        return;
    }

    showNotification({
            variant: "success",
            message: "Данные из справочника 2ГИC успешно получены "
        })
})

sample({
    clock: loadData,
    target: loadDataFx
})

export const events = {
    loadData
}

export const stores = {
    $isLoadingData: loadDataFx.pending
}
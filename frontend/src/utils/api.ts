import { getBoundingBox } from './getBoundingBox';
import { ApiResponse, ApiItem } from '../types/ApiResponse';

const API_KEY = process.env.REACT_APP_MAPGL_API_KEY || '';
const API_ENDPOINT = 'https://catalog.api.2gis.com/3.0/items';
const API_LOCALE = 'ru_RU';
const API_FIELDS = 'items.point,items.caption,items.links,items.links.database_entrances,items.geometry.hover,items.rubrics';
const API_TYPE = ['branch', 'building', 'station.metro,station_entrance,station_platform'];
const API_PAGE_SIZE = Number(process.env.REACT_APP_2GIS_PAGE_SIZE) || 10;

/**
 * Выполняет GET запрос к API 2GIS каталога с фиксированными параметрами для одного типа
 * @param type - Тип объекта для запроса
 * @param page - Опциональный номер страницы для пагинации
 * @returns Promise с ответом от API
 */
export async function fetchFromApi(type: string, page?: number): Promise<Response> {
    // Получаем ограничивающий прямоугольник из Urban зон
    const boundingBox = getBoundingBox();
    
    // Если ограничивающий прямоугольник не получен, останавливаем запрос
    if (!boundingBox) {
        throw new Error('Не удалось получить ограничивающий прямоугольник. Запрос к API остановлен.');
    }

    // Добавляем все параметры запроса
    const queryParams: Record<string, string | number> = {
        key: API_KEY,
        locale: API_LOCALE,
        fields: API_FIELDS,
        type: type,
        page_size: API_PAGE_SIZE,
        point1: `${boundingBox.topLeft.lng},${boundingBox.topLeft.lat}`,
        point2: `${boundingBox.bottomRight.lng},${boundingBox.bottomRight.lat}`,
    };

    // Добавляем page только если он передан
    if (page !== undefined) {
        queryParams.page = page;
    }

    // Создаем строку параметров запроса
    const searchParams = new URLSearchParams();
    Object.entries(queryParams).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
            searchParams.append(key, String(value));
        }
    });

    // Формируем полный URL
    const url = `${API_ENDPOINT}?${searchParams.toString()}`;

    // Выполняем запрос
    const response = await fetch(url);

    if (!response.ok) {
        throw new Error(`API request failed: ${response.status} ${response.statusText}`);
    }

    return response;
}

/**
 * Выполняет GET запрос к API 2GIS каталога и возвращает JSON ответ для одного типа
 * @param type - Тип объекта для запроса
 * @param page - Опциональный номер страницы для пагинации
 * @returns Promise с JSON ответом или null, если ответ не распарсился или items пуст
 */
export async function fetchJsonFromApi(type: string, page?: number): Promise<ApiResponse | null> {
    try {
        const response = await fetchFromApi(type, page);
        const data: ApiResponse = await response.json();

        // Проверяем, что items существует и не пустой
        if (!data.result?.items || data.result.items.length === 0) {
            return null;
        }

        return data;
    } catch (error) {
        console.log(`Ошибка при парсинге ответа API: ${error}`);
        throw new Error('Ошибка при парсинге ответа API');
    }
}


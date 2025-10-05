import { useEffect, useRef } from "react";
import { useMapglContext } from "../../../../../../MapglContext";
import { useUnit } from "effector-react";
import { stores, ResultEdgeGeo } from "../../../../../../stores/simulationState";

// Функция для получения цвета линии по весу
// Цвета подобраны для хорошего контраста с цветами зон
const getLineColor = (weight: number) => {
    if (weight < 0.3) return '#2196F3'; // синий для слабых связей - контрастен со всеми зонами
    if (weight < 0.7) return '#9C27B0'; // фиолетовый для средних связей - хорошо видно на всех фонах
    return '#E91E63'; // розово-красный для сильных связей - выделяется на всех зонах
};

// Функция для получения толщины линии по весу
const getLineWidth = (weight: number) => {
    return Math.max(2, Math.min(10, weight * 10)); // от 2 до 10 пикселей
};

export const SimulationLines: React.FC = () => {
    const { mapglInstance, mapgl } = useMapglContext();
    const simulationResult = useUnit(stores.$simulationResult);
    const polylinesRef = useRef<any[]>([]);

    

    // Создание полилиний на карте
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;

        // Очищаем существующие линии
        polylinesRef.current.forEach((polyline) => {
            try {
                polyline.destroy();
            } catch (e) {
                console.warn('Ошибка при удалении полилинии:', e);
            }
        });
        polylinesRef.current = [];

        // Если нет результатов симуляции, выходим
        if (!simulationResult || simulationResult.length === 0) {
            return;
        }

        // Создаем полилинии для каждой связи
        simulationResult.forEach((edge: ResultEdgeGeo, index: number) => {
            try {
                const polyline = new (mapgl as any).Polyline(mapglInstance, {
                    coordinates: [[edge.from.lng, edge.from.lat], [edge.to.lng, edge.to.lat]],
                    color: getLineColor(edge.weight),
                    width: getLineWidth(edge.weight),
                    strokeColor: '#FFFFFF', // белая обводка для лучшего контраста
                    strokeWidth: 1, // тонкая обводка
                    interactive: false,
                    zIndex: 500, // ниже маркеров POI, но выше полигонов
                });

                polylinesRef.current.push(polyline);
            } catch (e) {
                console.error(`Ошибка при создании полилинии ${index}:`, e);
            }
        });

        // Очистка при размонтировании компонента
        return () => {
            polylinesRef.current.forEach((polyline) => {
                try {
                    polyline.destroy();
                } catch (e) {
                    console.warn('Ошибка при очистке полилинии:', e);
                }
            });
            polylinesRef.current = [];
        };
    }, [mapglInstance, mapgl, simulationResult]);

    // Компонент не рендерит никаких DOM элементов
    return null;
};
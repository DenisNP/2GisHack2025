import { useEffect, useRef } from "react";
import { useMapglContext } from "../../../../../../MapglContext";
import { useUnit } from "effector-react";
import { stores, ResultGeoPoint } from "../../../../../../stores/simulationState";

// Стиль для коричневых точек симуляции
const POINT_COLOR = '#8B4513';

export const SimulationLines: React.FC = () => {
    const { mapglInstance, mapgl } = useMapglContext();
    const simulationResult = useUnit(stores.$simulationResult);
    const markersRef = useRef<any[]>([]);

    

    // Создание точек на карте
    useEffect(() => {
        if (!mapglInstance || !mapgl) return;

        // Очищаем существующие маркеры
        markersRef.current.forEach((marker) => {
            try {
                marker.destroy();
            } catch (e) {
                console.warn('Ошибка при удалении маркера:', e);
            }
        });
        markersRef.current = [];

        // Если нет результатов симуляции, выходим
        if (!simulationResult || simulationResult.length === 0) {
            return;
        }

        // Создаем маленькие коричневые точки для каждого результата
        simulationResult.forEach((point: ResultGeoPoint, index: number) => {
            try {
                const html = document.createElement('div');
                html.style.width = '6px';
                html.style.height = '6px';
                html.style.backgroundColor = POINT_COLOR; // коричневый цвет
                html.style.borderRadius = '50%';
                html.style.border = '1px solid rgba(255, 255, 255, 0.8)';
                html.style.boxShadow = '0 1px 2px rgba(0, 0, 0, 0.3)';

                const marker = new (mapgl as any).HtmlMarker(mapglInstance, {
                    coordinates: [point.point.lng, point.point.lat],
                    html,
                    interactive: false,
                    zIndex: 1000,
                });

                markersRef.current.push(marker);
            } catch (e) {
                console.error(`Ошибка при создании маркера ${index}:`, e);
            }
        });

        // Очистка при размонтировании компонента
        return () => {
            markersRef.current.forEach((marker) => {
                try {
                    marker.destroy();
                } catch (e) {
                    console.warn('Ошибка при очистке маркера:', e);
                }
            });
            markersRef.current = [];
        };
    }, [mapglInstance, mapgl, simulationResult]);

    // Компонент не рендерит никаких DOM элементов
    return null;
};
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

        const createMarkers = (points: ResultGeoPoint[]) => {
            // Настраиваем батч-обработку
            const batchSize = 100;
            let processedCount = 0;

            const processBatch = () => {
                const endIndex = Math.min(processedCount + batchSize, points.length);
                
                for (let i = processedCount; i < endIndex; i++) {
                    const point = points[i];

                    const size = 6;
                    const div = document.createElement('div');
                    div.style.cssText = `
                        width: ${size}px;
                        height: ${size}px;
                        background: ${POINT_COLOR};
                        border: 1px solid rgba(255, 255, 255, 0.8);
                        border-radius: 50%;
                        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
                        position: absolute;
                        transform: translate(-50%, -50%);
                        pointer-events: none;
                        will-change: transform;
                    `;

                    try {
                        const marker = new (mapgl as any).HtmlMarker(mapglInstance, {
                            coordinates: [point.point.lng, point.point.lat],
                            html: div,
                            interactive: false,
                            zIndex: 1000,
                        });

                        markersRef.current.push(marker);
                    } catch (e) {
                        console.warn('Ошибка создания маркера:', e);
                    }
                }

                processedCount = endIndex;

                if (processedCount < points.length) {
                    // Продолжаем обработку в следующем кадре
                    requestAnimationFrame(processBatch);
                }
            };

            // Запускаем обработку
            processBatch();
        };

        // Очищаем существующие маркеры
        markersRef.current.forEach((marker) => {
            try {
                marker.destroy();
            } catch (e) {
                console.warn('Ошибка при удалении маркера:', e);
            }
        });
        markersRef.current = [];

        createMarkers(simulationResult);

        // Очистка при размонтировании
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
import { Poi, PoiType } from '../../types/Poi';

export const EMPTY_POI: Poi = {
    id: 0,
    point: { x: 0, y: 0 },
    weight: 0,
    type: PoiType.Low,
    geoPoint: { lng: 0, lat: 0 },
};

export const weightByType = (type: PoiType): number => {
    switch (type) {
        case PoiType.Low:
            return 0.2;
        case PoiType.Medium:
            return 0.5;
        case PoiType.High:
            return 1;
    }
};
import { Poi, PoiType } from '../../types/Poi';
import { GeoPoint } from '../../types/GeoPoint';
import { MapStore } from '../../stores/mapStore';

export interface PoiManagerStore {
    poi: Poi[];
}

export interface AddPoiEventData {
    geoPoint: GeoPoint;
    type: PoiType;
}

export interface MovePoiEventData {
    geoPoint: GeoPoint;
    id: number;
}

export type EffectProps<T, S> = { data: T, store: S, mapStore: MapStore };
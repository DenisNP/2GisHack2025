import { Poi } from '../../types/Poi';
import { GeoPoint } from '../../types/GeoPoint';
import { MapStore } from '../../stores/mapStore';

export interface PoiManagerStore {
    poi: Poi[];
}

export interface AddPoiData {
    geoPoint: GeoPoint;
    weight: number;
}

export type EffectProps<T, S> = { data: T, store: S, mapStore: MapStore };
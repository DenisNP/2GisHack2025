import { Point } from './Point';
import { GeoPoint } from './GeoPoint';

export interface Poi {
    id: number;
    point: Point;
    geoPoint: GeoPoint;
    weight: number;
    type: PoiType;
}

export enum PoiType {
    High = 'High',
    Medium = 'Medium',
    Low = 'Low',
}
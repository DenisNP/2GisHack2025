import { Point } from './Point';

export interface Zone {
    id: number;
    region: Point[];
    type: ZoneType;
}

export enum ZoneType {
    Restricted = "Restricted",
    Urban = "Urban",
    Available = "Available",
}
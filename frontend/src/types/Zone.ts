import { Point } from './Point';

export interface Zone {
    id: number;
    region: Point[];
    type: ZoneType;
}

export enum ZoneType {
    None = "None",
    Restricted = "Restricted",
    Urban = "Urban",
    Available = "Available",
}

export const MAIN_ZONE_TYPE = ZoneType.None;
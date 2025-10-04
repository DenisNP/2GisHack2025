import { GeoPoint } from "../../types/GeoPoint";
import { ZoneType } from "../../types/Zone"

export type ZoneDrawerProps = {
    type: ZoneType,
    zones: ZoneData[];
    onZonesChanged: (polygons: ZoneData[]) => void;
}

export type ZoneData = { 
    id: number; 
    coords: GeoPoint[] 
};

export type ZonePolygon = {
    id: number;
    instance: any; 
    coords: GeoPoint[]; 
    rgb?: { r:number; g:number; b:number }
}
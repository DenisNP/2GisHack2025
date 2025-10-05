import { Point } from '../types/Point';
import { MAIN_ZONE_TYPE, Zone, ZoneType } from '../types/Zone';
import { GeoPoint } from '../types/GeoPoint';
import { stores } from '../stores/mapStore';
import { stores as zonesStores } from '../stores/zonesStore';
import { projectPoint } from './pointsProjection';

export const pointInRegion = (point: Point, region: Point[]): boolean => {
    // ray-casting algorithm based on
    // https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html
    
    const x = point.x;
    const y = point.y;
    
    let inside = false;
    for (let i = 0, j = region.length - 1; i < region.length; j = i++) {
        const xi = region[i].x;
        const yi = region[i].y;
        const xj = region[j].x;
        const yj = region[j].y;
        
        const intersect = ((yi > y) !== (yj > y))
            && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
        if (intersect) inside = !inside;
    }
    
    return inside;
}

export const geoPointInRegion = (point: GeoPoint, zone: Zone): boolean => {
    const { map, origin } = stores.$mapStore.getState();
    
    if (!map) {
        throw new Error('Map is not initialized');
    }
    
    const projectedPoint = projectPoint(point, origin, map);
    return pointInRegion(projectedPoint, zone.region);
}

export const geoPointInMainRegion = (point: GeoPoint): boolean => {
    const allZones = zonesStores.$allZones.getState();
    const urbanZone = allZones.find(zone => zone.type === MAIN_ZONE_TYPE);
    
    if (!urbanZone) {
        return false;
    }
    
    return geoPointInRegion(point, urbanZone);
}
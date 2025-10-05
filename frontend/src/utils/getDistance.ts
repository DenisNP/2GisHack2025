import { GeoPoint } from '../types/GeoPoint';
import { Point } from '../types/Point';

export const geoDistance = (p1: GeoPoint, p2: GeoPoint): number => {
    const R = 6378137; // Earth’s mean radius in meter
    const dLat = rad(p2.lat - p1.lat);
    const dLong = rad(p2.lng - p1.lng);
    const a =
        Math.sin(dLat / 2) * Math.sin(dLat / 2) +
        Math.cos(rad(p1.lat)) *
            Math.cos(rad(p2.lat)) *
            Math.sin(dLong / 2) *
            Math.sin(dLong / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

    return R * c;
};

export const distance = (p1: Point, p2: Point): number => {
    const dx = p1.x - p2.x;
    const dy = p1.y - p2.y;
    return Math.sqrt(dx * dx + dy * dy);
};

const rad = function(x: number): number {
    return x * Math.PI / 180;
};
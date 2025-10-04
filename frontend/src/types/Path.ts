import { Poi } from './Poi';
import { Point } from './Point';

export interface Path {
    start: Poi;
    end: Poi;
    points: Point[];
}
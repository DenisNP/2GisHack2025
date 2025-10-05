import { Poi } from "./Poi";
import { Point } from "./Point";
import { Zone } from "./Zone";

export interface RunSimulationRequest {
    zones: Zone[],
    poi: Poi[]
}


export interface ResultEdgeResponse {
    from: Point,
    to: Point,
    weight: number
}
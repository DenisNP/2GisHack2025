import { createEffect, createEvent, createStore, sample } from "effector";
import { RunSimulationRequest } from "../types/InternalApi";
import { runSimulationApi } from "../utils/internalApi";
import { stores as globalStores } from "./globalState"
import { stores as mapStores } from "./mapStore"
import { events as zonesEvents } from "./zonesStore"
import { getAdjustedPoi } from "../utils/getAdjustedPoi";
import { Map as MapGl } from '@2gis/mapgl/types';
import { GeoPoint } from "../types/GeoPoint";
import { unprojectPoint } from "../utils/pointsProjection";
import { showNotification } from "../utils/showNotification";

type RunSimulationProps = {
    request: RunSimulationRequest,
    mapInfo: {
        map: MapGl,
        origin: GeoPoint
    }
}

export type ResultEdgeGeo = {
    from: GeoPoint,
    to: GeoPoint,
    weight: number
}

const runSimulation = createEvent();

const runSimulationFx = createEffect(async ({request, mapInfo: {origin, map}}: RunSimulationProps) => {
    let isSuccess = true;
    let response: ResultEdgeGeo[] = [];
    try
    {
        var adjustedPoi = getAdjustedPoi(request.poi, request.zones);
        var apiResponse = await runSimulationApi({...request, poi: adjustedPoi});

        response = apiResponse.map(x=>({
            from: unprojectPoint(x.from, origin, map),
            to: unprojectPoint(x.to, origin, map),
            weight: x.weight
        }))
    }
    catch
    {
        isSuccess = false
    }

    return {isSuccess, response};
});

const $simulationResult = createStore<ResultEdgeGeo[]>([])
.on(runSimulationFx.doneData, (state, result)=> {
    return result.isSuccess ? result.response : state
})
.reset([runSimulation, zonesEvents.clearAllZones])

const $canSimulate = globalStores.$globalState.map(x=>x.zones.length > 0 && x.poi.length > 0)

sample({
    clock: runSimulation,
    source: {globalState: globalStores.$globalState, mapStore: mapStores.$mapStore},
    filter: ({mapStore}) => mapStore.map !== undefined,
    fn: ({globalState, mapStore}):RunSimulationProps => ({
        request: globalState, 
        mapInfo: {
            map: mapStore.map!, 
            origin: mapStore.origin}
        }),
    target: runSimulationFx
})

runSimulationFx.doneData.watch(({isSuccess}) => {
    if(!isSuccess)
    {
        showNotification({
            variant: "error",
            message: "Не удалось выполнить симуляцию маршрутов"
        })
        return;
    }

    showNotification({
            variant: "success",
            message: "Симуляция маршрутов успешно выполнена"
        })
})

export const events = {
    runSimulation
}

export const stores = {
    $simulationResult,
    $canSimulate,
    $isSimulating: runSimulationFx.pending
}
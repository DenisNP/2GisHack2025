import { createEffect, createEvent, createStore, sample } from "effector";
import { ResultEdgeResponse, RunSimulationRequest } from "../types/InternalApi";
import { runSimulationApi } from "../utils/internalApi";
import { stores as globalStores } from "./globalState"
import { getAdjustedPoi } from "../utils/getAdjustedPoi";

const runSimulation = createEvent();

const runSimulationFx = createEffect(async (request: RunSimulationRequest) => {
    let isSuccess = true;
    let response: ResultEdgeResponse[] = [];
    try
    {
        var adjustedPoi = getAdjustedPoi(request.poi, request.zones);
        response = await runSimulationApi({...request, poi: adjustedPoi});
    }
    catch
    {
        isSuccess = false
    }

    return {isSuccess, response};
});

const $simulationResult = createStore<ResultEdgeResponse[]>([])
.on(runSimulationFx.doneData, (state, result)=> {
    return result.isSuccess ? result.response : state
})
.reset(runSimulation)

const $canSimulate = globalStores.$globalState.map(x=>x.zones.length > 0 && x.poi.length > 0)

sample({
    clock: runSimulation,
    source: globalStores.$globalState,
    target: runSimulationFx
})

export const events = {
    runSimulation
}

export const stores = {
    $simulationResult,
    $canSimulate,
    $isSimulating: runSimulationFx.pending
}
import {stores as zonesStore} from "./zonesStore"
import {stores as poiStores} from "../components/PoiManager/models"
import { combine, createEffect, createEvent, sample } from "effector"
import { Zone } from "../types/Zone"
import { Poi } from "../types/Poi"
import { convertToSnakeCase } from "../utils/convertToSnakeCase"

type GlobalStateProps = {
    zones: Zone[],
    poi: Poi[]
}

const getJson = createEvent();

const getJsonFx = createEffect((state: GlobalStateProps)=>{
    console.log(convertToSnakeCase(JSON.stringify(state, null, 2)));
})

const $globalState = combine([zonesStore.$allZones, poiStores.$store], ([$allZones, poiStore]):GlobalStateProps => ({
    zones: $allZones,
    poi: poiStore.poi
}))

sample({
    clock: getJson,
    source: $globalState,
    target: getJsonFx
})

export const stores = {
    $globalState
}

export const events = {
    getJson
}
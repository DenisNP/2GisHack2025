import { combine, createEvent, createStore, restore, sample } from "effector";
import { ZoneData } from "../components/ZoneDrawer";
import { Zone, ZoneType } from "../types/Zone";
import { MapStore, stores as mapStores } from "./mapStore";
import { projectPoint } from "../utils/pointsProjection";
import { GeoPoint } from "../types/GeoPoint";
import { events as poiEvents } from "../components/PoiManager/models";
import { events as mapEvents } from "./mapStore";

type AddZoneProps = {
    type: ZoneType,
    coords: GeoPoint[] 
}

const setAvailableZones = createEvent<ZoneData[]>()
const addAvailabelZone = createEvent<ZoneData>()
const setRestrictedZones = createEvent<ZoneData[]>()
const addRestrictedZone = createEvent<ZoneData>()
const setUrbanZones = createEvent<ZoneData[]>()
const addUrbanZone = createEvent<ZoneData>()
const setBaseZones = createEvent<ZoneData[]>()

// События для управления состоянием рисования зон
const setDrawingForZone = createEvent<{ zoneType: ZoneType; isDrawing: boolean }>();
const cancelAllDrawing = createEvent();
const setActiveZone = createEvent<ZoneType | null>();
const addBaseZone = createEvent<ZoneData>()
const incrementZoneId = createEvent<void>()
const setNewId = createEvent<number>()
const addZone = createEvent<AddZoneProps>()
const clearAllZones = createEvent()

const $availableZones = restore(setAvailableZones, []).on(addAvailabelZone, (state, zone) => [
    ...state,
    zone])
const $restrictedZones = restore(setRestrictedZones, []).on(addRestrictedZone, (state, zone) => [
    ...state,
    zone])
const $urbanZones = restore(setUrbanZones, []).on(addUrbanZone, (state, zone) => [
    ...state,
    zone])
const $baseZones = restore(setBaseZones, []).on(addBaseZone, (state, zone) => [
    ...state,
    zone])
const $newZoneId = createStore(1)

// Stores для управления состоянием рисования зон
const $drawingStates = createStore<Record<ZoneType, boolean>>({
    [ZoneType.None]: false,
    [ZoneType.Available]: false,
    [ZoneType.Restricted]: false,
    [ZoneType.Urban]: false,
});

const $activeZone = createStore<ZoneType | null>(null);

// Обновляем состояние рисования и активной зоны
$drawingStates.on(setDrawingForZone, (state, { zoneType, isDrawing }) => ({
    ...state,
    [zoneType]: isDrawing
}));

$drawingStates.on(cancelAllDrawing, () => ({
    [ZoneType.None]: false,
    [ZoneType.Available]: false,
    [ZoneType.Restricted]: false,
    [ZoneType.Urban]: false,
}));

$activeZone.on(setActiveZone, (_, zoneType) => zoneType);

// Обновляем ID зон
$newZoneId
    .on(incrementZoneId, (state) => state + 1)
    .on(setNewId, (_, newId) => newId);

const mapZoneData = (zoneData: ZoneData, zoneType: ZoneType, mapStore: MapStore): Zone => {
    if(!mapStore.map)
    {
        return {
            id: -1,
            type: ZoneType.Restricted,
            region: []
        }
    }
    return {
        id: zoneData.id,
        type: zoneType,
        region: zoneData.coords.map(geoPoint => projectPoint(geoPoint, mapStore.origin, mapStore.map!)),
    }
}

const $allZones = combine([$baseZones, $availableZones, $restrictedZones, $urbanZones, mapStores.$mapStore], 
    ([baseZones, availableZones, restrictedZones, urbanZonde, mapStore]) => {
        if(!mapStore.map)
        {
            return [];
        }

    return [
        ...baseZones.map(x=>mapZoneData(x, ZoneType.None, mapStore)),
        ...availableZones.map(x=>mapZoneData(x, ZoneType.Available, mapStore)), 
        ...restrictedZones.map(x=>mapZoneData(x, ZoneType.Restricted, mapStore)),
        ...urbanZonde.map(x=>mapZoneData(x, ZoneType.Urban, mapStore)),
    ]
})

const $hasBaseZone = $baseZones.map(zones => zones.length > 0);

sample({
    clock: addZone,
    source: $newZoneId,
    filter: (_, data)=> data.type === ZoneType.Available,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addAvailabelZone, incrementZoneId]
})

sample({
    clock: addZone,
    source: $newZoneId,
    filter: (_, data)=> data.type === ZoneType.Restricted,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addRestrictedZone, incrementZoneId]
})

sample({
    clock: addZone,
    source: $newZoneId,
    filter: (_, data)=> data.type === ZoneType.Urban,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addUrbanZone, incrementZoneId]
})

sample({
    clock: addZone,
    source: $newZoneId,
    filter: (_, data)=> data.type === ZoneType.None,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addBaseZone, incrementZoneId]
})

sample({
    clock: setBaseZones,
    filter: (zones)=> zones.length === 0,
    target: clearAllZones
})

sample({
    clock: clearAllZones,
    fn: () => [],
    target: [setAvailableZones, setRestrictedZones, setUrbanZones, poiEvents.removeAllPoi],
})

sample({
    clock: setBaseZones,
    filter: (zones)=> zones.length !== 0,
    fn: (zones)=> zones[0].coords[0],
    target: mapEvents.setOrigin
})

// Логика управления рисованием при смене активной зоны
sample({
    clock: setActiveZone,
    target: cancelAllDrawing
});

sample({
    clock: setActiveZone,
    source: { baseZones: $baseZones },
    filter: (_, zoneType) => zoneType !== null,
    fn: ({ baseZones }, zoneType) => {
        // Проверяем можно ли рисовать (для None - только если нет зон)
        const canDraw = zoneType !== ZoneType.None || baseZones.length === 0;
        return { zoneType: zoneType!, isDrawing: canDraw };
    },
    target: setDrawingForZone
});

export const stores = {
    $availableZones,
    $restrictedZones,
    $urbanZones,
    $baseZones,
    $allZones,
    $newZoneId,
    $hasBaseZone,
    $drawingStates,
    $activeZone,
    // Функция для получения состояния рисования конкретной зоны
    getDrawingState: (zoneType: ZoneType) => $drawingStates.map(states => states[zoneType])
}

export const events = {
    setAvailableZones,
    setRestrictedZones,
    setUrbanZones,
    setBaseZones,
    incrementZoneId,
    addZone,
    setNewId,
    clearAllZones,
    setDrawingForZone,
    cancelAllDrawing,
    setActiveZone
}
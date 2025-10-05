import { useUnit } from "effector-react"
import { stores, events } from "../../stores/zonesStore"

export const useZones = () => {
    const [availableZones, 
        restrictedZones,
        urbanZones,
        baseZones,
        allZones,
        hasBaseZone,
        setAvailableZones,
        setRestrictedZones,
        setUrbanZones,
        setBaseZones,
    ] = useUnit([
        stores.$availableZones, 
        stores.$restrictedZones, 
        stores.$urbanZones,
        stores.$baseZones,
        stores.$allZones,
        stores.$hasBaseZone,
        events.setAvailableZones,
        events.setRestrictedZones,
        events.setUrbanZones,
        events.setBaseZones,
    ])

    return {
        availableZones,
        restrictedZones,
        urbanZones,
        baseZones,
        allZones,
        hasBaseZone,
        setAvailableZones,
        setRestrictedZones,
        setUrbanZones,
        setBaseZones,
    }
}
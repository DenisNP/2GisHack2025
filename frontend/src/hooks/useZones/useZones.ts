import { useUnit } from "effector-react"
import { stores, events } from "../../stores/zonesStore"

export const useZones = () => {
    const [availableZones, 
        restrictedZones,
        sidewalks,
        allZones,
        setAvailableZones,
        setRestrictedZones,
        setSidewalks
    ] = useUnit([
        stores.$availableZones, 
        stores.$restrictedZones, 
        stores.$sidewalks, 
        stores.$allZones,
        events.setAvailableZones,
        events.setRestrictedZones,
        events.setSidewalks
    ])

    console.log({allZones})

    return {
        availableZones,
        restrictedZones,
        sidewalks,
        allZones,
        setAvailableZones,
        setRestrictedZones,
        setSidewalks
    }
}
import { useUnit } from "effector-react"
import { events } from "../../stores/zonesStore"

export const useSaveRestoreZones = ()=>{
    const [saveZones, restoreZones] = useUnit([events.saveZones, events.restoreZones])

    return {saveZones, restoreZones}
}
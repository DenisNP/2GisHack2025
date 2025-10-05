import { useUnit } from "effector-react"
import { events, stores } from "../../stores/globalState"
import { stores as zonesStores } from "../../stores/zonesStore" 

export const useSaveRestoreState = ()=>{
    const [canSave,
        hasSavedState, 
        saveCurrentState, 
        restoreState] = useUnit([
        zonesStores.$hasBaseZone,
        stores.$hasSavedState,
        events.saveCurrentState, 
        events.restoreState])

    return { canSave, hasSavedState, saveCurrentState, restoreState }
}
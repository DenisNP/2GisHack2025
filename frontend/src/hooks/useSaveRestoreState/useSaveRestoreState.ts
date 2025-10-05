import { useUnit } from "effector-react"
import { events } from "../../stores/globalState"

export const useSaveRestoreState = ()=>{
    const [saveCurrentState, restoreState] = useUnit([events.saveCurrentState, events.restoreState])

    return {saveCurrentState, restoreState}
}
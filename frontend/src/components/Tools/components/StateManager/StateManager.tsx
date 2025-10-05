import { Button, Stack, Typography } from "@mui/material"
import { useSaveRestoreState } from "../../../../hooks/useSaveRestoreState"

export const StateManager : React.FC = () => {
    const {canSave, hasSavedState, saveCurrentState, restoreState} = useSaveRestoreState()
    
    return <Stack spacing={1}>
        <Typography variant="groupHeader">
            Управление состоянием
        </Typography>
        <Button 
            onClick={saveCurrentState}
            variant="contained"
            disabled={!canSave}
            fullWidth
        >
            Сохранить тек. состояние
        </Button>
        <Button 
            onClick={restoreState}
            variant="contained"
            disabled={!hasSavedState}
            fullWidth
        >
            Восстановить сохр. состояние
        </Button>
    </Stack>
}
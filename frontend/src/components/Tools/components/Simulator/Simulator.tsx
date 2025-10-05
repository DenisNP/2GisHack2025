import { useUnit } from "effector-react"
import { events, stores } from "../../../../stores/simulationState"
import { 
    Stack, 
    Typography, 
    Button, 
    CircularProgress, 
    Alert
} from "@mui/material"
import { PlayArrow as PlayArrowIcon } from "@mui/icons-material"
import { SimulationLines } from "./components/SimulationLines"

export const Simulator : React.FC = () => {
    const [$canSimulate, $isSimulating, runSimulation] = useUnit([
        stores.$canSimulate, 
        stores.$isSimulating, 
        events.runSimulation])

    // Определяем причину недоступности симуляции
    const getUnavailabilityReason = () => {
        // Здесь можно добавить логику для определения конкретной причины
        // Пока возвращаем общую причину
        return "Для запуска симуляции необходимо настроить рабочую область и добавить зоны";
    };

    return <Stack spacing={2}>
        <Typography variant="groupHeader">
            Симуляция
        </Typography>
        
        {/* Кнопка запуска симуляции */}
        <Button
            onClick={runSimulation}
            disabled={!$canSimulate || $isSimulating}
            variant="contained"
            fullWidth
            startIcon={
                $isSimulating ? (
                    <CircularProgress size={20} color="inherit" />
                ) : (
                    <PlayArrowIcon />
                )
            }
            sx={{
                height: 48,
                '& .MuiCircularProgress-root': {
                    marginRight: 1
                }
            }}
        >
            {$isSimulating ? 'Выполняется симуляция...' : 'Запустить симуляцию'}
        </Button>

        {/* Сообщение о недоступности */}
        {!$canSimulate && (
            <Alert severity="info" variant="outlined">
                <Typography variant="body2">
                    {getUnavailabilityReason()}
                </Typography>
            </Alert>
        )}

        {/* Компонент для рисования линий симуляции на карте */}
        <SimulationLines />
    </Stack>
}
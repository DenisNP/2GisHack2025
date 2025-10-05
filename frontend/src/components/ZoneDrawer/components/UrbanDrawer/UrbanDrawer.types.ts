export type UrbanDrawerProps = {
    isActiveZone: boolean;
    onDrawingStart?: () => void; // колбэк при начале рисования тротуара
    onDrawingCancel?: () => void; // колбэк при отмене рисования тротуара
    shouldCancelDrawing?: boolean; // флаг для принудительной отмены рисования
}
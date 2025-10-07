import React from 'react';
import { Box, Typography, CircularProgress } from '@mui/material';
import { keyframes } from '@mui/system';

// Создаем анимацию для каждой линии с ее реальной длиной
const createLineAnimation = (length: number) => keyframes`
  0% {
    stroke-dashoffset: ${length};
    opacity: 0;
  }
  5% {
    opacity: 0.15;
  }
  45% {
    stroke-dashoffset: 0;
    opacity: 0.15;
  }
  55% {
    stroke-dashoffset: 0;
    opacity: 0.15;
  }
  95% {
    stroke-dashoffset: ${-length};
    opacity: 0.15;
  }
  100% {
    stroke-dashoffset: ${-length};
    opacity: 0;
  }
`;

interface SimulationOverlayProps {
  isVisible: boolean;
}

// Генерируем случайные линии-дорожки от края до края
const generatePathLines = () => {
  const lines = [];
  
  // Создаем больше линий с одинаковой скоростью
  const lineCount = 15 + Math.floor(Math.random() * 10); // 15-25 линий
  const uniformDuration = 18; // Оптимальная скорость - 18 секунд
  
  for (let i = 0; i < lineCount; i++) {
    let startX = 0, startY = 0, endX = 0, endY = 0;
    
    // Случайно выбираем с какого края начинать
    const startEdge = Math.floor(Math.random() * 4); // 0=верх, 1=право, 2=низ, 3=лево
    const endEdge = Math.floor(Math.random() * 4);
    
    // Стартовая точка на краю экрана
    switch (startEdge) {
      case 0: // верх
        startX = Math.random() * 100;
        startY = 0;
        break;
      case 1: // право
        startX = 100;
        startY = Math.random() * 100;
        break;
      case 2: // низ
        startX = Math.random() * 100;
        startY = 100;
        break;
      case 3: // лево
        startX = 0;
        startY = Math.random() * 100;
        break;
    }
    
    // Конечная точка на противоположном или соседнем краю
    switch (endEdge) {
      case 0: // верх
        endX = Math.random() * 100;
        endY = 0;
        break;
      case 1: // право
        endX = 100;
        endY = Math.random() * 100;
        break;
      case 2: // низ
        endX = Math.random() * 100;
        endY = 100;
        break;
      case 3: // лево
        endX = 0;
        endY = Math.random() * 100;
        break;
    }
    
    // Избегаем линий из одной точки в ту же точку
    if (startEdge === endEdge && Math.abs(startX - endX) + Math.abs(startY - endY) < 10) {
      continue;
    }
    
    const delay = Math.random() * 8; // Короткие задержки до 8 секунд
    
    // Вычисляем длину линии для stroke-dasharray
    const deltaX = endX - startX;
    const deltaY = endY - startY;
    const length = Math.sqrt(deltaX * deltaX + deltaY * deltaY) * 15; // увеличиваем масштаб
    
    lines.push({
      id: i,
      startX,
      startY,
      endX,
      endY,
      duration: uniformDuration, // Одинаковая скорость
      delay,
      length,
      strokeWidth: 2 + Math.random() * 3, // 2-5px - более толстые линии
      opacity: 0.08 + Math.random() * 0.07, // 0.08-0.15 - очень прозрачные серые линии
    });
  }
  
  return lines;
};

export const SimulationOverlay: React.FC<SimulationOverlayProps> = ({ isVisible }) => {
  const pathLines = React.useMemo(() => generatePathLines(), []);

  return (
    <Box
      sx={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        zIndex: 9999,
        background: 'rgba(0, 0, 0, 0.8)',
        display: isVisible ? 'flex' : 'none',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        backdropFilter: 'blur(3px)',
        pointerEvents: isVisible ? 'all' : 'none',
      }}
    >
      {/* Убираем Fade анимацию для статичного отображения */}
      {isVisible && (
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            textAlign: 'center',
            opacity: 1, // Статичная прозрачность
          }}
        >
          {/* Простой прогресс-индикатор без лишних анимаций */}
          <Box sx={{ position: 'relative', mb: 4 }}>
            <CircularProgress
              size={80}
              thickness={3}
              sx={{
                color: '#64b5f6', // Нейтральный синий цвет
                '& .MuiCircularProgress-circle': {
                  strokeLinecap: 'round',
                },
              }}
            />
          </Box>

          {/* Заголовок без анимации */}
          <Typography
            variant="h4"
            sx={{
              color: 'white',
              fontWeight: 'bold',
              mb: 2,
              textShadow: '2px 2px 8px rgba(0,0,0,0.7)',
            }}
          >
            Симуляция пешеходных потоков
          </Typography>

          {/* Подзаголовок без точек */}
          <Typography
            variant="h6"
            sx={{
              color: 'rgba(255,255,255,0.9)',
              mb: 4,
              textShadow: '1px 1px 4px rgba(0,0,0,0.5)',
            }}
          >
            Обрабатываем данные...
          </Typography>

          {/* Информационные карточки без анимации */}
          <Box
            sx={{
              display: 'flex',
              gap: 3,
              flexWrap: 'wrap',
              justifyContent: 'center',
              maxWidth: 600,
            }}
          >
            {[
              { icon: '🚶‍♂️', text: 'Анализ движения' },
              { icon: '🗺️', text: 'Обработка зон' },
              { icon: '📊', text: 'Построение маршрутов' },
            ].map((item, index) => (
              <Box
                key={index}
                sx={{
                  background: 'rgba(100, 181, 246, 0.15) !important', // Нейтральный синий фон
                  borderRadius: 3,
                  padding: 2,
                  backdropFilter: 'blur(10px)',
                  border: '1px solid rgba(100, 181, 246, 0.3) !important', // Нейтральный бордер
                  minWidth: 160,
                  // Отключаем любые эффекты изменения цвета
                  transition: 'none !important',
                  '&:hover': {
                    background: 'rgba(100, 181, 246, 0.15) !important',
                    border: '1px solid rgba(100, 181, 246, 0.3) !important',
                  },
                  '&:focus': {
                    background: 'rgba(100, 181, 246, 0.15) !important',
                    border: '1px solid rgba(100, 181, 246, 0.3) !important',
                  },
                  '&:active': {
                    background: 'rgba(100, 181, 246, 0.15) !important',
                    border: '1px solid rgba(100, 181, 246, 0.3) !important',
                  },
                }}
              >
                <Typography
                  variant="h5"
                  sx={{ mb: 1, textAlign: 'center' }}
                >
                  {item.icon}
                </Typography>
                <Typography
                  variant="body2"
                  sx={{
                    color: 'rgba(255,255,255,0.9)',
                    textAlign: 'center',
                    fontWeight: 500,
                  }}
                >
                  {item.text}
                </Typography>
              </Box>
            ))}
          </Box>
        </Box>
      )}

      {/* Анимированные линии-дорожки в фоне */}
      {isVisible && (
        <Box
          component="svg"
          sx={{
            position: 'absolute',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            pointerEvents: 'none',
            zIndex: -1, // Еще глубже в фоне
          }}
          viewBox="0 0 100 100"
          preserveAspectRatio="none"
        >
          {pathLines.map((line) => (
            <Box
              key={line.id}
              component="line"
              sx={{
                stroke: '#888888', // Серый цвет
                strokeWidth: `${line.strokeWidth / 5}`, // увеличиваем толщину для viewBox
                strokeLinecap: 'round',
                fill: 'none',
                strokeDasharray: line.length,
                strokeDashoffset: line.length,
                animation: `${createLineAnimation(line.length)} ${line.duration}s linear infinite`,
                animationDelay: `${line.delay}s`,
                opacity: line.opacity,
              }}
              x1={line.startX}
              y1={line.startY}
              x2={line.endX}
              y2={line.endY}
            />
          ))}
        </Box>
      )}
    </Box>
  );
};
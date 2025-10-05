import { createTheme } from '@mui/material/styles';

// Расширяем типы палитры для нового цвета
declare module '@mui/material/styles' {
  interface Palette {
    neutral: Palette['primary'];
  }
  interface PaletteOptions {
    neutral?: PaletteOptions['primary'];
  }
}

// Расширяем типы MUI для наших кастомных вариантов
declare module '@mui/material/Typography' {
  interface TypographyPropsVariantOverrides {
    groupHeader: true;
  }
}

declare module '@mui/material/Button' {
  interface ButtonPropsVariantOverrides {
    panel: true;
    success: true;
    subtle: true;
  }
  interface ButtonPropsColorOverrides {
    neutral: true;
  }
}

// Создаем кастомную тему
export const customTheme = createTheme({
  palette: {
    // Добавляем новый цвет для кнопок
    neutral: {
      main: '#757575', // серый цвет
      contrastText: '#ffffff',
    },
  },
  components: {
    // Кастомный вариант для Typography - заголовки групп
    MuiTypography: {
      variants: [
        {
          props: { variant: 'groupHeader' },
          style: {
            fontWeight: 'bold',
            color: 'text.secondary',
            fontSize: '0.875rem',
            marginBottom: '-8px', // уменьшаем отступ снизу
            textTransform: 'none',
          },
        },
      ],
    },
    // Глобальные стили для всех Button компонентов
    MuiButton: {
      styleOverrides: {
        root: {
          justifyContent: 'flex-start',
          textTransform: 'none',
        },
      },
      variants: [
        {
          props: { variant: 'panel' },
          style: {
            textAlign: 'left',
            paddingLeft: '16px',
            paddingRight: '16px',
            borderRadius: '8px',
            '&:hover': {
              backgroundColor: 'action.hover',
            },
          },
        },
        // Зеленая кнопка для загрузки API
        {
          props: { variant: 'success' },
          style: {
            backgroundColor: '#4caf50',
            color: 'white',
            '&:hover': {
              backgroundColor: '#45a049',
            },
            '&:disabled': {
              backgroundColor: '#a5d6a7',
              color: 'white',
            },
          },
        },
        // Серая кнопка для удаления (ненавязчивая)
        {
          props: { variant: 'subtle' },
          style: {
            backgroundColor: 'transparent',
            color: '#757575', // серый цвет
            borderColor: '#bdbdbd', // серая граница
            '&:hover': {
              backgroundColor: '#f5f5f5',
              borderColor: '#9e9e9e',
              color: '#616161',
            },
            '&:disabled': {
              backgroundColor: 'transparent',
              color: '#e0e0e0',
              borderColor: '#e0e0e0',
            },
          },
        },
      ],
    },
  },
});
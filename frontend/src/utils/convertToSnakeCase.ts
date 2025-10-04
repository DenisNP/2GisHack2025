// Функция для преобразования camelCase в snake_case
const toSnakeCase = (str: string): string => {
    return str.replace(/[A-Z]/g, (letter) => `_${letter.toLowerCase()}`);
};

// Функция для рекурсивного преобразования объекта в snake_case
export const convertToSnakeCase = (obj: any): any => {
    if (Array.isArray(obj)) {
        return obj.map(convertToSnakeCase);
    } else if (obj !== null && typeof obj === 'object') {
        return Object.keys(obj).reduce((acc, key) => {
            const snakeKey = toSnakeCase(key);
            acc[snakeKey] = convertToSnakeCase(obj[key]);
            return acc;
        }, {} as any);
    }
    return obj;
};


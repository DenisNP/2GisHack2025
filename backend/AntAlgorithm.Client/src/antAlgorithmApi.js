// Base configuration
const API_BASE_URL = 'https://localhost:7034/';

// Types (based on your schemas)
/**
 * @typedef {Object} Point
 * @property {number} x
 * @property {number} y
 */

/**
 * @typedef {Object} Poi
 * @property {number} id
 * @property {Point} point
 * @property {number} weight
 */

/**
 * @typedef {Object} Result
 * @property {Poi} from
 * @property {Poi} to
 * @property {number} weight
 */

// Error class for API errors
class ApiError extends Error {
    /**
     * @param {string} message
     * @param {number} status
     * @param {any} response
     */
    constructor(message, status, response) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
        this.response = response;
    }
}

// Utility functions
const AntAlgorithmUtils = {
    /**
     * Create a Point object
     * @param {number} x
     * @param {number} y
     * @returns {Point}
     */
    createPoint(x, y) {
        return { x, y };
    },

    /**
     * Create a Poi object
     * @param {number} id
     * @param {Point} point
     * @param {number} weight
     * @returns {Poi}
     */
    createPoi(id, point, weight) {
        return { id, point, weight };
    },

    /**
     * Handle HTTP response
     * @param {Response} response
     * @returns {Promise<any>}
     */
    async handleResponse(response) {
        if (!response.ok) {
            let errorData;
            try {
                errorData = await response.json();
            } catch {
                errorData = await response.text();
            }
            throw new ApiError(
                `HTTP error! status: ${response.status}`,
                response.status,
                errorData
            );
        }
        return response;
    }
};

// API Client
const antAlgorithmApi = {
    /**
     * Get hello message
     * @returns {Promise<string>}
     */
    async getHello() {
        const response = await fetch(`${API_BASE_URL}hello`, {
            method: 'GET',
            headers: {
                'Accept': 'text/plain',
            },
        });

        await AntAlgorithmUtils.handleResponse(response);
        return await response.text();
    },

    async getAntAlgoInfo(edges) {
        try{
            const response = await fetch(`${API_BASE_URL}getAllWays`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                },
                body: JSON.stringify(edges),
            });

            await AntAlgorithmUtils.handleResponse(response);
            return await response.json();
        }catch (e) {
            console.error(e);
        }        
    },

    /**
     * Set custom base URL
     * @param {string} baseUrl
     */
    setBaseUrl(baseUrl) {
        this.baseUrl = baseUrl.endsWith('/') ? baseUrl : baseUrl + '/';
    }
};

// Initialize baseUrl property
antAlgorithmApi.baseUrl = API_BASE_URL;

// Class-based version
class AntAlgorithmClient {
    /**
     * @param {string} baseURL
     */
    constructor(baseURL = API_BASE_URL) {
        this.baseURL = baseURL.endsWith('/') ? baseURL : baseURL + '/';
    }

    /**
     * Get hello message
     * @returns {Promise<string>}
     */
    async getHello() {
        const response = await fetch(`${this.baseURL}hello`, {
            method: 'GET',
            headers: {
                'Accept': 'text/plain',
            },
        });

        await this.handleResponse(response);
        return await response.text();
    }

    /**
     * Get ant algorithm information
     * @param {Poi[]} pois - Array of POI objects
     * @returns {Promise<Result[]>}
     */
    async getAntAlgoInfo(pois) {
        try{
            const response = await fetch(`${this.baseURL}getAntAlgoInfo`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                },
                body: JSON.stringify(pois),
            });

            await this.handleResponse(response);
            return await response.json();
        }catch(err) {
            console.log(err.message);
        }
    }

    /**
     * Handle HTTP response
     * @param {Response} response
     * @returns {Promise<any>}
     */
    async handleResponse(response) {
        if (!response.ok) {
            let errorData;
            try {
                errorData = await response.json();
            } catch {
                errorData = await response.text();
            }
            throw new ApiError(
                `HTTP error! status: ${response.status}`,
                response.status,
                errorData
            );
        }
        return response;
    }

    /**
     * Set base URL
     * @param {string} baseURL
     */
    setBaseURL(baseURL) {
        this.baseURL = baseURL.endsWith('/') ? baseURL : baseURL + '/';
    }
}

// Enhanced version with interceptors and timeout support
class AdvancedAntAlgorithmClient {
    /**
     * @param {Object} options
     * @param {string} options.baseURL
     * @param {number} options.timeout
     * @param {Object} options.headers
     */
    constructor(options = {}) {
        this.baseURL = (options.baseURL || API_BASE_URL).endsWith('/')
            ? options.baseURL || API_BASE_URL
            : (options.baseURL || API_BASE_URL) + '/';
        this.timeout = options.timeout || 10000;
        this.defaultHeaders = {
            'Content-Type': 'application/json',
            ...options.headers,
        };
    }

    /**
     * Get hello message
     * @returns {Promise<string>}
     */
    async getHello() {
        return this.fetchWithTimeout(`${this.baseURL}hello`, {
            method: 'GET',
            headers: {
                'Accept': 'text/plain',
            },
        }).then(response => response.text());
    }

    /**
     * Get ant algorithm information
     * @param {Poi[]} pois - Array of POI objects
     * @returns {Promise<Result[]>}
     */
    async getAntAlgoInfo(pois) {
        return this.fetchWithTimeout(`${this.baseURL}getAntAlgoInfo`, {
            method: 'POST',
            headers: {
                ...this.defaultHeaders,
                'Accept': 'application/json',
            },
            body: JSON.stringify(pois),
        }).then(response => response.json());
    }

    /**
     * Fetch with timeout
     * @param {string} url
     * @param {RequestInit} options
     * @returns {Promise<Response>}
     */
    async fetchWithTimeout(url, options = {}) {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), this.timeout);

        try {
            const response = await fetch(url, {
                ...options,
                signal: controller.signal,
            });

            clearTimeout(timeoutId);

            if (!response.ok) {
                let errorData;
                try {
                    errorData = await response.json();
                } catch {
                    errorData = await response.text();
                }
                throw new ApiError(
                    `HTTP error! status: ${response.status}`,
                    response.status,
                    errorData
                );
            }

            return response;
        } catch (error) {
            clearTimeout(timeoutId);
            if (error.name === 'AbortError') {
                throw new ApiError('Request timeout', 408, 'Request timed out');
            }
            throw error;
        }
    }
}

export {
    antAlgorithmApi,
    AntAlgorithmClient,
    AdvancedAntAlgorithmClient,
    AntAlgorithmUtils,
    ApiError
};

// Default export
export default antAlgorithmApi;
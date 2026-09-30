/**
 * Global Application Configuration
 * Controlled directly by environment variables in .env.
 */

// Refresh interval in seconds (default: 20 seconds)
export const REFRESH_INTERVAL_SECONDS = parseInt(
    (typeof process !== 'undefined' && process.env && process.env.REACT_APP_REFRESH_INTERVAL_SECONDS) || '20',
    10
);

// Refresh interval in milliseconds for timers
export const REFRESH_INTERVAL_MS = REFRESH_INTERVAL_SECONDS * 1000;

// Backend API Base URL: directly configured via .env
export const API_BASE_URL = 
    (typeof process !== 'undefined' && process.env && process.env.REACT_APP_BACKEND_URL) ||
    'http://localhost:5152/api';

// Backend Label: directly uses REACT_APP_BACKEND_LABEL
export const BACKEND_LABEL = 
    (typeof process !== 'undefined' && process.env && process.env.REACT_APP_BACKEND_LABEL) ||
    '';

export const BACKEND_LABEL_LONG = BACKEND_LABEL;

// Badge styling: uses legacy badge styling if label contains 'legacy', otherwise uplifted style
export const IS_LEGACY_BACKEND = BACKEND_LABEL.toLowerCase().includes('legacy');

const config = {
    REFRESH_INTERVAL_SECONDS,
    REFRESH_INTERVAL_MS,
    API_BASE_URL,
    IS_LEGACY_BACKEND,
    BACKEND_LABEL,
    BACKEND_LABEL_LONG
};

export default config;

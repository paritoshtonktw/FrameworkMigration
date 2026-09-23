/**
 * Global Application Configuration
 * All environment-driven settings are declared here with sensible defaults.
 * Update REACT_APP_REFRESH_INTERVAL_SECONDS in .env to change globally.
 */

// Refresh interval in seconds (default: 20 seconds)
export const REFRESH_INTERVAL_SECONDS = parseInt(
    process.env.REACT_APP_REFRESH_INTERVAL_SECONDS || '20',
    10
);

// Refresh interval in milliseconds for timers
export const REFRESH_INTERVAL_MS = REFRESH_INTERVAL_SECONDS * 1000;

// Backend URL defaults
const LEGACY_URL = 'http://localhost:44341/api';
const UPLIFTED_URL = 'http://localhost:5152/api';

// Detect mode from multiple environment keys
const getEnvValue = () => {
    if (typeof process !== 'undefined' && process.env) {
        return (
            process.env.REACT_APP_BACKEND ||
            process.env.REACT_APP_ENV ||
            process.env.REACT_APP_BACKEND_ENV ||
            process.env.REACT_APP_ACTIVE_BACKEND ||
            ''
        ).toLowerCase();
    }
    return '';
};

const getQueryValue = () => {
    if (typeof window !== 'undefined' && window.location) {
        const urlParams = new URLSearchParams(window.location.search);
        return (urlParams.get('backend') || urlParams.get('env') || '').toLowerCase();
    }
    return '';
};

const envVal = getEnvValue() || getQueryValue();
const isLegacy = envVal.includes('legacy') || (typeof process !== 'undefined' && process.env && process.env.REACT_APP_API_BASE_URL && process.env.REACT_APP_API_BASE_URL.includes('44341'));

// Backend API Base URL
export const API_BASE_URL = isLegacy 
    ? LEGACY_URL 
    : ((typeof process !== 'undefined' && process.env && process.env.REACT_APP_API_BASE_URL) || UPLIFTED_URL);

export const IS_LEGACY_BACKEND = isLegacy;
export const BACKEND_LABEL = IS_LEGACY_BACKEND ? 'LEGACY AS-IS' : 'UPLIFTED CORE';
export const BACKEND_LABEL_LONG = IS_LEGACY_BACKEND ? 'LEGACY AS-IS PLATFORM' : 'UPLIFTED .NET CORE PLATFORM';

const config = {
    REFRESH_INTERVAL_SECONDS,
    REFRESH_INTERVAL_MS,
    API_BASE_URL,
    IS_LEGACY_BACKEND,
    BACKEND_LABEL,
    BACKEND_LABEL_LONG
};

export default config;


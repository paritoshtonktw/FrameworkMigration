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

// Backend API Base URL
export const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:5152/api';

const config = {
    REFRESH_INTERVAL_SECONDS,
    REFRESH_INTERVAL_MS,
    API_BASE_URL
};

export default config;


/// <reference path="../lib/jquery/dist/jquery.js" />

/**
 * Initializes session timeout functionality with warning dialog, countdown,
 * and automatic redirection. Also supports cross-tab synchronization using localStorage.
 *
 * @param {number} sessionTimeoutInSeconds - Total session duration in seconds
 * @param {number} warningBeforeInSeconds - Time before expiry to show warning
 * @param {string} timeoutUrl - URL to redirect to after session expiry
 */
function initSessionTimeout(sessionTimeoutInSeconds, warningBeforeInSeconds, timeoutUrl) {
    const sessionStorageKey = "shared-session-timeout"; // localStorage key for session expiry timestamp
    const sessionTimeoutMs = sessionTimeoutInSeconds * 1000;

    // Handles for all active timers
    const timers = {
        visibleCountdown: null,
        screenReaderAnnouncement: null,
        warningDialog: null,
        redirectOnTimeout: null
    };

    /**
     * Stops and clears all active timers (intervals/timeouts).
     */
    function stopAllTimers() {
        // Clear all timers and reset to null
        clearInterval(timers.visibleCountdown);
        clearInterval(timers.screenReaderAnnouncement);

        clearTimeout(timers.warningDialog);
        clearTimeout(timers.redirectOnTimeout);
    }

    /**
     * Sets the session expiry timestamp in localStorage.
     */
    function setSessionExpiryTimestamp() {
        const expiryTimestamp = Date.now() + sessionTimeoutMs;
        localStorage.setItem(sessionStorageKey, expiryTimestamp.toString());
    }

    /**
     * Retrieves the expiry timestamp from localStorage, or null if not present.
     */
    function getSessionExpiryTimestamp() {
        const rawValue = localStorage.getItem(sessionStorageKey);
        return rawValue ? parseInt(rawValue, 10) : null;
    }

    /**
     * Formats a duration in seconds into a human-readable string like "2 minutes and 30 seconds".
     */
    function formatRemainingTime(seconds) {
        // Calculate minutes and remaining seconds from total seconds
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = seconds % 60;
        const parts = [];

        // Add minutes part if applicable
        if (minutes > 0) {
            parts.push(`${minutes} minute${minutes !== 1 ? 's' : ''}`);
        }

        // Add seconds part if applicable
        if (remainingSeconds > 0) {
            parts.push(`${remainingSeconds} second${remainingSeconds !== 1 ? 's' : ''}`);
        }

        // If no minutes or seconds, return '0 seconds'
        if (parts.length === 0) {
            return '0 seconds';
        }

        // Join parts with 'and' for human-readable output
        return parts.join(' and ');
    }

    /**
     * Announces time remaining using an ARIA live region for screen reader support.
     */
    function announceRemainingTime(secondsLeft) {
        const template = $('#timeout-message-template').text();
        const timeMessage = template.replace('{0}', formatRemainingTime(secondsLeft));
        const titleText = $('#timeout-title').text().trim();

        $('#timeout-countdown').text(`${titleText}. ${timeMessage}`);
    }

    /**
     * Hides the session timeout warning modal/dialog.
     */
    function hideTimeoutWarningDialog() {
        $('#timeout-warning-overlay, #timeout-warning').attr('hidden', true);
        document.getElementById('timeout-warning').close();
    }

    /**
     * Displays the session timeout warning modal and starts countdown timers.
     */
    function showTimeoutWarningDialog() {
        let remainingSeconds = warningBeforeInSeconds;

        $('#timeout-warning-overlay, #timeout-warning').removeAttr('hidden');
        document.getElementById('timeout-warning').showModal();
        $('#focus-element').trigger('focus');

        $('#timeout-countdown-visible').text(formatRemainingTime(remainingSeconds));

        // Visible countdown timer (updates every second)
        timers.visibleCountdown = setInterval(() => {
            // Announce remaining time at the start of the countdown
            if (remainingSeconds === warningBeforeInSeconds) {
                announceRemainingTime(remainingSeconds);
            }

            remainingSeconds--;

            // Update visible countdown text
            $('#timeout-countdown-visible').text(formatRemainingTime(remainingSeconds));

            if (remainingSeconds <= 0) {
                clearSessionTrackingKey();
                stopAllTimers();
                window.location.href = timeoutUrl;
            }
        }, 1000);

        // Screen reader updates (every 15 seconds)
        timers.screenReaderAnnouncement = setInterval(() => {
            announceRemainingTime(remainingSeconds - 5);
        }, 15000);
    }

    /**
     * Schedules timers for showing the warning and final redirection,
     * based on the timestamp in localStorage.
     */
    function setupTimersBasedOnStoredExpiry() {
        stopAllTimers();

        const expiryTimestamp = getSessionExpiryTimestamp();
        if (!expiryTimestamp) {
            return; // No session tracking set
        }

        const now = Date.now();
        const msUntilExpiry = expiryTimestamp - now; // Time remaining until session expiry in milliseconds
        const msUntilWarning = msUntilExpiry - (warningBeforeInSeconds * 1000); // Time until warning dialog in milliseconds

        if (msUntilWarning > 0) {
            // Show warning dialog after calculated delay
            timers.warningDialog = setTimeout(showTimeoutWarningDialog, msUntilWarning);
        } else if (msUntilExpiry > 0) {
            // Already within warning window
            showTimeoutWarningDialog();
        } else {
            // Session expired
            clearSessionTrackingKey();
            stopAllTimers();
            window.location.href = timeoutUrl;
        }

        // Schedule auto-redirection at expiry
        timers.redirectOnTimeout = setTimeout(() => {
            clearSessionTrackingKey();
            stopAllTimers();
            window.location.href = timeoutUrl;
        }, msUntilExpiry);
    }

    /**
     * Removes the localStorage key manually. Use this on logout.
     */
    function clearSessionTrackingKey() {
        localStorage.removeItem(sessionStorageKey);
    }

    // Initial session setup
    if (!localStorage.getItem(sessionStorageKey)) {
        setSessionExpiryTimestamp();
    }

    setupTimersBasedOnStoredExpiry();

    // Respond to session timestamp changes in other tabs (cross-tab sync)
    window.addEventListener('storage', function (event) {
        if (event.key === sessionStorageKey) {
            hideTimeoutWarningDialog();
            setupTimersBasedOnStoredExpiry();
        }
    });

    // Stay signed in button
    $('#stay-signed-in').on('click', function () {
        clearSessionTrackingKey();
        hideTimeoutWarningDialog();
        setSessionExpiryTimestamp();
        window.location.reload();
    });
}
/// <reference path="../lib/jquery/dist/jquery.js" />

// Declare the global initializer
function initSessionTimeout(sessionTimeoutInSeconds, warningBeforeInSeconds, timeoutUrl) {
    const sessionKey = 'shared-session-timeout';
    const timeoutMs = sessionTimeoutInSeconds * 1000;

    let warningTimer = null;
    let countdownTimer = null;
    let timeoutRedirectTimer = null;
    let lastSpokenValue = null;

    function setExpiryTimestamp() {
        const expiry = Date.now() + timeoutMs;
        localStorage.setItem(sessionKey, expiry.toString());
    }

    function getExpiryTimestamp() {
        const expiry = localStorage.getItem(sessionKey);
        return expiry ? parseInt(expiry, 10) : null;
    }

    function stopAllTimers() {
        clearTimeout(warningTimer);
        clearInterval(countdownTimer);
        clearTimeout(timeoutRedirectTimer);
    }

    function hideWarning() {
        $('.govuk-template__body').removeAttr('aria-hidden');
        $('.govuk-service-navigation').removeAttr('aria-hidden');
        $('#main-content').removeAttr('aria-hidden');
        $('#timeout-warning-overlay').attr('hidden', true);
        $('#timeout-warning').attr('hidden', true);
    }

    function showWarning() {
        let remaining = warningBeforeInSeconds;

        updateCountdown(remaining);
        $('.govuk-template__body').attr('aria-hidden', true);
        //$('.govuk-service-navigation').attr('aria-hidden', true);
        //$('#main-content').attr('aria-hidden', true);
        $('#timeout-warning-overlay').removeAttr('hidden');
        $('#timeout-warning').removeAttr('hidden');
        //$('#timeout-content').trigger("focus");

        //$('#timeout-countdown').text(''); // Clear previous countdown

        countdownTimer = setInterval(() => {
            remaining--;
            updateCountdown(remaining);

            if (remaining <= 0) {
                clearInterval(countdownTimer);
                window.location.href = timeoutUrl;
            }
        }, 1000);
    }

    function updateCountdown(secondsLeft) {
        const minutes = Math.floor(secondsLeft / 60);
        const seconds = secondsLeft % 60;

        let minutePart = '';
        let secondPart = '';
        let andPart = '';
        let fallbackPart = '';

        if (minutes > 0) {
            minutePart = minutes === 1 ? '1 minute' : `${minutes} minutes`;
        }

        if (seconds > 0) {
            secondPart = seconds === 1 ? '1 second' : `${seconds} seconds`;
        }

        if (minutePart && secondPart) {
            andPart = ' and ';
        }

        if (!minutePart && !secondPart && minutes === 0) {
            fallbackPart = '0 seconds';
        }

        let spoken = `${minutePart}${andPart}${secondPart}${fallbackPart}`;

        // Update visible countdown
        $('#timeout-countdown-visible').text(spoken);

        // Only update AT region if time qualifies and content has changed
        const isEvery15Sec = secondsLeft % 15 === 0;
        //const isFinal15Sec = secondsLeft <= 15;
        const isFinal = secondsLeft === 0;

        //if ((isEvery15Sec) && spoken !== lastSpokenValue) {
        if ((isEvery15Sec)) {
            const titleText = $('#timeout-title').text().trim();
            const descriptionText = $('#at-timer-description').text().trim();

            //$('#timeout-countdown').text(spoken);
            //lastSpokenValue = spoken;

            const fullAnnouncement = `${titleText}. ${descriptionText}`;
            //const $announcer = $('#modal-aria-announcer');
            $('#timeout-countdown').text(fullAnnouncement);
            //lastSpokenValue = spoken;

            ////$announcer.text(''); // Clear to ensure re-read
            //setTimeout(() => {
            //    $announcer.text(fullAnnouncement);
            //}, 100);
        }
    }

    function setupTimers() {
        stopAllTimers();

        const expiry = getExpiryTimestamp();
        if (!expiry) return;

        const now = Date.now();
        const msUntilTimeout = expiry - now;
        const msUntilWarning = msUntilTimeout - (warningBeforeInSeconds * 1000);

        if (msUntilWarning > 0) {
            // Wait until we're 120 seconds from expiry to show the warning
            warningTimer = setTimeout(showWarning, msUntilWarning);
        } else if (msUntilTimeout > 0) {
            // We're already inside the warning window
            showWarning();
        } else {
            // Already expired
            window.location.href = timeoutUrl;
        }

        timeoutRedirectTimer = setTimeout(() => {
            stopAllTimers();
            window.location.href = timeoutUrl;
        }, msUntilTimeout);
    }

    // Initial setup
    if (!localStorage.getItem(sessionKey)) {
        setExpiryTimestamp(); // Set only if not already present
    }

    setupTimers();

    //// Refresh on user activity
    //$(document).on('click keypress scroll mousemove', function () {
    //    setExpiryTimestamp();
    //});

    // Sync timers across tabs
    window.addEventListener('storage', function (e) {
        if (e.key === sessionKey) {
            hideWarning();
            setupTimers();
        }
    });

    // Stay signed in click
    $('#stay-signed-in').on('click', function () {
        // Hide warning to prevent flicker on reload
        hideWarning();

        // Optionally reset timestamp to prevent race conditions
        setExpiryTimestamp();

        // Reload page to refresh session
        window.location.reload();
    });

    // Stop timers if user signs out (we can detect signout if sessionKey is removed)
    window.onstorage = function (e) {
        if (e.key === sessionKey && e.newValue === null) {
            stopAllTimers();
            hideWarning();
        }
    };
}
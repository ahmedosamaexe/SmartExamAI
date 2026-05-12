(function () {
    'use strict';

    var submissionId = Number(document.getElementById('submissionId').value);
    var violationThreshold = Number(document.getElementById('violationThreshold').value);
    var currentViolationCount = Number(document.getElementById('violationCount').value);
    var lastViolationTime = 0;
    var COOLDOWN_MS = 3000;

    function getToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]').value;
    }

    // --- Tab/window switch detection (logging only) ---
    document.addEventListener('visibilitychange', function () {
        if (document.hidden) {
            recordViolation('TabSwitch');
        }
    });

    // --- Copy/paste prevention ---
    document.addEventListener('copy', function (e) { e.preventDefault(); });
    document.addEventListener('paste', function (e) { e.preventDefault(); });
    document.addEventListener('cut', function (e) { e.preventDefault(); });
    document.addEventListener('contextmenu', function (e) { e.preventDefault(); });

    document.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && ['c', 'v', 'x', 'a', 'u'].indexOf(e.key.toLowerCase()) !== -1) {
            e.preventDefault();
        }
    });

    // --- Record violation (logging only, no termination) ---
    function recordViolation(type) {
        var now = Date.now();
        if (now - lastViolationTime < COOLDOWN_MS) {
            return;
        }
        lastViolationTime = now;

        fetch('/Student/Exam/RecordViolation', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getToken(),
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ submissionId: submissionId, type: type })
        })
        .then(function (response) {
            if (response.ok) return response.json();
            return null;
        })
        .then(function (data) {
            if (!data || !data.success) return;
            currentViolationCount = data.violationCount;
            updateViolationBadge(currentViolationCount, violationThreshold);
        })
        .catch(function () { /* silent */ });
    }

    // --- Update violation badge ---
    function updateViolationBadge(count, threshold) {
        var vc = document.getElementById('violation-count');
        if (vc) vc.innerText = count;

        var badge = document.getElementById('violations-badge');
        if (!badge) return;

        badge.textContent = 'Violations: ' + count + ' / ' + threshold;

        if (count >= threshold - 1) {
            badge.style.backgroundColor = '#B85C5C';
            badge.style.color = '#FFFFFF';
            badge.style.borderColor = '#B85C5C';
        } else if (count > 0) {
            badge.style.backgroundColor = '#C89A3A';
            badge.style.color = '#FFFFFF';
            badge.style.borderColor = '#C89A3A';
        } else {
            badge.style.backgroundColor = '#E0DED8';
            badge.style.color = '#2C2C2C';
            badge.style.borderColor = '#E0DED8';
        }
    }

    // Init badge display
    updateViolationBadge(currentViolationCount, violationThreshold);
})();

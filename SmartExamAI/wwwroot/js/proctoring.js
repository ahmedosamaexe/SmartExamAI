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

    // --- Fullscreen enforcement ---
    function requestFS() {
        try {
            var docElm = document.documentElement;
            var promise = null;
            if (docElm.requestFullscreen) {
                promise = docElm.requestFullscreen();
            } else if (docElm.webkitRequestFullscreen) {
                promise = docElm.webkitRequestFullscreen();
            }
            if (promise) {
                promise.catch(function() { /* silent */ });
            }
        } catch (e) { 
            /* silent */
        }
    }

    document.addEventListener('DOMContentLoaded', function() {
        requestFS();
    });

    // Also call immediately in case it's deferred
    requestFS();

    document.addEventListener('fullscreenchange', function () {
        if (!document.fullscreenElement && !window.examSubmitted) {
            recordViolation('FullscreenExit');
            showToast('⚠ You exited fullscreen. This has been recorded.', 'danger', 5000);
        }
    });

    // --- Tab/window switch detection ---
    document.addEventListener('visibilitychange', function () {
        if (document.hidden) {
            recordViolation('TabSwitch');
        }
    });

    window.addEventListener('blur', function () {
        recordViolation('WindowBlur');
    });

    // --- Copy/paste/context prevention ---
    document.addEventListener('copy', function (e) { e.preventDefault(); });
    document.addEventListener('paste', function (e) { e.preventDefault(); });
    document.addEventListener('cut', function (e) { e.preventDefault(); });
    document.addEventListener('contextmenu', function (e) { e.preventDefault(); });

    document.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && ['c', 'v', 'x', 'a', 'u'].indexOf(e.key.toLowerCase()) !== -1) {
            e.preventDefault();
        }
    });

    // --- Record violation with cooldown ---
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

            if (data.isTerminated) {
                handleTermination();
            }
        })
        .catch(function () { /* silent */ });
    }

    // --- Update violation badge if it exists ---
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

    // --- Auto-termination ---
    function handleTermination() {
        // Show a persistent toast (will not auto-dismiss since we redirect)
        if (typeof showToast === 'function') {
            showToast('Exam terminated due to too many violations.', 'danger');
        }

        // Disable all inputs
        document.querySelectorAll('input, textarea, button').forEach(function (el) {
            el.disabled = true;
        });

        setTimeout(function () {
            window.location.href = '/Student/Exam/Terminated/' + submissionId;
        }, 2500);
    }

    // --- Toast function (uses exam page's custom-toast if available) ---
    function showToast(message, type, duration) {
        var container = document.getElementById('toast-container');
        if (!container) return;

        var toast = document.createElement('div');
        toast.className = 'custom-toast';
        if (type === 'warning') toast.classList.add('toast-warning');
        if (type === 'danger') toast.classList.add('toast-danger');
        toast.textContent = message;
        container.appendChild(toast);
        setTimeout(function () { toast.remove(); }, duration || 4000);
    }

    // Init badge display
    updateViolationBadge(currentViolationCount, violationThreshold);
})();

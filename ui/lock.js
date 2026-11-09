(function () {
    "use strict";

    var api = window.chrome && window.chrome.webview;
    var text = document.getElementById("lock-text");
    var unlock = document.getElementById("unlock");
    var reset = document.getElementById("reset");
    var confirm = document.getElementById("confirm");
    var confirmReset = document.getElementById("confirm-reset");
    var cancelReset = document.getElementById("cancel-reset");

    function post(message) {
        if (api) api.postMessage(message);
    }

    function showConfirm(show) {
        confirm.hidden = !show;
        reset.hidden = show;
    }

    function apply(state) {
        if (!state || state.type !== "lock") return;

        var message = state.message || "";
        var busy = state.state === "checking" || state.state === "working";

        if (state.state === "checking") message = "Waiting for Windows Hello\u2026";
        else if (state.state === "working") message = "Deleting saved data\u2026";
        else if (state.state === "failed" && !message) message = "Windows Hello did not verify. Try again.";
        else if (state.state === "unavailable" && !message) message = "Windows Hello is not available.";

        text.textContent = message || "Unlock with Windows Hello to continue.";

        unlock.textContent = state.state === "failed" || state.state === "unavailable"
            ? "Try again"
            : "Unlock with Windows Hello";
        unlock.disabled = busy;

        if (state.state !== "unavailable") showConfirm(false);
        reset.hidden = state.state !== "unavailable" || !confirm.hidden;
    }

    unlock.addEventListener("click", function () {
        text.textContent = "Waiting for Windows Hello\u2026";
        unlock.disabled = true;
        post("unlock");
    });

    reset.addEventListener("click", function () {
        showConfirm(true);
        confirmReset.focus();
    });

    cancelReset.addEventListener("click", function () {
        showConfirm(false);
        reset.focus();
    });

    confirmReset.addEventListener("click", function () {
        text.textContent = "Deleting saved data\u2026";
        confirmReset.disabled = true;
        cancelReset.disabled = true;
        post("reset");
    });

    if (api) {
        api.addEventListener("message", function (event) {
            apply(event.data);
        });
    }

    post("lock-init");
})();

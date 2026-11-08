(function () {
    "use strict";

    var api = window.chrome && window.chrome.webview;
    var version = document.getElementById("version");

    function post(message) {
        if (api) api.postMessage(message);
    }

    function applyState(state) {
        if (!state || state.type !== "state") return;

        if (version) {
            version.textContent = state.version ? "v" + state.version : "";
        }

        var dataDir = document.getElementById("data-dir");
        if (dataDir && state.dataDir) dataDir.textContent = state.dataDir;

        var downloads = document.getElementById("downloads");
        if (downloads) downloads.checked = state.downloads === true;
    }

    if (api) {
        api.addEventListener("message", function (event) {
            applyState(event.data);
        });
    }

    var downloads = document.getElementById("downloads");
    if (downloads) {
        downloads.addEventListener("change", function () {
            post("downloads:" + (this.checked ? "on" : "off"));
        });
    }

    var openData = document.getElementById("open-data");
    if (openData) {
        openData.addEventListener("click", function () {
            post("open-data-folder");
        });
    }

    var dialog = document.getElementById("clear-dialog");
    var clearButton = document.getElementById("clear-data");
    if (dialog && clearButton) {
        clearButton.addEventListener("click", function () {
            dialog.showModal();
        });
        document.getElementById("cancel-clear").addEventListener("click", function () {
            dialog.close();
        });
        document.getElementById("confirm-clear").addEventListener("click", function () {
            dialog.close();
            clearButton.disabled = true;
            clearButton.textContent = "Clearing...";
            post("clear-data");
        });
    }

    post("init");
})();

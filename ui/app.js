(function () {
    "use strict";

    var api = window.chrome && window.chrome.webview;
    var version = document.getElementById("version");
    var site = window.SITE || {};

    function applySite() {
        if (site.serviceName) {
            document.title = document.body.getAttribute("data-page") === "config"
                ? site.serviceName + " - Settings"
                : site.serviceName;
        }

        var logo = document.getElementById("logo");
        if (logo && site.serviceName) logo.alt = site.serviceName;

        var homeLink = document.getElementById("open-dashboard");
        if (homeLink) {
            if (site.homeLabel) homeLink.textContent = site.homeLabel;
            if (site.homeUrl) homeLink.href = site.homeUrl;
        }

        var appName = document.getElementById("app-name");
        if (appName && site.serviceName) appName.textContent = site.serviceName;

        var affiliation = document.getElementById("affiliation");
        if (affiliation && site.serviceAfnName) {
            affiliation.textContent = "Unofficial client. Not affiliated with, endorsed, or supported by ";
            if (site.serviceAfnUrl) {
                var serviceLink = document.createElement("a");
                serviceLink.href = site.serviceAfnUrl;
                serviceLink.textContent = site.serviceAfnName;
                affiliation.appendChild(serviceLink);
            } else {
                affiliation.appendChild(document.createTextNode(site.serviceAfnName));
            }
            affiliation.appendChild(document.createTextNode("."));
        }

        var projectLink = document.getElementById("project-link");
        if (projectLink) {
            if (site.projectUrl) {
                projectLink.href = site.projectUrl;
                projectLink.hidden = false;
            } else {
                projectLink.hidden = true;
            }
        }
    }

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

        var saveSession = document.getElementById("save-session");
        if (saveSession) saveSession.checked = state.saveSession === true;
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

    var saveSession = document.getElementById("save-session");
    var privateDialog = document.getElementById("private-dialog");
    if (saveSession) {
        saveSession.addEventListener("change", function () {
            if (this.checked) {
                post("save-session:on");
                return;
            }
            if (!privateDialog) {
                post("save-session:off");
                return;
            }
            var box = this;
            box.checked = true;
            privateDialog.showModal();
            document.getElementById("cancel-private").onclick = function () {
                privateDialog.close();
            };
            document.getElementById("confirm-private").onclick = function () {
                privateDialog.close();
                post("save-session:off");
            };
        });
    }

    var openDashboard = document.getElementById("open-dashboard");
    if (openDashboard) {
        openDashboard.addEventListener("click", function (event) {
            event.preventDefault();
            post("open-dashboard");
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

    applySite();
    post("init");
})();

﻿/// <reference path="../lib/jquery.min.js" />
import * as constants from "./Constants.js";
import * as cookies from "./Cookies.js";

$(document).ready(() => {
    $("#file_drop_zone").on("drop", ($ev) => {
        //prevents the default open file behavior
        let ev = $ev.originalEvent;
        ev.preventDefault();
        if (ev.dataTransfer.items) {
            appendListItemsFromDataTransferItems(ev.dataTransfer.items, $("#confirmationModal ul"));

            //TODO this really needs a in progress modal
            $("#confirmationModal").modal("show").on("click",
                "#confirmationModalConfirmationButton", () => {
                    uploadFiles(ev.dataTransfer.items);
                    $("#confirmationModal").modal("hide");
                });
        }
    });

    $("#file_drop_zone").on("dragover", (ev) => {
        ev.preventDefault();
    });
});

function appendListItemsFromDataTransferItems(dataTransferItems, $ul) {
    $ul.empty();
    for (var i = 0; i < dataTransferItems.length; i++) {
        if (dataTransferItems[i].kind === 'file') {
            let file = dataTransferItems[i].getAsFile();
            $ul.append(`<li>${file.name}</li>`);
        }
    }
}

function uploadFiles(dataTransferItems) {
    if (dataTransferItems) {
        for (var i = 0; i < dataTransferItems.length; i++) {
            // If dropped items aren't files, reject them
            if (dataTransferItems[i].kind === 'file') {
                let file = dataTransferItems[i].getAsFile();

                let formData = new FormData();
                formData.append("file", file);

                $.ajax({
                    method: "post",
                    url: constants.apiUrl + "/File/UploadFile",
                    contentType: false,
                    processData: false,
                    mimeType: "multipart/form-data",
                    data: formData,
                    headers: {
                        "Authorization": "Bearer " + cookies.getAuthCookieTokenOrEmpty()
                    }
                }).done(() => {
                    $("#successDialog").modal("show");
                });
            }
        }
    }
}
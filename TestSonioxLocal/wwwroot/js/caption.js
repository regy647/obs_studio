"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/caption-hub").build();

connection.on("ReceiveCaption", function (caption) {
    console.log(caption);
});

connection.start().then(function () {
    console.log("Caption hub connected.");
}).catch(function (err) {
    return console.error(err.toString());
});
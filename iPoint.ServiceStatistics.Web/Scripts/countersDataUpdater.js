var CountersDataUpdater = function (counterParameres) {
    "use strict";
    var self = this;
    self.drawingSurfaces = [];
    self.dateStarted = new Date();
    self.currentData = [[]];
    self.parameters = counterParameres;
    var timeoutHandler;
    var disposed = false;

    self.UpdateCurrentData = function (data) {
        self.parameters.sd = data.lastDate;
        self.parameters.ed = "";
        $.each(data.seriesData, function (index, series) {
            if (self.currentData.length - 1 < index)
                self.currentData.push([]);
            self.currentData[index].label = series.source + "_" + series.instance + "_" + series.extData + "_" + series.seriesName;
            if (!self.currentData[index].data)
                self.currentData[index].data = [];
            $.each(series.data, function (idx2, seriesValues) {
                self.currentData[index].data.push(seriesValues);
            });
        });
    };

    var onDataReceived = function (data, timeout) {
        if (!disposed) {
            self.UpdateCurrentData(data);
            if (timeout) {
                timeoutHandler = setTimeout(function () { self.UpdateInternal(); }, self.timeout);
            }
        }
    };

    self.StartAutoUpdate = function (timeout) {
        self.timeout = timeout;
        console.log("Starting autoupdate of updater started at " + self.dateStarted);
        timeoutHandler = setTimeout(function () { self.UpdateInternal(); }, 0);
    };

    self.UpdateInternal = function () {
        console.log("Updating drawer started at " + self.dateStarted);
        var caller = this;
        $.getJSON("/Counters/CounterData", caller.parameters, function (data) {
            onDataReceived(data, self.timeout);
        });
    };


    self.RegisterDrawingSurface = function (drawingSurface) {
        self.drawingSurfaces.push(drawingSurface);
    };

    self.StopAutoUpdate = function () {
        console.log("Stopping autoupdate of updater started at " + self.dateStarted);
        self.timeout = undefined;
        clearTimeout(timeoutHandler);
    };

    self.Dispose = function () {
        disposed = true;
        self.timeout = undefined;
        clearTimeout(timeoutHandler);
    };

    self.UpdateOnce = function () { setTimeout(function () { self.UpdateInternal(); }, 0); };

    self.ParamsToString = function () {
        return JSON.stringify(counterParameres);

    };


}

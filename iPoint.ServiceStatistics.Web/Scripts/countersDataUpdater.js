var CountersDataUpdater = function (counterParameres) {
    "use strict";
    var self = this;

    //    self.drawingSurfaces = [];
    self.dateStarted = new Date();
    self.currentData = [];
    self.parameters = counterParameres;
    self.parameters.sd = self.parameters.initialSd;
    self.parameters.ed = self.parameters.initialEd;
    self.DataType = undefined;
    var timeoutHandler;
    var disposed = false;

    var removeUnneededPoints = function (seriesData, stripeInterval, untouchedTailing, totalInterval) {
        var toRemove = 0;

        if (seriesData.data.length > 0) {
            while (seriesData.data[seriesData.data.length - 1] == null || (seriesData.data[seriesData.data.length - 1][0] - totalInterval > seriesData.data[0][0])) {
                seriesData.data.splice(0, 1);
                if (seriesData.lastNonstrippedPointIdx > 0)
                    seriesData.lastNonstrippedPointIdx--;
            }
        }

        for (var i = seriesData.lastNonstrippedPointIdx + 1; i < seriesData.data.length; i++) {
            if (seriesData.data[i] == null) continue;
            if (seriesData.data[i][0] + untouchedTailing > seriesData.data[seriesData.data.length - 1][0]) {
                break;
            }
            
            if (seriesData.data[seriesData.lastNonstrippedPointIdx] == null) continue;
            if (seriesData.data[i][0] < seriesData.data[seriesData.lastNonstrippedPointIdx][0] + stripeInterval) {
                toRemove++;
                continue;
            }

            if (seriesData.data[i][0] >= seriesData.data[seriesData.lastNonstrippedPointIdx][0] + stripeInterval) {
                seriesData.data.splice(seriesData.lastNonstrippedPointIdx + 1, toRemove);
                toRemove = 0;
                seriesData.lastNonstrippedPointIdx++;
                i = seriesData.lastNonstrippedPointIdx;
            }
        }
    };

    self.UpdateCurrentData = function (data) {
        self.parameters.sd = data.lastDate;
        self.parameters.ed = "";
        self.DataType = data.dataType;
        $.each(data.seriesData, function (index, series) {

            var label = series.counterCategory + ": " + series.counterName;
            var labelExtent = "";
            if (series.source != "ALL_SOURCES")
                labelExtent += series.source + ", ";
            if (series.instance != "ALL_INSTANCES")
                labelExtent += series.instance + ", ";
            if (series.extData != "ALL_EXTDATA")
                labelExtent += series.extData + ", ";
            labelExtent = labelExtent.replace(/,\s$/, "");
            if (labelExtent != "")
                label += " (" + labelExtent + ")";
            if (series.seriesName != "value")
                label += " - " + series.seriesName;


            var dataToUpdate = getSeriesDataByUniqId(series.uniqId);

            /*if (self.currentData.length - 1 < index)
            self.currentData.push([]);*/
            dataToUpdate.label = label;
            //self.currentData[index].label = label;
            dataToUpdate.yaxis = series.yaxis;
            //self.currentData[index].yaxis = series.yaxis;

            if (!dataToUpdate.data) {
                dataToUpdate.data = [];
                dataToUpdate.lastNonstrippedPointIdx = 0;
            }

            /*if (!self.currentData[index].data) {
            self.currentData[index].data = [];
            self.currentData[index].lastNonstrippedPointIdx = 0;
            }*/
            $.each(series.data, function (idx2, seriesValues) {
                dataToUpdate.data.push(seriesValues);
                //self.currentData[index].data.push(seriesValues);
            });
            //removeUnneededPoints(self.currentData[index], 300000 /*5 min*/, 600000 * 3 /*30 min*/, Date.parse(self.parameters.initialEd) - Date.parse(self.parameters.initialSd));
            removeUnneededPoints(dataToUpdate, 300000 /*5 min*/, 600000 * 3 /*30 min*/, Date.parse(self.parameters.initialEd) - Date.parse(self.parameters.initialSd));
        });
    };

    var getSeriesDataByUniqId = function (uniqId) {
        for (var i = 0; i < self.currentData.length; i++) {
            if (self.currentData[i].uniqId === uniqId)
                return self.currentData[i];
        }
        self.currentData.push([]);
        var addedSeries = self.currentData[self.currentData.length - 1];
        addedSeries.uniqId = uniqId;
        return addedSeries;
    };

    var onDataReceived = function (data, timeout) {
        if (!disposed) {
            if (data && data.success)
                self.UpdateCurrentData(data);
            if (timeout) {
                timeoutHandler = setTimeout(function () { self.UpdateInternal(); }, self.timeout);
            }
        }
    };

    self.StartAutoUpdate = function (timeout) {
        self.timeout = timeout;
        console.log("Starting autoupdate of updater started at " + self.dateStarted.toString());
        timeoutHandler = setTimeout(function () { self.UpdateInternal(); }, 0);
    };

    self.UpdateInternal = function () {
        console.log("Updating drawer started at " + self.dateStarted.toString());
        var caller = this;
        $.getJSON("/Counters/CounterData", caller.parameters, function (data) {
            onDataReceived(data, self.timeout);
        });
    };


    /*   self.RegisterDrawingSurface = function (drawingSurface) {
    self.drawingSurfaces.push(drawingSurface);
    };*/

    self.StopAutoUpdate = function () {
        console.log("Stopping autoupdate of updater started at " + self.dateStarted.toString());
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
        return JSON.stringify(self.parameters);

    };


}

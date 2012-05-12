var DrawingSurface = function (name, id, drawingArea, overviewArea, legendArea) {
    "use strict";
    var self = this;

    var dataUpdaters = new Array();
    self.drawingArea = drawingArea;
    self.drawingAreaPlot = undefined;
    self.overviewArea = overviewArea;
    self.overviewAreaPlot = undefined;
    var timeoutHandler;

    self.millisecondsToDuration = function (ticks, axis) {
        var hms = "";
        var dtm = new Date();
        dtm.setTime(ticks);
        var h = "0" + Math.floor(ticks / 3600000);
        var m = "0" + dtm.getMinutes();
        var s = "0" + dtm.getSeconds();
        var cs = "00" + Math.round(dtm.getMilliseconds() / 10);
        hms = h.substr(h.length - 4) + ":" + m.substr(m.length - 2) + ":";
        hms += s.substr(s.length - 2) + "." + cs.substr(cs.length - 3);
        return hms;
    };
    
    self.drawingOptions = {
        lines: {
            show: true
        },
        points: {
            show: false
        },
        xaxis: {
            mode: "time",
            autoscaleMargin: 0.02
        },
        yaxis: { max: null,alignTicksWithAxis:1 },
        yaxes: [{}, { position: "right", tickFormatter: self.millisecondsToDuration}],
        zoom: {
            interactive: false
        },
        pan: {
            interactive: false
        },
        //selection: { mode: "xy" },
        legend: { container: legendArea }
    };

    if (!legendArea)
        $.extend(true, self.drawingOptions, { legend: { show: false} });



    self.overviewOptions = {
        legend: { show: false },
        xaxis: { ticks: 4, mode: "time" },
        yaxis: { ticks: 3 },
        grid: { color: "#999" },
        selection: { mode: "xy" },
        series: {
            lines: { show: true, lineWidth: 1 },
            shadowSize: 0
        }
    };
    var getCurrentData = function () {
        var result = [];
        $.each(dataUpdaters, function (dataUpdaterIdx, dataUpdater) {
            $.each(dataUpdater.currentData, function (seriesIndex, dataSeries) {
                result.push(dataSeries);
            });
        });
        return result;
    };


    timeoutHandler = setTimeout(function () { self.UpdateInternal(); }, 0);


    /* self.drawingArea.bind("plotselected", function (event, ranges) {
    // clamp the zooming to prevent eternal zoom
    if (ranges.xaxis.to - ranges.xaxis.from < 0.00001)
    ranges.xaxis.to = ranges.xaxis.from + 0.00001;
    if (ranges.yaxis.to - ranges.yaxis.from < 0.00001)
    ranges.yaxis.to = ranges.yaxis.from + 0.00001;

    self.drawingAreaPlot = $.plot(self.drawingArea, getData(ranges.xaxis.from, ranges.xaxis.to),
    $.extend(true, {}, options, {
    xaxis: { min: ranges.xaxis.from, max: ranges.xaxis.to },
    yaxis: { min: ranges.yaxis.from, max: ranges.yaxis.to }
    }));

    // don't fire event on the overview to prevent eternal loop
    if (self.overviewAreaPlot)
    self.overviewAreaPlot.setSelection(ranges, true);
    });

    if (self.overviewAreaPlot)
    self.overviewArea.bind("plotselected", function (event, ranges) {
    self.drawingAreaPlot.setSelection(ranges);
    });
        
    */

    self.Dispose = function () {
        $.each(dataUpdaters, function (dataUpdaterIdx, dataUpdater) {
            dataUpdater.Dispose();
        });
        clearTimeout(timeoutHandler);
    };


    self.RegisterDataUpdater = function (counterDataUpdater) {
        dataUpdaters.push(counterDataUpdater);
    };

    self.UpdateInternal = function () {
        var currentData = getCurrentData();
        self.drawingAreaPlot = $.plot(self.drawingArea, currentData, self.drawingOptions);
        if (self.overviewAreaPlot)
            self.overviewAreaPlot = $.plot(self.overviewArea, currentData, self.overviewOptions);
        timeoutHandler = setTimeout(function () { self.UpdateInternal(); }, 5000);
    };

    self.DataUpdaterSettingsToString = function () {
        var result = "";
        $.each(dataUpdaters, function (idx, dataUpdater) { result = result + dataUpdater.ParamsToString() + "\n"; });
        return $.base64.encode(result);
    };

    
}
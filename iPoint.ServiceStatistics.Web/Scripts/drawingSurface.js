var DrawingSurface = function (name, id, drawingArea, overviewArea, legendArea) {
    "use strict";
    var self = this;
    drawingArea.resizable();
    var dataUpdaters = new Array();
    self.Name = name;
    self.Id = id;
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


    self.formatLabel = function (label, s) {
        var action = s.lines.enabled ? 'hide' : 'show';
        var seriesName = label;
        label = label + ' (<a href="#" onclick="drawingSurfaces[\'' + id + '\'].ToggleSeries(\'' + seriesName.replace(new RegExp("\'", 'g'), "\\\'") + '\'); return false;">' + action + '</a>)';
        var smooth = s.lines.smooth ? 'turn off' : 'turn on';
        return label + ' smooth: (<a href="#" onclick="drawingSurfaces[\'' + id + '\'].SmoothSeries(\'' + seriesName.replace(new RegExp("\'", 'g'), "\\\'") + '\'); return false;">' + smooth + '</a>)';
    };

    //<a href="#" onclick="javascript:drawingSurfaces["bff1b2c3-4109-46d3-bf5b-1b0133eee675"].toggleseries("ft:="" error_count="" (app103)");"="">FT: Error_Count (APP103)</a>

    //<a href="#" onclick="drawingSurfaces[7986ac57-99aa-439a-be88-30d77a5bf4e2].ToggleSeries(" ft:="" error_count="" (app106)")"="">FT: Error_Count (APP106)</a>

    self.ToggleSeries = function (seriesName) {
        $.each(dataUpdaters, function (dataUpdaterIdx, dataUpdater) {
            $.each(dataUpdater.currentData, function (seriesIndex, dataSeries) {
                if (dataSeries.label != seriesName) return;
                if (dataSeries.lines == undefined)
                    dataSeries.lines = {enabled: true, smooth: true};
                dataSeries.lines.enabled = !dataSeries.lines.enabled;
            });
        });
        self.ForcedUpdate();
    };

    self.SmoothSeries = function (seriesName) {
        $.each(dataUpdaters, function (dataUpdaterIdx, dataUpdater) {
            $.each(dataUpdater.currentData, function (seriesIndex, dataSeries) {
                if (dataSeries.label != seriesName) return;
                if (dataSeries.lines == undefined)
                    dataSeries.lines = { enabled: true, smooth: true };
                dataSeries.lines.smooth = !dataSeries.lines.smooth;
            });
        });
        self.ForcedUpdate();
    };

    self.drawingOptions = {
        lines: {
            //spline: 1000,
            //steps: true,
            show: false,
            enabled: true

        },
        points: {
            show: false
        },
        grid: { axisMargin: 5, labelMargin: 5 },
        xaxis: {
            mode: "time",
            autoscaleMargin: 0.02
        },
        series: {
            shadowSize: 0,
            curvedLines: {
                active: true
            }
        },
        yaxis: { max: null, alignTicksWithAxis: 1 },
        yaxes: [{}, { position: "right", tickFormatter: self.millisecondsToDuration}],
        zoom: {
            interactive: false
        },
        pan: {
            interactive: false
        },
        //selection: { mode: "xy" },
        legend: { container: legendArea, labelFormatter: self.formatLabel }
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
                if (!dataSeries.lines || dataSeries.lines.enabled)
                    if (!dataSeries.lines || dataSeries.lines.smooth)
                        result.push($.extend(false, {}, dataSeries, { curvedLines: { active: true, show: true, fit: true, fitPointDist: 0.000000001, lineWidth: 3} }));
                    else
                        result.push($.extend(false, {}, dataSeries, { lines: { show: true} }));
                else
                    result.push($.extend(false, {}, dataSeries, { data: [null] }));
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

    self.ForcedUpdate = function () {

        var currentData = getCurrentData();
        self.drawingAreaPlot = $.plot(self.drawingArea, currentData, self.drawingOptions);
        if (self.overviewAreaPlot)
            self.overviewAreaPlot = $.plot(self.overviewArea, currentData, self.overviewOptions);
    };

    self.DataUpdaterSettingsToString = function () {
        var result = "";
        $.each(dataUpdaters, function (idx, dataUpdater) { result = result + dataUpdater.ParamsToString() + "\n"; });
        return $.base64.encode(result);
    };


}
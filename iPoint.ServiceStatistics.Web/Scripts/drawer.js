var Drawer = function (drawingSurface, counterParameres) {
    "use strict";
    var self = this;
    this.options = {
        lines: {
            show: true
        },
        points: {
            show: false
        },
        xaxis: {
            mode: "time",
            autoscaleMargin : 0.02
        },
        yaxis: { max: null },
        zoom: {
            interactive: false
        },
        pan: {
            interactive: false
        },
        selection: { mode: "xy" },
        legend:{ container: $("#legendContainer")}

    };

    this.overviewOpts = { legend: { show: false },
        xaxis: { ticks: 4, mode: "time" },
        yaxis: { ticks: 3 },
        grid: { color: "#999" },
        selection: { mode: "xy" },
        series: {
            lines: { show: true, lineWidth: 1 },
            shadowSize: 0
        }
        
    };

    this.drawingSurface = drawingSurface;
    this.dateStarted = new Date();
    this.currentData = [[]];
    this.parameters = counterParameres;
    var timeoutHandler;
    var disposed = false;
    var plot = $.plot(self.drawingSurface, self.currentData, self.options);
    var overview = $.plot($("#overview"), self.currentData, self.overviewOpts);

     self.drawingSurface.bind("plotselected", function (event, ranges) {
        // clamp the zooming to prevent eternal zoom
        if (ranges.xaxis.to - ranges.xaxis.from < 0.00001)
            ranges.xaxis.to = ranges.xaxis.from + 0.00001;
        if (ranges.yaxis.to - ranges.yaxis.from < 0.00001)
            ranges.yaxis.to = ranges.yaxis.from + 0.00001;

        plot = $.plot($("#placeholder"), getData(ranges.xaxis.from, ranges.xaxis.to),
                      $.extend(true, {}, options, {
                          xaxis: { min: ranges.xaxis.from, max: ranges.xaxis.to },
                          yaxis: { min: ranges.yaxis.from, max: ranges.yaxis.to }
                      }));

        // don't fire event on the overview to prevent eternal loop
        overview.setSelection(ranges, true);
    });
    
    $("#overview").bind("plotselected", function (event, ranges) {
        plot.setSelection(ranges);
    });
    
    this.getMax = function (run) {
        if (!run) {
            run = 1;
        }
        var sliceLast = 5 * run;
        var sliceLastEnd = 5 * (run - 1);
        var max = -Infinity;
        var beginOfArrayReached = false;
        var data = self.currentData;
        $.each(data, function (idx, serie) {
            var sliceFrom = 0;
            if (!serie.data) {
                beginOfArrayReached = true;
                return;
            }
            var sliceTo = serie.data.length;
            if (serie.data.length >= sliceLast) {
                sliceFrom = serie.data.length - sliceLast;
                sliceTo = serie.data.length - sliceLastEnd;
            }
            beginOfArrayReached = sliceFrom === 0;
            var slice = serie.data.slice(sliceFrom, sliceTo);

            $.each(slice, function (idx2, elem) {
                if (elem[1] !== null && max < elem[1]) {
                    max = elem[1];
                }
            });
        });
        if (max == -Infinity && !beginOfArrayReached) {
            max = self.getMax(run + 1);
        }
        return max;
    };

    this.Draw = function (timeout) {
        if (!disposed) {
            var maxY = self.getMax();
            self.options.yaxis.max = maxY + (maxY / 2);
            //self.options.xaxis.max = self.options.xaxis.max + 1000*10;
            $.plot(self.drawingSurface, self.currentData, self.options);
            $.plot($("#overview"), self.currentData, self.overviewOpts);
            if (timeout) {
                timeoutHandler = setTimeout(function () { self.UpdateAndDraw(timeout); }, timeout);
            }
        }
    };

    this.Update = function (data) {
        self.parameters.sd = data.lastDate;
        self.parameters.ed = "";
        $.each(data.seriesData, function (index, series) {
            if (self.currentData.length - 1 < index)
                self.currentData.push([]);
            self.currentData[index].label = series.label;
            if (!self.currentData[index].data)
                self.currentData[index].data = [];
            $.each(series.data, function (idx2, seriesValues) {
                self.currentData[index].data.push(seriesValues);
            });

        });

        //currentData[0].push(data.seriesData[0]);
    };
    var onDataReceived = function (data, timeout) {
        self.Update(data);
        self.Draw(timeout);
    };

    this.UpdateAndDraw = function (timeout) {
        console.log("Updating drawer started at " + self.dateStarted);

        var caller = this;
        $.getJSON("/Counters/CounterData", caller.parameters, function (data) {
            onDataReceived(data, timeout);
        });
    };

    this.Dispose = function () {
        disposed = true;
        self.timeout = undefined;
        clearTimeout(timeoutHandler);
    };
};

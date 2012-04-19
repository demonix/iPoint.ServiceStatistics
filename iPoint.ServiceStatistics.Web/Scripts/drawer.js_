var Drawer = function (drawingSurface, overviewSurface, legendContainer, counterParameres) {
    "use strict";
    var self = this;

    this.dateStarted = new Date();
    this.currentData = [[]];
    this.parameters = counterParameres;
    var timeoutHandler;
    var disposed = false;

    self.getMax = function (run) {
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

    self.Draw = function () {
        var maxY = self.getMax();
        self.options.yaxis.max = maxY + (maxY / 2);
        $.plot(self.drawingSurface, self.currentData, self.options);
        $.plot(self.overviewSurface, self.currentData, self.overviewOpts);
    };

    self.Update = function (data) {
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
    };
    
    var onDataReceived = function(data, timeout) {
        if (!disposed) {
            self.Update(data);
            self.Draw();
            if (timeout) {
                timeoutHandler = setTimeout(function() { self.UpdateAndDraw(timeout); }, timeout);
            }
        }
    };

    self.UpdateAndDraw = function (timeout) {
        console.log("Updating drawer started at " + self.dateStarted);
        var caller = this;
        $.getJSON("/Counters/CounterData", caller.parameters, function (data) {
            onDataReceived(data, timeout);
        });
    };

    self.Dispose = function () {
        disposed = true;
        self.timeout = undefined;
        clearTimeout(timeoutHandler);
    };
};

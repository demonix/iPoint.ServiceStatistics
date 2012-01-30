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
            mode: "time"
        }
    };

    this.drawingSurface = drawingSurface;
    this.dateStarted = new Date();
    this.currentData = [[]];
    this.parameters = counterParameres;
    var timeoutHandler;
    var disposed = false;
    $.plot(self.drawingSurface, self.currentData, self.options);

    this.Draw = function (timeout) {
        if (!disposed) {
            $.plot(self.drawingSurface, self.currentData, self.options);
            if (timeout) {
                timeoutHandler = setTimeout(function () { self.UpdateAndDraw(timeout); }, timeout);
            }
        }
    };

    this.Update = function (data) {
        self.parameters.sd = data.lastDate;
        self.parameters.ed = "";
        $.each(data.seriesData, function (index, series) {
            self.currentData[0].push(series);
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

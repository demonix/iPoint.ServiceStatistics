namespace iPoint.ServiceStatistics.Web.Models
{
    public class SingleGraphModel
    {
        public SingleGraphModel(string graphTitle, string drawerParameters, int plotWidth, int plotHeight)
        {
            GraphTitle = graphTitle;
            DrawerParameters = drawerParameters;
            PlotWidth = plotWidth;
            PlotHeight = plotHeight;
        }

        public string GraphTitle { get; private set; }
        public string DrawerParameters { get; private set; }
        public int PlotWidth { get; private set; }
        public int PlotHeight { get; private set; }

    }
}
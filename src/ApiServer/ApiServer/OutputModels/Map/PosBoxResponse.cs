namespace WomPlatform.Web.Api.OutputModels.Map {
    public class PosBoxResponse {

        public class PosEntry {
            public string Id { get; init; }

            public string Name { get; init; }

            public GeoCoords Position { get; init; }

            public string Url { get; init; }
        }

        public PosEntry[] Pos { get; init; }

        public GeoCoords LowerLeft { get; init; }

        public GeoCoords UpperRight { get; init; }

    }
}
